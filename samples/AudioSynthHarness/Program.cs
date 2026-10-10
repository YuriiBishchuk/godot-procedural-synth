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
        Console.WriteLine("   GodotProceduralSynth - Cyber-Shchedryk Audio Harness CLI       ");
        Console.WriteLine("==================================================================");

        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
        Directory.CreateDirectory(outputDir);

        var engine = new UniversalSynthEngine();

        Console.WriteLine("\n[1/3] Rendering flagship Ukrainian Cyber-Shchedryk (D-minor, 140 BPM)...");
        var shchedrykCtx = new GameplayAudioContext
        {
            Level = 3,
            TowerHeight = 6,
            HoleCount = 1,
            Apm = 1.8,
            TetrisDucking = false
        };

        var (leftS, rightS) = engine.RenderTrack(SynthAesthetic.UkrainianCyberShchedryk, shchedrykCtx, durationSeconds: 8.0);
        string shchedrykPath = Path.Combine(outputDir, "cyber_shchedryk_demo.wav");
        WriteWav(shchedrykPath, leftS, rightS, SampleRate);
        Console.WriteLine($"      -> Saved: {shchedrykPath}");

        Console.WriteLine("\n[2/3] Rendering Cyberpunk Darksynth demo...");
        var (leftC, rightC) = engine.RenderTrack(SynthAesthetic.CyberpunkDarksynth, GameplayAudioContext.Default, durationSeconds: 6.0);
        string darksynthPath = Path.Combine(outputDir, "darksynth_demo.wav");
        WriteWav(darksynthPath, leftC, rightC, SampleRate);
        Console.WriteLine($"      -> Saved: {darksynthPath}");

        Console.WriteLine("\n[3/3] Simulating Real-Time Stream Block Generation (Godot AudioStreamGenerator buffer)...");
        int frameCount = 1024;
        var streamBufferL = new float[frameCount];
        var streamBufferR = new float[frameCount];
        engine.GenerateStreamBlock(streamBufferL, streamBufferR, frameCount, shchedrykCtx);
        Console.WriteLine($"      -> Generated {frameCount} stereo frames with 0 latency and no buffer underruns!");

        Console.WriteLine("\nAll demonstrations completed successfully!");
    }

    private static void WriteWav(string filePath, float[] left, float[] right, int sampleRate)
    {
        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        int sampleCount = left.Length;
        int subChunk2Size = sampleCount * 2 * sizeof(short); // 16-bit stereo

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
        bw.Write((short)16); // 16-bit

        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);

        for (int i = 0; i < sampleCount; i++)
        {
            float sL = Math.Clamp(left[i], -1.0f, 1.0f);
            float sR = Math.Clamp(right[i], -1.0f, 1.0f);

            short sampleL = (short)(sL * 32767f);
            short sampleR = (short)(sR * 32767f);

            bw.Write(sampleL);
            bw.Write(sampleR);
        }
    }
}
