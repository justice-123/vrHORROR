using System;
using System.Collections;
using UnityEngine;
using Unity.InferenceEngine;

public class BreathInputML : MonoBehaviour
{
    [Header("Microphone")]
    public string deviceName = null;
    public int sampleRate = 48000;

    [Header("Model")]
    public ModelAsset modelAsset;
    public BackendType backend = BackendType.CPU;

    [Header("Analysis")]
    public float clipDuration = 0.5f;
    public float classifyInterval = 0.25f;

    [Header("MFCC Parameters (must match training)")]
    public int nMfcc = 13;
    public int nFft = 2048;
    public int hopLength = 512;
    public int expectedTimeFrames = 47;

    [Header("ML Confidence Gate")]
    [Range(0f, 1f)]
    public float confidenceThreshold = 0.95f;

    [Header("RMS Gate (ML path only)")]
    [Tooltip("ML inhale rejected if RMS exceeds this (likely speech)")]
    public float maxInhaleRms = 0.02f;

    [Header("Pitch Detection (loud breath path)")]
    [Tooltip("Lowest voice pitch to search for (Hz)")]
    public float minPitchHz = 70f;
    [Tooltip("Highest voice pitch to search for (Hz)")]
    public float maxPitchHz = 400f;
    [Tooltip("Autocorrelation peak below this = no pitch = breath")]
    [Range(0f, 1f)]
    public float pitchConfidenceThreshold = 0.4f;
    [Tooltip("RMS must be above this for pitch path to activate")]
    public float minLoudBreathRms = 0.005f;

    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    [Header("Outputs - Read Only")]
    public string detectedClass = "none";
    public string detectionPath = "none";
    public float inhaleConfidence;
    public float exhaleConfidence;
    public float silenceConfidence;
    public bool isBreathing;
    [Range(0f, 1f)]
    public float breathIntensity01;
    public float debugRms;
    public float debugPitchConfidence;

    private Worker worker;
    private AudioClip micClip;
    private float[] audioBuffer;
    private int clipSamples;
    private float classifyClock;
    private float smoothedIntensity;

    IEnumerator Start()
    {
#if UNITY_ANDROID
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return null;
        }
#endif
        clipSamples = (int)(sampleRate * clipDuration);
        audioBuffer = new float[clipSamples];

        if (modelAsset == null)
        {
            Debug.LogError("BreathInputML: No model assigned!");
            yield break;
        }

