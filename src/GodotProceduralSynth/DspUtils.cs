using System;

namespace GodotProceduralSynth;

public static class DspUtils
{
    /// <summary>
    /// Smooth analog soft saturation.
    /// Never hard-clips: asymptotically reaches +/- 1.0 via hyperbolic tangent.
    /// </summary>
    public static double SoftSat(double x, double drive = 1.0)
    {
        double driven = x * drive;
        return Math.Tanh(driven);
    }

    /// <summary>
    /// PolyBLEP residual function for bandlimited step correction (anti-aliasing).
    /// </summary>
    public static double PolyBlep(double t, double dt)
    {
        if (t < dt)
        {
            t /= dt;
            return t + t - t * t - 1.0;
        }
        else if (t > 1.0 - dt)
        {
            t = (t - 1.0) / dt;
            return t * t + t + t + 1.0;
        }
        return 0.0;
    }

    /// <summary>
    /// Anti-aliased bandlimited Sawtooth wave [ -1.0 .. +1.0 ].
    /// </summary>
    public static double BandlimitedSaw(ref double phase, double freq, int sampleRate)
    {
        double dt = freq / sampleRate;
        phase += dt;
        while (phase >= 1.0) phase -= 1.0;

        double naive = 2.0 * phase - 1.0;
        double blep = PolyBlep(phase, dt);
        return naive - blep;
    }

    /// <summary>
    /// Anti-aliased bandlimited Pulse/Square wave with pulse width [ 0.05 .. 0.95 ].
    /// </summary>
    public static double BandlimitedPulse(ref double phase, double freq, double width, int sampleRate)
    {
        double dt = freq / sampleRate;
        phase += dt;
        while (phase >= 1.0) phase -= 1.0;

        double naive = (phase < width) ? 1.0 : -1.0;
        double blep1 = PolyBlep(phase, dt);
        double phaseShift = (phase + 1.0 - width) % 1.0;
        double blep2 = PolyBlep(phaseShift, dt);

        return naive + blep1 - blep2;
    }

    /// <summary>
    /// Smooth Sine wave.
    /// </summary>
    public static double Sine(ref double phase, double freq, int sampleRate)
    {
        phase += (2.0 * Math.PI * freq) / sampleRate;
        if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
        return Math.Sin(phase);
    }
}

/// <summary>
/// 4-pole resonant Moog ladder low-pass filter (24 dB/octave).
/// Produces warm, silky analog filtering without harsh resonance blowup.
/// </summary>
public class MoogLadderFilter
{
    private double _s1, _s2, _s3, _s4;

    public void Reset()
    {
        _s1 = _s2 = _s3 = _s4 = 0.0;
    }

    public double Process(double input, double cutoffHz, double resonance, int sampleRate)
    {
        // Clamp cutoff to safe bounds
        cutoffHz = Math.Clamp(cutoffHz, 40.0, sampleRate * 0.45);
        resonance = Math.Clamp(resonance, 0.0, 3.8); // 4.0 is self-oscillation

        // Bilinear transform calculation
        double wc = 2.0 * Math.PI * cutoffHz / sampleRate;
        double g = 0.9892 * wc - 0.4342 * (wc * wc) + 0.1381 * (wc * wc * wc);
        g = Math.Clamp(g, 0.001, 0.999);

        // Feedback with soft saturation to prevent blowing out
        double fb = resonance * _s4;
        double u = Math.Tanh(input - fb);

        _s1 += g * (u - _s1);
        _s2 += g * (_s1 - _s2);
        _s3 += g * (_s2 - _s3);
        _s4 += g * (_s3 - _s4);

        return _s4;
    }
}

/// <summary>
/// ADSR Envelope generator with linear attack and exponential decay/release.
/// </summary>
public class AdsrEnvelope
{
    public double AttackSec { get; set; } = 0.01;
    public double DecaySec { get; set; } = 0.15;
    public double SustainLevel { get; set; } = 0.70;
    public double ReleaseSec { get; set; } = 0.10;

    public double Value(double timeSinceNoteOn, double noteDuration)
    {
        if (timeSinceNoteOn < 0.0) return 0.0;

        if (timeSinceNoteOn < noteDuration)
        {
            // Note is held (Attack -> Decay -> Sustain)
            if (timeSinceNoteOn < AttackSec)
            {
                return timeSinceNoteOn / Math.Max(0.001, AttackSec);
            }
            double decayProgress = (timeSinceNoteOn - AttackSec) / Math.Max(0.001, DecaySec);
            if (decayProgress < 1.0)
            {
                return 1.0 - (1.0 - SustainLevel) * Math.Sin(decayProgress * Math.PI * 0.5);
            }
            return SustainLevel;
        }
        else
        {
            // Note is released
            double releaseTime = timeSinceNoteOn - noteDuration;
            if (releaseTime >= ReleaseSec) return 0.0;
            double releaseProgress = releaseTime / Math.Max(0.001, ReleaseSec);
            return SustainLevel * Math.Exp(-releaseProgress * 4.0);
        }
    }
}

public record DspMetrics(float Peak, float CrestDb, float BassRatio, float Harshness);

public static class DspAnalyzer
{
    public static DspMetrics AnalyzeDsp(float[] samples, int sr)
    {
        float peak = 0.0f;
        double sumSq = 0.0;
        foreach (float s in samples)
        {
            float abs = Math.Abs(s);
            if (abs > peak) peak = abs;
            sumSq += s * s;
        }

        float rms = (float)Math.Sqrt(sumSq / samples.Length);
        float crestDb = 20.0f * (float)Math.Log10(peak / (rms + 1e-9f));

        float alphaLp250 = 2.0f * MathF.PI * 250.0f / (sr + 2.0f * MathF.PI * 250.0f);
        float alphaHp4500 = 1.0f / (1.0f + 2.0f * MathF.PI * 4500.0f / sr);
        float alphaLp2500 = 2.0f * MathF.PI * 2500.0f / (sr + 2.0f * MathF.PI * 2500.0f);

        float prevLp250 = 0, prevHp4500 = 0, prevLp2500 = 0, prevX = samples[0];
        double energyBass = 0, energyHigh = 0, energyMid = 0;

        foreach (float s in samples)
        {
            float lp250 = prevLp250 + alphaLp250 * (s - prevLp250);
            prevLp250 = lp250;
            energyBass += lp250 * lp250;

            float hp4500 = alphaHp4500 * (prevHp4500 + s - prevX);
            prevHp4500 = hp4500;
            energyHigh += hp4500 * hp4500;

            float lp2500 = prevLp2500 + alphaLp2500 * (s - prevLp2500);
            prevLp2500 = lp2500;
            energyMid += lp2500 * lp2500;

            prevX = s;
        }

        float bassRatio = (float)(energyBass / (sumSq + 1e-9));
        float harshness = (float)(energyHigh / (energyMid + 1e-9));

        return new DspMetrics(peak, crestDb, bassRatio, harshness);
    }
}
