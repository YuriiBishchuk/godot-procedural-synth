using Godot;
using System;
using GodotProceduralSynth;

namespace MyGodotGame;

/// <summary>
/// Example Godot 4.x C# Node integrating GodotProceduralSynth with AudioStreamGenerator.
/// Synthesizes dynamic music in 100% real-time on Godot's audio thread.
/// </summary>
public partial class AdaptiveMusicEngine : Node
{
    private const int SampleRate = UniversalSynthEngine.SampleRate; // 44100 Hz
    private const float GeneratorBufferSeconds = 0.15f; // 150ms buffer for zero glitching/underruns

    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback _playback = null!;
    private readonly UniversalSynthEngine _synthEngine = new();

    // Reusable transfer buffers
    private readonly float[] _bufferL = new float[2048];
    private readonly float[] _bufferR = new float[2048];

    // Current game state context
    public GameplayAudioContext Context { get; set; } = GameplayAudioContext.Default;

    public override void _Ready()
    {
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

        // Check how many frames the generator needs
        int framesAvailable = _playback.GetFramesAvailable();
        if (framesAvailable <= 0)
            return;

        int toGenerate = Math.Min(framesAvailable, _bufferL.Length);

        // Generate procedural block with zero phase reset
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
    public void UpdateGameState(int level, int towerHeight, int holes, double apm, bool isDucking, double gameOverPitch = 1.0)
    {
        Context = new GameplayAudioContext
        {
            Level = level,
            TowerHeight = towerHeight,
            HoleCount = holes,
            Apm = apm,
            TetrisDucking = isDucking,
            GameOverPitch = gameOverPitch
        };
    }
}
