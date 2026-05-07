using System;
using UnityEngine;

/// <summary>
/// Computes MFCC features from raw audio samples in C#.
/// Matches librosa's MFCC output so the features are compatible
/// with the Python-trained ONNX model.
///
/// Usage:
///     float[] audio = ...; // 0.5 seconds of audio at 48kHz
///     float[,] mfcc = MFCCExtractor.ComputeMFCC(audio, 48000);
///     // mfcc shape: (13, timeFrames) - ready for model input
/// </summary>
public static class MFCCExtractor
{
    // Default parameters matching the Python training pipeline
    public const int DefaultNMfcc = 13;
    public const int DefaultNFft = 2048;
    public const int DefaultHopLength = 512;
    public const int DefaultNMels = 128;
    public const float DefaultFMin = 0f;
    public const float DefaultFMax = 8000f;

    /// <summary>
    /// Compute MFCC features from audio samples.
    /// Returns a (nMfcc, timeFrames) array.
    /// </summary>
    public static float[,] ComputeMFCC(
        float[] audio, int sampleRate,
        int nMfcc = DefaultNMfcc,
        int nFft = DefaultNFft,
        int hopLength = DefaultHopLength,
        int nMels = DefaultNMels)
    {
        // first computing power spectrogram via STFT
        float[,] powerSpec = ComputePowerSpectrogram(audio, nFft, hopLength);

        //  Applying the mel filterbank
        float[,] melFilters = CreateMelFilterbank(sampleRate, nFft, nMels, DefaultFMin, DefaultFMax);
        float[,] melSpec = ApplyMelFilterbank(powerSpec, melFilters);

        // Converting to log scale
        LogScale(melSpec);

        //  Applying DCT to get MFCCs
        float[,] mfcc = ApplyDCT(melSpec, nMfcc);

        return mfcc;
    }

