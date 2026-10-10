using System;
using System.Runtime.InteropServices.JavaScript;
using GodotProceduralSynth;

Console.WriteLine("GodotProceduralSynth WebAssembly Runtime initialized!");

public partial class WasmAudioBridge
{
    private static readonly UniversalSynthEngine Engine = new();

    [JSExport]
    public static byte[] RenderAestheticBytes(
        int count,
        double tension,
        double apm,
        int intensityLevel,
        bool isDucking,
        double pitchScale)
    {
        float[] left = new float[count];
        float[] right = new float[count];

        var ctx = new AdaptiveAudioContext
        {
            Tension = (float)tension,
            ActivityRate = apm,
            IntensityLevel = intensityLevel,
            IsDucking = isDucking,
            PitchScale = pitchScale
        };

        Engine.GenerateStreamBlock(left, right, count, ctx);

        byte[] bytes = new byte[count * 2 * sizeof(float)];
        float[] interleaved = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            interleaved[i * 2] = left[i];
            interleaved[i * 2 + 1] = right[i];
        }
        Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    [JSExport]
    public static byte[] RenderRecipeBytes(int recipeId, double durationSeconds)
    {
        var catalog = UniversalSynthEngine.GetDefault50Catalog();
        var recipe = Array.Find(catalog, r => r.Id == recipeId) ?? catalog[0];

        var (left, right) = Engine.RenderFromRecipe(recipe with { Duration = durationSeconds });

        int count = left.Length;
        byte[] bytes = new byte[count * 2 * sizeof(float)];
        float[] interleaved = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            interleaved[i * 2] = left[i];
            interleaved[i * 2 + 1] = right[i];
        }
        Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    [JSExport]
    public static byte[] RenderCustomRecipeBytes(
        double bpm,
        double rootFreq,
        int bassPattern,
        int drumPattern,
        int leadStyle,
        double filterCutoff,
        double filterResonance,
        double bassDrive,
        double sidechainDepth,
        int scaleMode,
        double durationSeconds)
    {
        var recipe = new TrackRecipe
        {
            Title = "Custom User Track",
            Category = "UserCustom",
            Bpm = bpm,
            RootFreq = rootFreq,
            BassPattern = (BassPattern)bassPattern,
            DrumPattern = (DrumPattern)drumPattern,
            LeadStyle = (LeadStyle)leadStyle,
            FilterCutoff = filterCutoff,
            FilterResonance = filterResonance,
            BassDrive = bassDrive,
            SidechainDepth = sidechainDepth,
            ScaleMode = scaleMode,
            Duration = durationSeconds
        };

        var (left, right) = Engine.RenderFromRecipe(recipe);

        int count = left.Length;
        byte[] bytes = new byte[count * 2 * sizeof(float)];
        float[] interleaved = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            interleaved[i * 2] = left[i];
            interleaved[i * 2 + 1] = right[i];
        }
        Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    [JSExport]
    public static byte[] GenerateSfxBytes(int sfxType)
    {
        float[] samples = ProceduralSfx.Generate((SfxType)sfxType);
        byte[] bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
