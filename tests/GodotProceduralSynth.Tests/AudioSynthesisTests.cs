using System;
using Xunit;
using GodotProceduralSynth;

namespace GodotProceduralSynth.Tests;

public class AudioSynthesisTests
{
    [Fact]
    public void ProceduralEngine_GeneratesValidAudioBuffers()
    {
        var engine = new UniversalSynthEngine();
        var ctx = GameplayAudioContext.Default;
        int frameCount = 1024;
        var left = new float[frameCount];
        var right = new float[frameCount];

        engine.GenerateStreamBlock(left, right, frameCount, ctx);

        for (int i = 0; i < frameCount; i++)
        {
            Assert.False(float.IsNaN(left[i]), $"NaN at frame {i} in left channel");
            Assert.False(float.IsNaN(right[i]), $"NaN at frame {i} in right channel");
            Assert.InRange(left[i], -1.0f, 1.0f);
            Assert.InRange(right[i], -1.0f, 1.0f);
        }
    }

    [Fact]
    public void NoDigitalClipping_HeadroomPreserved()
    {
        var engine = new UniversalSynthEngine();
        var ctx = GameplayAudioContext.Default;
        var (left, right) = engine.RenderTrack(SynthAesthetic.UkrainianCyberShchedryk, ctx, 2.0);

        float peak = 0.0f;
        for (int i = 0; i < left.Length; i++)
        {
            peak = MathF.Max(peak, MathF.Max(MathF.Abs(left[i]), MathF.Abs(right[i])));
        }

        Assert.True(peak <= 1.0f, $"Procedural Shchedryk exceeds 0 dBFS peak clipping (peak: {peak:F3})");
        Assert.True(peak >= 0.15f, $"Procedural Shchedryk is too quiet / inaudible (peak: {peak:F3})");
    }

    [Fact]
    public void MosquitoBuzzSpectralRatio_MustBeBelowThreshold()
    {
        var engine = new UniversalSynthEngine();
        var ctx = GameplayAudioContext.Default;
        var (samples, _) = engine.RenderTrack(SynthAesthetic.UkrainianCyberShchedryk, ctx, 3.0);
        int sampleRate = UniversalSynthEngine.SampleRate;

        float alphaHp = 1.0f / (1.0f + 2.0f * MathF.PI * 4500.0f / sampleRate);
        float alphaLp = 2.0f * MathF.PI * 2500.0f / (sampleRate + 2.0f * MathF.PI * 2500.0f);

        float prevX = samples[0];
        float prevHp = 0.0f;
        float prevLp = 0.0f;

        double energyHigh = 0.0;
        double energyLow = 0.0;

        foreach (float s in samples)
        {
            float hp = alphaHp * (prevHp + s - prevX);
            prevHp = hp;
            prevX = s;
            energyHigh += hp * hp;

            float lp = prevLp + alphaLp * (s - prevLp);
            prevLp = lp;
            energyLow += lp * lp;
        }

        double harshnessRatio = energyHigh / (energyLow + 1e-9);
        Assert.True(harshnessRatio < 0.15,
            $"Cyber-Shchedryk failed mosquito test: harshness ratio is {harshnessRatio:F3} (threshold < 0.15).");
    }

    [Fact]
    public void LowEndBassAndKickEnergy_MustBeProminent()
    {
        var engine = new UniversalSynthEngine();
        var ctx = GameplayAudioContext.Default;
        var (samples, _) = engine.RenderTrack(SynthAesthetic.UkrainianCyberShchedryk, ctx, 3.0);
        int sampleRate = UniversalSynthEngine.SampleRate;

        float alphaLp = 2.0f * MathF.PI * 250.0f / (sampleRate + 2.0f * MathF.PI * 250.0f);
        float prevLp = 0.0f;

        double lowEnergy = 0.0;
        double totalEnergy = 0.0;

        foreach (float s in samples)
        {
            float lp = prevLp + alphaLp * (s - prevLp);
            prevLp = lp;
            lowEnergy += lp * lp;
            totalEnergy += s * s;
        }

        double lowPercentage = lowEnergy / (totalEnergy + 1e-9);
        Assert.True(lowPercentage >= 0.30,
            $"Cyber-Shchedryk lacks low-end body: only {lowPercentage * 100:F1}% energy below 250 Hz (expected >= 30%).");
    }

    [Fact]
    public void SeamlessLoopBoundaryContinuity_NoPopOnClick()
    {
        var engine = new UniversalSynthEngine();
        var ctx = GameplayAudioContext.Default;
        int blockSize = 512;
        var left1 = new float[blockSize];
        var right1 = new float[blockSize];
        var left2 = new float[blockSize];
        var right2 = new float[blockSize];

        engine.GenerateStreamBlock(left1, right1, blockSize, ctx);
        engine.GenerateStreamBlock(left2, right2, blockSize, ctx);

        float stepDiff = MathF.Abs(left2[0] - left1[^1]);
        Assert.True(stepDiff < 0.25f,
            $"Discontinuous block boundary (end: {left1[^1]:F3}, next start: {left2[0]:F3}, diff: {stepDiff:F3}). Will pop!");
    }

