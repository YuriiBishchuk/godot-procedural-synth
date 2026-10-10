using System;

namespace GodotProceduralSynth;

/// <summary>
/// Preset sound effect types for rapid procedural sound design.
/// </summary>
public enum SfxType
{
    UiClick,
    Laser,
    Explosion,
    Jump,
    PowerUp,
    Coin,
    Hit,
    Whoosh
}

/// <summary>
/// Parameters for parameterized procedural sound effect generation.
/// </summary>
public struct SfxParameters
{
    public double StartFreq;
    public double EndFreq;
    public double Duration;
    public double NoiseMix;
    public double Distortion;
    public double DecayRate;

    public static SfxParameters Laser => new()
    {
        StartFreq = 1200.0,
        EndFreq = 220.0,
        Duration = 0.12,
        NoiseMix = 0.05,
        Distortion = 0.2,
        DecayRate = 25.0
    };

    public static SfxParameters Explosion => new()
    {
        StartFreq = 220.0,
        EndFreq = 40.0,
        Duration = 0.35,
        NoiseMix = 0.75,
        Distortion = 0.6,
        DecayRate = 8.0
    };

    public static SfxParameters UiClick => new()
    {
        StartFreq = 800.0,
        EndFreq = 400.0,
        Duration = 0.035,
        NoiseMix = 0.0,
        Distortion = 0.0,
        DecayRate = 80.0
    };

    public static SfxParameters Jump => new()
    {
        StartFreq = 180.0,
        EndFreq = 540.0,
        Duration = 0.14,
        NoiseMix = 0.02,
        Distortion = 0.1,
        DecayRate = 18.0
    };

    public static SfxParameters PowerUp => new()
    {
        StartFreq = 330.0,
        EndFreq = 880.0,
        Duration = 0.25,
        NoiseMix = 0.0,
        Distortion = 0.15,
        DecayRate = 10.0
    };

    public static SfxParameters Coin => new()
    {
        StartFreq = 987.77, // B5
        EndFreq = 1318.51,  // E6
        Duration = 0.18,
        NoiseMix = 0.0,
        Distortion = 0.0,
        DecayRate = 16.0
    };

    public static SfxParameters Hit => new()
    {
        StartFreq = 300.0,
        EndFreq = 80.0,
        Duration = 0.09,
        NoiseMix = 0.4,
        Distortion = 0.45,
        DecayRate = 35.0
    };

    public static SfxParameters Whoosh => new()
    {
        StartFreq = 400.0,
        EndFreq = 150.0,
        Duration = 0.18,
        NoiseMix = 0.85,
        Distortion = 0.1,
        DecayRate = 12.0
    };
}

/// <summary>
/// Universal procedural sound effect generator.
/// Synthesizes instant retro, arcade, and modern sound effects with zero audio files.
/// </summary>
public static class ProceduralSfx
{
    private const int SampleRate = UniversalSynthEngine.SampleRate;

    /// <summary>
    /// Generates a sound effect by preset type.
    /// Returns mono float samples normalized to [-1.0, 1.0].
    /// </summary>
    public static float[] Generate(SfxType type)
    {
        return type switch
        {
            SfxType.UiClick => GenerateCustom(SfxParameters.UiClick),
            SfxType.Laser => GenerateCustom(SfxParameters.Laser),
            SfxType.Explosion => GenerateCustom(SfxParameters.Explosion),
            SfxType.Jump => GenerateCustom(SfxParameters.Jump),
            SfxType.PowerUp => GenerateCustom(SfxParameters.PowerUp),
            SfxType.Coin => GenerateCustom(SfxParameters.Coin),
            SfxType.Hit => GenerateCustom(SfxParameters.Hit),
            SfxType.Whoosh => GenerateCustom(SfxParameters.Whoosh),
            _ => GenerateCustom(SfxParameters.UiClick)
        };
    }

    /// <summary>
    /// Synthesizes custom parameterized sound effects on the fly.
    /// </summary>
    public static float[] GenerateCustom(SfxParameters p)
    {
        int sampleCount = (int)(p.Duration * SampleRate);
        if (sampleCount <= 0) sampleCount = 1;

        float[] buffer = new float[sampleCount];
        double phase = 0.0;
        Random rng = new(42);

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / SampleRate;
            double progress = t / p.Duration;

            // Exponential frequency sweep
            double freq = p.StartFreq + (p.EndFreq - p.StartFreq) * Math.Pow(progress, 0.8);
            phase += 2.0 * Math.PI * freq / SampleRate;

            // Waveform oscillator (sine + slight square harmonic)
            double osc = Math.Sin(phase) * 0.75 + Math.Sign(Math.Sin(phase)) * 0.25;

            // Noise component
            double noise = (rng.NextDouble() * 2.0 - 1.0);

            // Envelope
            double env = Math.Exp(-t * p.DecayRate);

            // Mix
            double mixed = (osc * (1.0 - p.NoiseMix) + noise * p.NoiseMix) * env;

            // Drive distortion
            if (p.Distortion > 0.0)
            {
                mixed = Math.Tanh(mixed * (1.0 + p.Distortion * 3.0));
            }

            buffer[i] = Math.Clamp((float)mixed, -1.0f, 1.0f);
        }

        return buffer;
    }
}
