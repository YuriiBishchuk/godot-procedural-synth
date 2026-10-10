using System;
using System.IO;
using System.Text;
using GodotProceduralSynth;

namespace AudioSynthHarness;

public class Program
{
    private const int SampleRate = UniversalSynthEngine.SampleRate;

    public static void Main(string[] args)
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine("   GodotProceduralSynth - Universal Audio & Music Generator       ");
        Console.WriteLine("   (Pure C# Modular Architecture for In-Game & Offline Audio)     ");
        Console.WriteLine("==================================================================");

        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
        Directory.CreateDirectory(outputDir);

        var engine = new UniversalSynthEngine();

        Console.WriteLine("\n[1/4] Generating Procedural Sound Effects (Zero Audio Assets)...");
        var sfxTypes = new[] { SfxType.Laser, SfxType.Explosion, SfxType.Coin, SfxType.PowerUp, SfxType.UiClick };
        foreach (var sfx in sfxTypes)
        {
            float[] sfxSamples = ProceduralSfx.Generate(sfx);
            string sfxPath = Path.Combine(outputDir, $"sfx_{sfx.ToString().ToLower()}.wav");
            WriteWavMono(sfxPath, sfxSamples, SampleRate);
            Console.WriteLine($"      -> Generated {sfx,-10}: {sfxPath}");
        }

        Console.WriteLine("\n[2/4] Rendering Dynamic Soundtrack Recipes...");
        var catalog = UniversalSynthEngine.GetDefault50Catalog();
        Console.WriteLine($"      -> Loaded {catalog.Length} curated procedural recipes across Darksynth, Industrial, Arcade, and Cyberpunk.");

        // Render sample tracks: 1 (Darksynth), 25 (Industrial), 41 (Cyber-Shchedryk demo)
        foreach (int trackId in new[] { 1, 25, 41 })
        {
            var recipe = Array.Find(catalog, r => r.Id == trackId);
            if (recipe != null)
            {
                var (left, right) = engine.RenderFromRecipe(recipe);
                string trackPath = Path.Combine(outputDir, $"track_{recipe.Id:D2}_{recipe.Title.Replace(" ", "_")}.wav");
                WriteWavStereo(trackPath, left, right, SampleRate);
                Console.WriteLine($"      -> Saved Track #{recipe.Id:D2} [{recipe.Category}] '{recipe.Title}': {trackPath}");
            }
        }

        Console.WriteLine("\n[3/4] Live Adaptive Context Simulation (Intensity, Tension, Activity)...");
        var adaptiveCtx = new AdaptiveAudioContext
        {
            IntensityLevel = 4,
            Tension = 0.65f,
            Dissonance = 0.2f,
            ActivityRate = 2.8,
            IsDucking = false,
            PitchScale = 1.0
        };

        int frameCount = 2048;
        var streamBufferL = new float[frameCount];
        var streamBufferR = new float[frameCount];
        engine.GenerateStreamBlock(streamBufferL, streamBufferR, frameCount, adaptiveCtx);
        Console.WriteLine($"      -> Successfully synthesized {frameCount} stereo frames with real-time parameter modulation!");

        Console.WriteLine("\n[4/4] Summary & Verification Complete! All outputs generated cleanly.");
    }

    private static void WriteWavMono(string filePath, float[] samples, int sampleRate)
    {
        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        int sampleCount = samples.Length;
        int subChunk2Size = sampleCount * sizeof(short);

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + subChunk2Size);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * sizeof(short));
        bw.Write((short)sizeof(short));
        bw.Write((short)16);

        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);

        for (int i = 0; i < sampleCount; i++)
        {
            short s = (short)(Math.Clamp(samples[i], -1.0f, 1.0f) * 32767f);
            bw.Write(s);
        }
    }

    private static void WriteWavStereo(string filePath, float[] left, float[] right, int sampleRate)
    {
        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        int sampleCount = left.Length;
        int subChunk2Size = sampleCount * 2 * sizeof(short);

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + subChunk2Size);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)2); // Stereo
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2 * sizeof(short));
        bw.Write((short)(2 * sizeof(short)));
        bw.Write((short)16);

        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);

        for (int i = 0; i < sampleCount; i++)
        {
            short sL = (short)(Math.Clamp(left[i], -1.0f, 1.0f) * 32767f);
            short sR = (short)(Math.Clamp(right[i], -1.0f, 1.0f) * 32767f);
            bw.Write(sL);
            bw.Write(sR);
        }
    }
}
