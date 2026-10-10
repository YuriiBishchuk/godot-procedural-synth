using Godot;
using System;
using GodotProceduralSynth;

namespace MyGodotGame;

/// <summary>
/// Universal Godot 4.x C# Node integrating GodotProceduralSynth with AudioStreamGenerator.
/// Synthesizes dynamic adaptive music in 100% real-time on Godot's audio thread.
/// </summary>
public partial class AdaptiveMusicEngine : Node
{
    public static AdaptiveMusicEngine Instance { get; private set; } = null!;

    private const int SampleRate = UniversalSynthEngine.SampleRate; // 44100 Hz
    private const float GeneratorBufferSeconds = 0.15f; // 150ms buffer for glitch-free streaming

    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback _playback = null!;
    private readonly UniversalSynthEngine _synthEngine = new();

    // Reusable transfer buffers
    private readonly float[] _bufferL = new float[2048];
    private readonly float[] _bufferR = new float[2048];

    // Current game state context
    public AdaptiveAudioContext Context { get; set; } = AdaptiveAudioContext.Default;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        var generator = new AudioStreamGenerator
        {
            MixRate = SampleRate,
            BufferLength = GeneratorBufferSeconds
        };

        _player = new AudioStreamPlayer
        {
            Stream = generator,
            Bus = "Music",
            Autoplay = false
        };

        AddChild(_player);
        _player.Play();
        _playback = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();
    }

    public override void _Process(double delta)
    {
        if (_playback == null || !_player.Playing)
            return;

        // Check how many audio frames the generator buffer needs
        int framesAvailable = _playback.GetFramesAvailable();
        if (framesAvailable <= 0)
            return;

        int toGenerate = Math.Min(framesAvailable, _bufferL.Length);

        // Generate procedural block with zero phase clicks
        _synthEngine.GenerateStreamBlock(_bufferL, _bufferR, toGenerate, Context);

        // Push stereo samples to Godot audio stream
        for (int i = 0; i < toGenerate; i++)
        {
            _playback.PushFrame(new Vector2(_bufferL[i], _bufferR[i]));
        }
    }

    /// <summary>
    /// Update live gameplay parameters to modulate tempo, filter cutoff, and intensity.
    /// </summary>
    /// <param name="intensityLevel">Progression stage or speed (1 to 15+).</param>
    /// <param name="tension">Danger or urgency ratio (0.0 to 1.0).</param>
    /// <param name="dissonance">Dissonance or hazard distortion (0.0 to 1.0).</param>
    /// <param name="activityRate">Player input velocity / actions per minute.</param>
    /// <param name="isDucking">Trigger sidechain volume dip (-8dB) on combo/SFX.</param>
    /// <param name="pitchScale">Pitch modifier (1.0 = normal, 0.0 = tape stop dive).</param>
    public void UpdateGameState(
        int intensityLevel = 1,
        float tension = 0.0f,
        float dissonance = 0.0f,
        double activityRate = 1.0,
        bool isDucking = false,
        double pitchScale = 1.0)
    {
        Context = new AdaptiveAudioContext
        {
            IntensityLevel = intensityLevel,
            Tension = tension,
            Dissonance = dissonance,
            ActivityRate = activityRate,
            IsDucking = isDucking,
            PitchScale = pitchScale
        };
    }
}