    [Fact]
    public void UniversalSynthEngine_RendersAllAestheticsWithoutClippingOrNaN()
    {
        var engine = new UniversalSynthEngine();
        var aesthetics = new[]
        {
            SynthAesthetic.CyberpunkDarksynth,
            SynthAesthetic.IndustrialMidTempo,
            SynthAesthetic.UkrainianCyberShchedryk
        };

        foreach (var aesthetic in aesthetics)
        {
            var ctx = GameplayAudioContext.Default;
            var (left, right) = engine.RenderTrack(aesthetic, ctx, durationSeconds: 2.0);

            Assert.Equal(left.Length, right.Length);
            Assert.True(left.Length > 0);

            float maxL = 0f, maxR = 0f;
            for (int i = 0; i < left.Length; i++)
            {
                Assert.False(float.IsNaN(left[i]), $"NaN detected in left channel of {aesthetic}");
                Assert.False(float.IsNaN(right[i]), $"NaN detected in right channel of {aesthetic}");

                maxL = MathF.Max(maxL, MathF.Abs(left[i]));
                maxR = MathF.Max(maxR, MathF.Abs(right[i]));

                Assert.True(MathF.Abs(left[i]) <= 1.0f, $"Hard clipping detected in left channel of {aesthetic}");
                Assert.True(MathF.Abs(right[i]) <= 1.0f, $"Hard clipping detected in right channel of {aesthetic}");
            }

            Assert.True(maxL > 0.1f, $"Aesthetic {aesthetic} produced silent left channel");
            Assert.True(maxR > 0.1f, $"Aesthetic {aesthetic} produced silent right channel");
        }
    }

    [Fact]
    public void UniversalSynthEngine_HasZeroSpeakerOverloadHarshness()
    {
        var engine = new UniversalSynthEngine();
        var ctx = new GameplayAudioContext
        {
            Level = 8,
            TowerHeight = 15,
            HoleCount = 5,
            Apm = 4.0,
            TetrisDucking = false
        };

        var (left, _) = engine.RenderTrack(SynthAesthetic.CyberpunkDarksynth, ctx, 3.0);
        var metrics = DspAnalyzer.AnalyzeDsp(left, UniversalSynthEngine.SampleRate);

        Assert.True(metrics.Peak <= 0.99f, $"Peak exceeds headroom: {metrics.Peak:F2}");
        Assert.True(metrics.Harshness < 0.05f, $"Harshness too high (potential speaker rattle): {metrics.Harshness:F4}");
        Assert.True(metrics.BassRatio >= 0.50f, $"Bass body is too weak: {metrics.BassRatio * 100:F1}%");
    }

    [Fact]
    public void Catalog50_AllRecipesRenderSuccessfully()
    {
        var engine = new UniversalSynthEngine();
        var catalog = UniversalSynthEngine.GetDefault50Catalog();

        Assert.Equal(50, catalog.Length);

        // Spot check track 1, track 25, track 41
        foreach (int id in new[] { 1, 25, 41 })
        {
            var recipe = Array.Find(catalog, r => r.Id == id);
            Assert.NotNull(recipe);

            var (left, right) = engine.RenderFromRecipe(recipe!);
            Assert.True(left.Length > 0);
            Assert.Equal(left.Length, right.Length);
        }
    }

    [Fact]
    public void ProceduralSfx_AllPresetsGenerateValidAudioWithoutClipping()
    {
        var presets = Enum.GetValues<SfxType>();
        foreach (var type in presets)
        {
            var samples = ProceduralSfx.Generate(type);
            Assert.True(samples.Length > 0, $"SFX {type} produced empty buffer");

            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.False(float.IsNaN(samples[i]), $"NaN in SFX {type} at index {i}");
                peak = MathF.Max(peak, MathF.Abs(samples[i]));
                Assert.InRange(samples[i], -1.0f, 1.0f);
            }

            Assert.True(peak > 0.05f, $"SFX {type} was too quiet (peak: {peak})");
            Assert.True(peak <= 1.0f, $"SFX {type} clipped beyond 1.0 (peak: {peak})");
        }
    }

    [Fact]
    public void AdaptiveAudioContext_GeneratesCleanStreamBlocks()
    {
        var engine = new UniversalSynthEngine();
        var ctx = new AdaptiveAudioContext
        {
            IntensityLevel = 5,
            Tension = 0.75f,
            Dissonance = 0.3f,
            ActivityRate = 2.5,
            IsDucking = false,
            PitchScale = 1.0
        };

        var left = new float[1024];
        var right = new float[1024];
        engine.GenerateStreamBlock(left, right, 1024, ctx);

        for (int i = 0; i < 1024; i++)
        {
            Assert.False(float.IsNaN(left[i]));
            Assert.False(float.IsNaN(right[i]));
            Assert.InRange(left[i], -1.0f, 1.0f);
            Assert.InRange(right[i], -1.0f, 1.0f);
        }
    }
}
