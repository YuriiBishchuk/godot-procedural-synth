using Godot;
using System;
using GodotProceduralSynth;

namespace GodotProceduralSynth.Addon;

/// <summary>
/// Custom Godot 4 Node providing high-level procedural audio and dynamic music streaming.
/// Ready to use directly in Godot Scene Trees.
/// </summary>
[GlobalClass]
public partial class ProceduralAudioStreamPlayer : AudioStreamPlayer
{
    private const int SampleRate = UniversalSynthEngine.SampleRate;
    private AudioStreamGeneratorPlayback? _playback;
    private readonly UniversalSynthEngine _engine = new();

    private readonly float[] _bufferL = new float[1024];
    private readonly float[] _bufferR = new float[1024];

    [Export]
    public bool AutoStartMusic { get; set; } = false;

    [Export]
    public SynthAesthetic InitialAesthetic { get; set; } = SynthAesthetic.CyberpunkDarksynth;

    [Export(PropertyHint.Range, "1,20,1")]
    public int IntensityLevel { get; set; } = 1;

    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float Tension { get; set; } = 0.1f;

    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float Dissonance { get; set; } = 0.0f;

    [Export(PropertyHint.Range, "0.5,10.0,0.1")]
    public double ActivityRate { get; set; } = 1.0;

    [Export]
    public bool IsDucking { get; set; } = false;

    [Export(PropertyHint.Range, "0.0,1.5,0.05")]
    public double PitchScaleFactor { get; set; } = 1.0;

    public override void _Ready()
    {
        if (Stream == null || Stream is not AudioStreamGenerator)
        {
            Stream = new AudioStreamGenerator
            {
                MixRate = SampleRate,
                BufferLength = 0.15f
            };
        }

        if (AutoStartMusic)
        {
            StartMusic();
        }
    }

    public void StartMusic()
    {
        Play();
        _playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();
    }

    public override void _Process(double delta)
    {
        if (_playback == null || !Playing)
            return;

        int available = _playback.GetFramesAvailable();
        if (available <= 0)
            return;

        int count = Math.Min(available, _bufferL.Length);

        var context = new AdaptiveAudioContext
        {
            IntensityLevel = IntensityLevel,
            Tension = Tension,
            Dissonance = Dissonance,
            ActivityRate = ActivityRate,
            IsDucking = IsDucking,
            PitchScale = PitchScaleFactor
        };

        _engine.GenerateStreamBlock(_bufferL, _bufferR, count, context);

        for (int i = 0; i < count; i++)
        {
            _playback.PushFrame(new Vector2(_bufferL[i], _bufferR[i]));
        }
    }

    /// <summary>
    /// Instantly synthesizes and plays a procedural sound effect without loading any audio files.
    /// </summary>
    public void PlayProceduralSfx(SfxType type)
    {
        float[] samples = ProceduralSfx.Generate(type);
        // Play one-shot via temporary AudioStreamWav or AudioStreamGenerator
        var sfxPlayer = new AudioStreamPlayer
        {
            Bus = Bus
        };

        var generator = new AudioStreamGenerator
        {
            MixRate = SampleRate,
            BufferLength = (float)(samples.Length / (double)SampleRate) + 0.05f
        };

        sfxPlayer.Stream = generator;
        GetParent().AddChild(sfxPlayer);
        sfxPlayer.Play();

        var playback = (AudioStreamGeneratorPlayback)sfxPlayer.GetStreamPlayback();
        for (int i = 0; i < samples.Length; i++)
        {
            playback.PushFrame(new Vector2(samples[i], samples[i]));
        }

        sfxPlayer.Finished += () => sfxPlayer.QueueFree();
    }
}