        Model runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backend);

        micClip = Microphone.Start(deviceName, true, 2, sampleRate);
        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;
    }

    void OnDisable()
    {
        if (Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
        worker?.Dispose();
    }

    void Update()
    {
        if (micClip == null || worker == null) return;

        classifyClock += Time.deltaTime;
        if (classifyClock < classifyInterval) return;
        classifyClock = 0f;

        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < clipSamples) return;

        int start = micPos - clipSamples;
        if (start < 0) start += micClip.samples;
        micClip.GetData(audioBuffer, start);

        // RMS
        double rmsSum = 0;
        for (int i = 0; i < audioBuffer.Length; i++)
            rmsSum += audioBuffer[i] * audioBuffer[i];
        float rms = Mathf.Sqrt((float)(rmsSum / audioBuffer.Length));
        debugRms = rms;

        // =============================================
        // PATH A: ML model (quiet-to-moderate sounds)
        // =============================================
        float[,] mfcc = MFCCExtractor.ComputeMFCC(
            audioBuffer, sampleRate, nMfcc, nFft, hopLength);

        int timeFrames = mfcc.GetLength(1);
        float[] tensorData = new float[nMfcc * expectedTimeFrames];
        int tMax = Mathf.Min(timeFrames, expectedTimeFrames);
        for (int m = 0; m < nMfcc; m++)
            for (int t = 0; t < tMax; t++)
                tensorData[m * expectedTimeFrames + t] = mfcc[m, t];

        TensorShape shape = new TensorShape(1, nMfcc, expectedTimeFrames);
        Tensor<float> inputTensor = new Tensor<float>(shape, tensorData);
        worker.Schedule(inputTensor);

        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
        outputTensor.ReadbackAndClone();

        float logitInhale = outputTensor[0, 0];
        float logitExhale = outputTensor[0, 1];
        float logitSilence = outputTensor[0, 2];

        float maxLogit = Mathf.Max(logitInhale, Mathf.Max(logitExhale, logitSilence));
        float expInhale = Mathf.Exp(logitInhale - maxLogit);
        float expExhale = Mathf.Exp(logitExhale - maxLogit);
        float expSilence = Mathf.Exp(logitSilence - maxLogit);
        float expSum = expInhale + expExhale + expSilence;

        inhaleConfidence = expInhale / expSum;
        exhaleConfidence = expExhale / expSum;
        silenceConfidence = expSilence / expSum;

        bool mlSaysInhale = inhaleConfidence >= exhaleConfidence
            && inhaleConfidence >= silenceConfidence
            && inhaleConfidence >= confidenceThreshold
            && rms <= maxInhaleRms;

        // =============================================
        // PATH B: Pitch detection (loud sounds only)
        // =============================================
        bool pitchSaysBreath = false;
        debugPitchConfidence = 0f;

        if (rms > maxInhaleRms && rms >= minLoudBreathRms)
        {
            float pitchConf;
            float pitchHz;
            DetectPitch(audioBuffer, sampleRate, minPitchHz, maxPitchHz,
                        out pitchConf, out pitchHz);
            debugPitchConfidence = pitchConf;

            // No pitch detected = not speech = loud breath
            pitchSaysBreath = pitchConf < pitchConfidenceThreshold;
        }

        // =============================================
        // Combine: either path can confirm a breath
        // =============================================
        if (mlSaysInhale)
        {
            detectedClass = "inhale";
            detectionPath = "ML";
            isBreathing = true;
        }
        else if (pitchSaysBreath)
        {
            detectedClass = "inhale";
            detectionPath = "pitch";
            isBreathing = true;
        }
        else
        {
            detectedClass = (exhaleConfidence > silenceConfidence) ? "exhale" : "silence";
            detectionPath = "none";
            isBreathing = false;
        }

        // Intensity: baseline 0.5 + RMS boost
        float targetIntensity = 0f;
        if (isBreathing)
        {
            float rmsBoost = Mathf.Clamp01(rms * 200f);
            targetIntensity = Mathf.Lerp(0.5f, 1f, rmsBoost);
        }
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        breathIntensity01 = smoothedIntensity;

        inputTensor.Dispose();
        outputTensor.Dispose();
    }

    // Pitch detection via normalized autocorrelation
    static void DetectPitch(float[] data, int sr, float minHz, float maxHz,
                            out float confidence, out float pitchHz)
    {
        int minLag = Mathf.Max(1, (int)(sr / maxHz));
        int maxLag = Mathf.Min(data.Length / 2, (int)(sr / minHz));

        if (minLag >= maxLag)
        {
            confidence = 0f;
            pitchHz = 0f;
            return;
        }

        float bestCorr = -1f;
        int bestLag = minLag;
        int n = data.Length;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            double sum = 0;
            double energy1 = 0;
            double energy2 = 0;
            int count = n - lag;

            for (int i = 0; i < count; i++)
            {
                sum += data[i] * data[i + lag];
                energy1 += data[i] * data[i];
                energy2 += data[i + lag] * data[i + lag];
            }

            double denom = Math.Sqrt(energy1 * energy2);
            float corr = (denom > 0) ? (float)(sum / denom) : 0f;

            if (corr > bestCorr)
            {
                bestCorr = corr;
                bestLag = lag;
            }
        }

        confidence = Mathf.Clamp01(bestCorr);
        pitchHz = (bestCorr > 0.2f) ? (float)sr / bestLag : 0f;
    }
}