"""
MFCC Extractor - Python mirror of MFCCExtractor.cs
====================================================
This replicates the EXACT same computation as the C# Unity code,
so the model trains on identical features to what it sees at runtime.

DO NOT use librosa.feature.mfcc for training if you're using
MFCCExtractor.cs at runtime - they produce different values.

Usage:
    import mfcc_mirror
    mfcc = mfcc_mirror.compute_mfcc(audio, sr=48000)
"""

import numpy as np


DEFAULT_N_MFCC = 13
DEFAULT_N_FFT = 2048
DEFAULT_HOP_LENGTH = 512
DEFAULT_N_MELS = 128
DEFAULT_F_MIN = 0.0
DEFAULT_F_MAX = 8000.0


def compute_mfcc(audio, sr=48000, n_mfcc=DEFAULT_N_MFCC, n_fft=DEFAULT_N_FFT,
                 hop_length=DEFAULT_HOP_LENGTH, n_mels=DEFAULT_N_MELS,
                 f_min=DEFAULT_F_MIN, f_max=DEFAULT_F_MAX):
    """
    Compute MFCC features matching the C# MFCCExtractor exactly.
    Returns: (n_mfcc, time_frames) array
    """
    # Step 1: Power spectrogram via STFT
    power_spec = _compute_power_spectrogram(audio, n_fft, hop_length)

    # Step 2: Mel filterbank
    mel_filters = _create_mel_filterbank(sr, n_fft, n_mels, f_min, f_max)
    mel_spec = mel_filters @ power_spec  # (n_mels, time_frames)

    # Step 3: Log scale (natural log, matching C# Mathf.Log)
    mel_spec = np.log(np.maximum(mel_spec, 1e-10))

    # Step 4: DCT (unnormalized, matching C# implementation)
    mfcc = _apply_dct(mel_spec, n_mfcc)

    return mfcc.astype(np.float32)


def _compute_power_spectrogram(audio, n_fft, hop_length):
    """STFT with Hann window, returns power spectrum."""
    num_frames = 1 + (len(audio) - n_fft) // hop_length
    if num_frames <= 0:
        num_frames = 1

    freq_bins = n_fft // 2 + 1
    power = np.zeros((freq_bins, num_frames), dtype=np.float64)

    # Hann window matching C#: 0.5 * (1 - cos(2*pi*i/(n-1)))
    window = 0.5 * (1.0 - np.cos(2.0 * np.pi * np.arange(n_fft) / (n_fft - 1)))

    for t in range(num_frames):
        start = t * hop_length
        frame = np.zeros(n_fft)
        end = min(start + n_fft, len(audio))
        length = end - start
        frame[:length] = audio[start:end] * window[:length]

        # FFT
        fft_result = np.fft.fft(frame)

        # Power spectrum (magnitude squared)
        power[:, t] = np.abs(fft_result[:freq_bins]) ** 2

    return power


def _hz_to_mel(hz):
    """Matches C#: 2595 * log10(1 + hz/700)"""
    return 2595.0 * np.log10(1.0 + hz / 700.0)


def _mel_to_hz(mel):
    """Matches C#: 700 * (10^(mel/2595) - 1)"""
    return 700.0 * (10.0 ** (mel / 2595.0) - 1.0)


def _create_mel_filterbank(sr, n_fft, n_mels, f_min, f_max):
    """Create mel filterbank matching C# implementation with Slaney normalization."""
    freq_bins = n_fft // 2 + 1
    filterbank = np.zeros((n_mels, freq_bins))

    mel_min = _hz_to_mel(f_min)
    mel_max = _hz_to_mel(f_max)

    # Equally spaced mel points
    mel_points = np.linspace(mel_min, mel_max, n_mels + 2)

    # Convert to FFT bin indices (matching C#: hz * (n_fft + 1) / sr)
    bin_freqs = np.array([_mel_to_hz(m) * (n_fft + 1) / sr for m in mel_points])

    for m in range(n_mels):
        left = bin_freqs[m]
        center = bin_freqs[m + 1]
        right = bin_freqs[m + 2]

        for f in range(freq_bins):
            if f >= left and f <= center and center > left:
                filterbank[m, f] = (f - left) / (center - left)
            elif f > center and f <= right and right > center:
                filterbank[m, f] = (right - f) / (right - center)

        # Slaney normalization (matching C#)
        enorm = 2.0 / (_mel_to_hz(mel_points[m + 2]) - _mel_to_hz(mel_points[m]))
        filterbank[m] *= enorm

    return filterbank


def _apply_dct(mel_spec, n_mfcc):
    """
    Type-II DCT WITHOUT normalization.
    Matches the C# implementation exactly.
    """
    n_mels = mel_spec.shape[0]
    num_frames = mel_spec.shape[1]
    mfcc = np.zeros((n_mfcc, num_frames))

    for k in range(n_mfcc):
        for t in range(num_frames):
            s = 0.0
            for n in range(n_mels):
                s += mel_spec[n, t] * np.cos(
                    np.pi * k * (2 * n + 1) / (2.0 * n_mels))
            mfcc[k, t] = s

    return mfcc