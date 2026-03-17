using System;
using System.Collections;
using UnityEngine;
using Unity.InferenceEngine;

/// <summary>
/// ML-based breath detector using ONNX model via Unity Inference Engine.
///
/// Setup:
/// 1. Train the model using the Python scripts
/// 2. Import the .onnx file into Unity (it becomes a ModelAsset)
/// 3. Drag the ModelAsset into the "model" field on this component
///
/// Requires: com.unity.ai.inference package
/// </summary>
public class BreathInputML : MonoBehaviour
{
    [Header("Microphone")]
    public string deviceName = null;
    public int sampleRate = 48000;

    [Header("Model")]
    [Tooltip("Drag your .onnx ModelAsset here")]
    public ModelAsset modelAsset;

    [Tooltip("Run on CPU or GPU. CPU is safer for Quest.")]
    public BackendType backend = BackendType.CPU;

    [Header("Analysis")]
    [Tooltip("Seconds of audio per classification (must match training)")]
    public float clipDuration = 0.5f;

    [Tooltip("How often to run classification (seconds)")]
    public float classifyInterval = 0.25f;

    [Header("MFCC Parameters (must match training)")]
    public int nMfcc = 13;
    public int nFft = 2048;
    public int hopLength = 512;
    [Tooltip("Expected time frames from MFCC extraction (printed during training)")]
    public int expectedTimeFrames = 47;

    [Header("Confidence")]
    [Tooltip("Minimum softmax probability to accept a classification")]
    [Range(0f, 1f)]
    public float confidenceThreshold = 1f;

    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    [Header("Outputs - Read Only")]
    public string detectedClass = "none";
    public float inhaleConfidence;
    public float exhaleConfidence;
    public float silenceConfidence;
    public bool isBreathing;
    [Range(0f, 1f)]
    public float breathIntensity01;
    public float debugRms;

    // Inference Engine
    private Worker worker;

    // Audio
    private AudioClip micClip;
    private float[] audioBuffer;
    private int clipSamples;
    private float classifyClock;
    private float smoothedIntensity;

    private const int CLASS_INHALE = 0;
    private const int CLASS_EXHALE = 1;
    private const int CLASS_SILENCE = 2;

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

        // Load model and create worker
        Model runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, backend);
        Debug.Log("BreathInputML: Model loaded");

        // Start mic
        micClip = Microphone.Start(deviceName, true, 2, sampleRate);
        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;

        Debug.Log("BreathInputML: Mic started");
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

        // Grab audio
        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < clipSamples) return;

        int start = micPos - clipSamples;
        if (start < 0) start += micClip.samples;
        micClip.GetData(audioBuffer, start);

        // Compute RMS for intensity
        double rmsSum = 0;
        for (int i = 0; i < audioBuffer.Length; i++)
            rmsSum += audioBuffer[i] * audioBuffer[i];
        float rms = Mathf.Sqrt((float)(rmsSum / audioBuffer.Length));
        debugRms = rms;

        // Extract MFCC features
        float[,] mfcc = MFCCExtractor.ComputeMFCC(
            audioBuffer, sampleRate, nMfcc, nFft, hopLength);

        int timeFrames = mfcc.GetLength(1);

        // Pad or truncate to expected size
        float[] tensorData = new float[nMfcc * expectedTimeFrames];
        int tMax = Mathf.Min(timeFrames, expectedTimeFrames);
        for (int m = 0; m < nMfcc; m++)
            for (int t = 0; t < tMax; t++)
                tensorData[m * expectedTimeFrames + t] = mfcc[m, t];

        // Create input tensor: shape (1, nMfcc, expectedTimeFrames)
        TensorShape shape = new TensorShape(1, nMfcc, expectedTimeFrames);
        Tensor<float> inputTensor = new Tensor<float>(shape, tensorData);

        // Run inference
        worker.Schedule(inputTensor);

        // Get output
        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
        outputTensor.ReadbackAndClone();

        // Apply softmax to get probabilities
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

        // Classify - only inhales count as breathing now
        if (inhaleConfidence >= exhaleConfidence && inhaleConfidence >= silenceConfidence
            && inhaleConfidence >= confidenceThreshold)
        {
            detectedClass = "inhale";
            isBreathing = true;
        }
        else
        {
            detectedClass = (exhaleConfidence > silenceConfidence) ? "exhale" : "silence";
            isBreathing = false;
        }

        // Intensity from inhale confidence + RMS boost
        // Baseline of 0.5 ensures even quiet inhales are clearly visible
        float targetIntensity = 0f;
        if (isBreathing)
        {
            float rmsBoost = Mathf.Clamp01(rms * 200f); // 0-1 from RMS
            targetIntensity = Mathf.Lerp(0.5f, 1f, rmsBoost); // 0.5 minimum, louder = closer to 1
        }
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        breathIntensity01 = smoothedIntensity;

        // Clean up
        inputTensor.Dispose();
        outputTensor.Dispose();
    }
}