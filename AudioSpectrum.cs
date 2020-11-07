// Audio spectrum component
// By Keijiro Takahashi, 2013
// https://github.com/keijiro/unity-audio-spectrum
using UnityEngine;
using System.Collections;

public class AudioSpectrum : MonoBehaviour
{
    #region Band type definition
    public enum BandType {
        FourBand,
        FourBandVisual,
        EightBand,
        TenBand,
        TwentySixBand,
        ThirtyOneBand
    };

    private static readonly float[][] MiddleFrequenciesForBands = {
        new[]{ 125.0f, 500, 1000, 2000 },
        new[]{ 250.0f, 400, 600, 800 },
        new[]{ 63.0f, 125, 500, 1000, 2000, 4000, 6000, 8000 },
        new[]{ 31.5f, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 },
        new[]{ 25.0f, 31.5f, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000 },
        new[]{ 20.0f, 25, 31.5f, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000 },
    };

    private static readonly float[] BandwidthForBands = {
        1.414f, // 2^(1/2)
        1.260f, // 2^(1/3)
        1.414f, // 2^(1/2)
        1.414f, // 2^(1/2)
        1.122f, // 2^(1/6)
        1.122f  // 2^(1/6)
    };
    #endregion
    #region Public variables
    public AudioSource audioSource;
    public int numberOfSamples = 1024;
    public BandType bandType = BandType.TenBand;
    public float fallSpeed = 0.08f;
    public float sensibility = 8.0f;
    #endregion

    #region Private variables
    private float[] rawSpectrum;
    private float[] levels;
    private float[] peakLevels;
    private float[] meanLevels;
    private float[] maxLevels;
    AudioSource source;
    private float amplitudeHighest = 0.01f;
    #endregion

    #region Public property
    public float[] Levels => levels;

    public float[] PeakLevels => peakLevels;

    public float[] MeanLevels => meanLevels;

    public float Amplitude { get; set; }
    public float AmplitudeBuffer { get; set; }
    #endregion

    #region Private functions
    void CheckBuffers ()
    {
        if (rawSpectrum == null || rawSpectrum.Length != numberOfSamples) {
            rawSpectrum = new float[numberOfSamples];
        }
        var bandCount = MiddleFrequenciesForBands [(int)bandType].Length;
        if (levels == null || levels.Length != bandCount) {
            levels = new float[bandCount];
            peakLevels = new float[bandCount];
            meanLevels = new float[bandCount];
            maxLevels = new float[bandCount];
            for(int i = 0; i < bandCount; i++)
            {
                maxLevels[i] = 0.01f;
            }
        }
    }

    int FrequencyToSpectrumIndex (float f)
    {
        var i = Mathf.FloorToInt (f / AudioSettings.outputSampleRate * 2.0f * rawSpectrum.Length);
        return Mathf.Clamp (i, 0, rawSpectrum.Length - 1);
    }

    void GetAmplitude()
    {
        float currentAmplitude = 0;
        for(int i = 0; i < levels.Length; i++)
        {
            currentAmplitude += levels[i];
        }
        if (currentAmplitude > amplitudeHighest)
            amplitudeHighest = currentAmplitude;

        Amplitude = currentAmplitude / amplitudeHighest;
    }
    #endregion

    #region Monobehaviour functions
    void Awake ()
    {
        if (audioSource != null) source = audioSource;
        else source = GetComponent<AudioSource>();
        CheckBuffers ();
    }

    void Update ()
    {
        CheckBuffers ();

        source.GetSpectrumData (rawSpectrum, 0, FFTWindow.BlackmanHarris);

        float[] middlefrequencies = MiddleFrequenciesForBands [(int)bandType];
        var bandwidth = BandwidthForBands [(int)bandType];

        var falldown = fallSpeed * Time.deltaTime;
        var filter = Mathf.Exp (-sensibility * Time.deltaTime);

        for (var bi = 0; bi < levels.Length; bi++) {
            int imin = FrequencyToSpectrumIndex (middlefrequencies [bi] / bandwidth);
            int imax = FrequencyToSpectrumIndex (middlefrequencies [bi] * bandwidth);

            var bandAcc = 0.0f;
            for (var fi = imin; fi <= imax; fi++) {
                bandAcc += rawSpectrum [fi];
            }

            maxLevels[bi] = Mathf.Max(maxLevels[bi], bandAcc);
            Debug.Assert(maxLevels[bi] != 0, "Max level is zero");

            levels[bi] = bandAcc / maxLevels[bi];
            peakLevels [bi] = Mathf.Max (peakLevels [bi] - falldown, levels[bi]);
            meanLevels [bi] = bandAcc - (levels[bi] - meanLevels [bi]) * filter;
        }
        GetAmplitude();
        AmplitudeBuffer = Mathf.Max(Amplitude - falldown, Amplitude);
    }
    #endregion
}