    /// <summary>
    /// Flatten a 2D MFCC array to 1D for model input.
    /// Output order: all time frames for coeff 0, then all for coeff 1, etc.
    /// </summary>
    public static float[] Flatten(float[,] mfcc)
    {
        int rows = mfcc.GetLength(0);
        int cols = mfcc.GetLength(1);
        float[] flat = new float[rows * cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                flat[r * cols + c] = mfcc[r, c];
        return flat;
    }

    // internal methiods

    static float[,] ComputePowerSpectrogram(float[] audio, int nFft, int hopLength)
    {
        int numFrames = 1 + (audio.Length - nFft) / hopLength;
        if (numFrames <= 0) numFrames = 1;

        int freqBins = nFft / 2 + 1;
        float[,] power = new float[freqBins, numFrames];

        float[] window = HannWindow(nFft);
        float[] fftReal = new float[nFft];
        float[] fftImag = new float[nFft];
        float[] frame = new float[nFft];

        for (int t = 0; t < numFrames; t++)
        {
            int start = t * hopLength;

            // Extract and window the frame
            for (int i = 0; i < nFft; i++)
            {
                int idx = start + i;
                frame[i] = (idx < audio.Length) ? audio[idx] * window[i] : 0f;
            }

            // Copy to FFT buffers
            Array.Copy(frame, fftReal, nFft);
            Array.Clear(fftImag, 0, nFft);

            // In-place FFT
            FFT(fftReal, fftImag);

            // Power spectrum (magnitude squared)
            for (int f = 0; f < freqBins; f++)
            {
                float re = fftReal[f];
                float im = fftImag[f];
                power[f, t] = re * re + im * im;
            }
        }

        return power;
    }

    static float[,] CreateMelFilterbank(int sr, int nFft, int nMels, float fMin, float fMax)
    {
        int freqBins = nFft / 2 + 1;
        float[,] filterbank = new float[nMels, freqBins];

        // Convert Hz to Mel
        float melMin = HzToMel(fMin);
        float melMax = HzToMel(fMax);

        // Create equally spaced mel points
        float[] melPoints = new float[nMels + 2];
        for (int i = 0; i < nMels + 2; i++)
            melPoints[i] = melMin + (melMax - melMin) * i / (nMels + 1);

        // Convert mel points back to Hz, then to FFT bin indices
        float[] binFreqs = new float[nMels + 2];
        for (int i = 0; i < nMels + 2; i++)
        {
            float hz = MelToHz(melPoints[i]);
            binFreqs[i] = hz * (nFft + 1) / sr;
        }

        // Create triangular filters
        for (int m = 0; m < nMels; m++)
        {
            float left = binFreqs[m];
            float center = binFreqs[m + 1];
            float right = binFreqs[m + 2];

            for (int f = 0; f < freqBins; f++)
            {
                if (f >= left && f <= center && center > left)
                    filterbank[m, f] = (f - left) / (center - left);
                else if (f > center && f <= right && right > center)
                    filterbank[m, f] = (right - f) / (right - center);
                else
                    filterbank[m, f] = 0f;
            }
        }

        // Normalise (Slaney normalisation like librosa)
        for (int m = 0; m < nMels; m++)
        {
            float enorm = 2.0f / (MelToHz(melPoints[m + 2]) - MelToHz(melPoints[m]));
            for (int f = 0; f < freqBins; f++)
                filterbank[m, f] *= enorm;
        }

        return filterbank;
    }

    static float[,] ApplyMelFilterbank(float[,] powerSpec, float[,] melFilters)
    {
        int nMels = melFilters.GetLength(0);
        int freqBins = melFilters.GetLength(1);
        int numFrames = powerSpec.GetLength(1);

        float[,] melSpec = new float[nMels, numFrames];

        for (int m = 0; m < nMels; m++)
        {
            for (int t = 0; t < numFrames; t++)
            {
                float sum = 0f;
                for (int f = 0; f < freqBins; f++)
                    sum += melFilters[m, f] * powerSpec[f, t];
                melSpec[m, t] = sum;
            }
        }

        return melSpec;
    }

    static void LogScale(float[,] melSpec)
    {
        int rows = melSpec.GetLength(0);
        int cols = melSpec.GetLength(1);
        float floor = 1e-10f;

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                melSpec[r, c] = Mathf.Log(Mathf.Max(melSpec[r, c], floor));
    }

    static float[,] ApplyDCT(float[,] melSpec, int nMfcc)
    {
        int nMels = melSpec.GetLength(0);
        int numFrames = melSpec.GetLength(1);
        float[,] mfcc = new float[nMfcc, numFrames];

        
        for (int k = 0; k < nMfcc; k++)
        {
            for (int t = 0; t < numFrames; t++)
            {
                float sum = 0f;
                for (int n = 0; n < nMels; n++)
                {
                    sum += melSpec[n, t] * Mathf.Cos(
                        Mathf.PI * k * (2 * n + 1) / (2f * nMels));
                }
                mfcc[k, t] = sum;
            }
        }

        return mfcc;
    }

    // utility funtions here:

    static float HzToMel(float hz)
    {
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    static float MelToHz(float mel)
    {
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }

    static float[] HannWindow(int size)
    {
        float[] w = new float[size];
        for (int i = 0; i < size; i++)
            w[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (size - 1)));
        return w;
    }

    /// <summary>
    /// In-place radix-2 Cooley-Tukey FFT. Length must be power of 2.
    /// </summary>
    static void FFT(float[] real, float[] imag)
    {
        int n = real.Length;

        // Bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            while ((j & bit) != 0) { j ^= bit; bit >>= 1; }
            j ^= bit;
            if (i < j)
            {
                float tmp = real[i]; real[i] = real[j]; real[j] = tmp;
                tmp = imag[i]; imag[i] = imag[j]; imag[j] = tmp;
            }
        }

        // Butterfly stages
        for (int len = 2; len <= n; len *= 2)
        {
            float angle = -2f * Mathf.PI / len;
            float wRe = Mathf.Cos(angle);
            float wIm = Mathf.Sin(angle);

            for (int i = 0; i < n; i += len)
            {
                float curRe = 1f, curIm = 0f;
                for (int j = 0; j < len / 2; j++)
                {
                    int u = i + j;
                    int v = i + j + len / 2;

                    float tRe = curRe * real[v] - curIm * imag[v];
                    float tIm = curRe * imag[v] + curIm * real[v];

                    real[v] = real[u] - tRe;
                    imag[v] = imag[u] - tIm;
                    real[u] += tRe;
                    imag[u] += tIm;

                    float newRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = newRe;
                }
            }
        }
    }
}