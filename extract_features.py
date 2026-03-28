"""
Extract MFCC Features (4-class version)
=========================================
Extracts MFCCs from inhale, exhale, silence, and noise/speech classes.
Uses mfcc_mirror.py to match C# MFCCExtractor.

Usage:
    python extract_features.py --repo_path .
"""

import os
import argparse
import numpy as np
import mfcc_mirror

SAMPLE_RATE = 48000
SEGMENT_DURATION = 0.5
CLASSES = ["inhale", "exhale", "silence", "noise"]


def classify_path(filepath):
    lower = filepath.lower()
    if "inhale" in lower or "wdech" in lower:
        return 0
    elif "exhale" in lower or "wydech" in lower:
        return 1
    elif "silence" in lower or "cisza" in lower:
        return 2
    elif "noise" in lower or "speech" in lower:
        return 3
    return None


def find_wav_files(root_dir):
    wav_files = []
    if not os.path.exists(root_dir):
        return wav_files
    for dirpath, dirnames, filenames in os.walk(root_dir):
        for fname in filenames:
            if fname.lower().endswith(".wav"):
                wav_files.append(os.path.join(dirpath, fname))
    return wav_files


def load_wav(filepath, target_sr=SAMPLE_RATE):
    try:
        import soundfile as sf
        audio, sr = sf.read(filepath, dtype="float32")
    except Exception:
        import wave
        import struct
        with wave.open(filepath, "r") as wf:
            sr = wf.getframerate()
            n = wf.getnframes()
            ch = wf.getnchannels()
            raw = wf.readframes(n)
            if wf.getsampwidth() == 2:
                samples = struct.unpack("<" + "h" * (n * ch), raw)
                audio = np.array(samples, dtype=np.float32) / 32768.0
                if ch > 1:
                    audio = audio.reshape(-1, ch).mean(axis=1)
            else:
                return None

    if len(audio.shape) > 1:
        audio = audio.mean(axis=1)

    if sr != target_sr:
        duration = len(audio) / sr
        target_len = int(duration * target_sr)
        indices = np.linspace(0, len(audio) - 1, target_len)
        audio = np.interp(indices, np.arange(len(audio)), audio).astype(np.float32)

    return audio


def process_wav(filepath, sr=SAMPLE_RATE, segment_dur=SEGMENT_DURATION):
    label = classify_path(filepath)
    if label is None:
        return []

    audio = load_wav(filepath, sr)
    if audio is None or len(audio) == 0:
        return []

    segment_samples = int(sr * segment_dur)
    results = []

    for start in range(0, len(audio) - segment_samples + 1, segment_samples):
        segment = audio[start:start + segment_samples]
        rms = np.sqrt(np.mean(segment ** 2))
        if rms < 1e-5:
            continue
        mfcc = mfcc_mirror.compute_mfcc(segment, sr)
        results.append((mfcc, label))

    return results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo_path", default=".",
                        help="Path to Breathing-Classification repo root")
    parser.add_argument("--quest_path", default="./quest_data")
    parser.add_argument("--output", default="dataset.npz")
    args = parser.parse_args()

    all_features = []
    all_labels = []

    repo_data_dir = os.path.join(args.repo_path, "data")
    print(f"Scanning: {repo_data_dir}")
    repo_wavs = find_wav_files(repo_data_dir)
    print(f"  Found {len(repo_wavs)} WAV files")

    for i, wav_path in enumerate(repo_wavs):
        if (i + 1) % 50 == 0:
            print(f"  Processing {i+1}/{len(repo_wavs)}...")
        results = process_wav(wav_path)
        if results:
            for mfcc, label in results:
                all_features.append(mfcc)
                all_labels.append(label)

    if os.path.exists(args.quest_path):
        print(f"\nScanning Quest data: {args.quest_path}")
        for wav_path in find_wav_files(args.quest_path):
            for mfcc, label in process_wav(wav_path):
                all_features.append(mfcc)
                all_labels.append(label)

    if not all_features:
        print("No data found!")
        return

    time_dims = [f.shape[1] for f in all_features]
    target_time = max(set(time_dims), key=time_dims.count)

    X, y = [], []
    for feat, label in zip(all_features, all_labels):
        if feat.shape[1] >= target_time:
            X.append(feat[:, :target_time])
        else:
            padded = np.zeros((feat.shape[0], target_time), dtype=np.float32)
            padded[:, :feat.shape[1]] = feat
            X.append(padded)
        y.append(label)

    X = np.array(X, dtype=np.float32)
    y = np.array(y, dtype=np.int64)

    print(f"\nDataset: X={X.shape}")
    for i, name in enumerate(CLASSES):
        count = np.sum(y == i)
        print(f"  {name}: {count} ({100*count/len(y):.1f}%)")

    np.savez(args.output, X=X, y=y, label_names=CLASSES,
             sample_rate=SAMPLE_RATE, n_mfcc=X.shape[1],
             hop_length=mfcc_mirror.DEFAULT_HOP_LENGTH,
             n_fft=mfcc_mirror.DEFAULT_N_FFT,
             segment_duration=SEGMENT_DURATION)
    print(f"Saved to {args.output}")


if __name__ == "__main__":
    main()
