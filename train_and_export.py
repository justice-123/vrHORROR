"""
Train and Export to ONNX (4-class version)
============================================
Supports: inhale, exhale, silence, noise/speech

Usage:
    python train_and_export.py
"""

import argparse
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader, TensorDataset
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix


class BreathClassifier(nn.Module):
    """
    1D CNN breath classifier.
    Input:  (batch, n_mfcc, time_frames)
    Output: (batch, num_classes) logits
    """
    def __init__(self, n_mfcc=13, num_classes=4, dropout=0.3):
        super().__init__()
        self.conv1 = nn.Conv1d(n_mfcc, 64, kernel_size=3, padding=1)
        self.conv2 = nn.Conv1d(64, 128, kernel_size=3, padding=1)
        self.conv3 = nn.Conv1d(128, 64, kernel_size=3, padding=1)
        self.pool = nn.AdaptiveAvgPool1d(1)
        self.drop = nn.Dropout(dropout)
        self.fc1 = nn.Linear(64, 64)
        self.fc2 = nn.Linear(64, num_classes)
        self.relu = nn.ReLU()

    def forward(self, x):
        x = self.relu(self.conv1(x))
        x = self.relu(self.conv2(x))
        x = self.relu(self.conv3(x))
        x = self.pool(x).squeeze(-1)
        x = self.drop(x)
        x = self.relu(self.fc1(x))
        x = self.drop(x)
        return self.fc2(x)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", default="dataset.npz")
    parser.add_argument("--output", default="breath_classifier.onnx")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--batch_size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=0.001)
    args = parser.parse_args()

    print("Loading dataset...")
    data = np.load(args.dataset, allow_pickle=True)
    X = data["X"]
    y = data["y"]
    label_names = list(data["label_names"])

    n_mfcc = X.shape[1]
    time_frames = X.shape[2]
    num_classes = len(label_names)

    print(f"Samples: {X.shape[0]}, MFCCs: {n_mfcc}, Time frames: {time_frames}")
    print(f"Classes ({num_classes}): {label_names}")
    for i, name in enumerate(label_names):
        print(f"  {name}: {np.sum(y == i)}")

    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    print(f"\nTrain: {len(X_train)}, Test: {len(X_test)}")

    train_loader = DataLoader(
        TensorDataset(torch.FloatTensor(X_train), torch.LongTensor(y_train)),
        batch_size=args.batch_size, shuffle=True
    )
    test_loader = DataLoader(
        TensorDataset(torch.FloatTensor(X_test), torch.LongTensor(y_test)),
        batch_size=args.batch_size
    )

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = BreathClassifier(n_mfcc=n_mfcc, num_classes=num_classes).to(device)
    print(f"Device: {device}")
    print(f"Parameters: {sum(p.numel() for p in model.parameters()):,}")

    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=args.lr)
    scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, patience=10, factor=0.5)

    best_acc = 0
    for epoch in range(args.epochs):
        model.train()
        total_loss = 0
        correct = 0
        total = 0

        for bx, by in train_loader:
            bx, by = bx.to(device), by.to(device)
            optimizer.zero_grad()
            out = model(bx)
            loss = criterion(out, by)
            loss.backward()
            optimizer.step()
            total_loss += loss.item()
            _, pred = torch.max(out, 1)
            total += by.size(0)
            correct += (pred == by).sum().item()

        train_acc = correct / total

        model.eval()
        val_correct = 0
        val_total = 0
        val_loss = 0
        with torch.no_grad():
            for bx, by in test_loader:
                bx, by = bx.to(device), by.to(device)
                out = model(bx)
                val_loss += criterion(out, by).item()
                _, pred = torch.max(out, 1)
                val_total += by.size(0)
                val_correct += (pred == by).sum().item()

        val_acc = val_correct / val_total
        scheduler.step(val_loss)

        if (epoch + 1) % 10 == 0 or val_acc > best_acc:
            print(f"Epoch {epoch+1:3d}: train={train_acc:.3f} val={val_acc:.3f} loss={total_loss/len(train_loader):.4f}")

        if val_acc > best_acc:
            best_acc = val_acc
            torch.save(model.state_dict(), "best_model.pth")

    print(f"\nBest validation accuracy: {best_acc:.3f}")
    model.load_state_dict(torch.load("best_model.pth", weights_only=True))
    model.eval()

    all_preds = []
    all_labels = []
    with torch.no_grad():
        for bx, by in test_loader:
            bx = bx.to(device)
            _, pred = torch.max(model(bx), 1)
            all_preds.extend(pred.cpu().numpy())
            all_labels.extend(by.numpy())

    print("\nClassification Report:")
    print(classification_report(all_labels, all_preds, target_names=label_names))
    print("Confusion Matrix:")
    print(confusion_matrix(all_labels, all_preds))

    # Export ONNX
    print("\nExporting to ONNX...")
    model.cpu().eval()
    dummy = torch.randn(1, n_mfcc, time_frames)

    torch.onnx.export(
        model, dummy, args.output,
        export_params=True,
        opset_version=18,
        input_names=["mfcc_input"],
        output_names=["class_output"],
        dynamo=False
    )
    print(f"Saved: {args.output}")

    # Verify
    import onnx
    onnx.checker.check_model(onnx.load(args.output))

    import onnxruntime as ort
    sess = ort.InferenceSession(args.output)
    test_out = sess.run(None, {"mfcc_input": np.random.randn(1, n_mfcc, time_frames).astype(np.float32)})
    print(f"ONNX test output shape: {test_out[0].shape}")

    print(f"\n--- Unity Setup ---")
    print(f"MFCC coefficients: {n_mfcc}")
    print(f"Time frames: {time_frames}")
    print(f"Sample rate: {int(data['sample_rate'])}")
    print(f"Hop length: {int(data['hop_length'])}")
    print(f"FFT size: {int(data['n_fft'])}")
    print(f"Clip duration: {float(data['segment_duration'])}s")
    print(f"Classes ({num_classes}): {label_names}")
    print(f"\nCopy {args.output} to Unity Assets/")


if __name__ == "__main__":
    main()