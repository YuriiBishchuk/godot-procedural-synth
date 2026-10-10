using System;

namespace GodotProceduralSynth;

/// <summary>
/// Preset musical aesthetics for quick demonstration and theme selection.
/// </summary>
public enum SynthAesthetic
{
    CyberpunkDarksynth = 0,
    IndustrialMidTempo = 1,
    UkrainianCyberShchedryk = 2,
    RetroArcade = 3,
    AmbientDrone = 4
}

/// <summary>
/// Universal adaptive context passed to the synthesizer to modulate audio dynamically.
/// </summary>
public struct AdaptiveAudioContext
{
    /// <summary>Intensity / Speed stage (1 to 15+). Modulates tempo and drum arrangement.</summary>
    public int IntensityLevel;

    /// <summary>Tension / Danger ratio (0.0 to 1.0). Opens resonant filter cutoff and lifts octave register.</summary>
    public float Tension;

    /// <summary>Dissonance / Hazard ratio (0.0 to 1.0). Adds dark sub-harmonics and distortion.</summary>
    public float Dissonance;

    /// <summary>Player action rate / APM (Actions Per Minute). Drives arpeggio density.</summary>
    public double ActivityRate;

    /// <summary>Whether sidechain ducking (-8dB volume dip) is currently active.</summary>
    public bool IsDucking;

    /// <summary>Pitch scale factor (1.0 = normal, 0.0 = full tape-stop dive).</summary>
    public double PitchScale;

    public static AdaptiveAudioContext Default => new()
    {
        IntensityLevel = 1,
        Tension = 0.1f,
        Dissonance = 0.0f,
        ActivityRate = 1.0,
        IsDucking = false,
        PitchScale = 1.0
    };
}

/// <summary>
/// Legacy gameplay context mapping for game boards (e.g., arcade/puzzle games).
/// Maps directly to universal adaptive parameters.
/// </summary>
public struct GameplayAudioContext
{
    public int Level;
    public int TowerHeight;
    public int HoleCount;
    public double Apm;
    public bool TetrisDucking;
    public double GameOverPitch;

    public static GameplayAudioContext Default => new()
    {
        Level = 1,
        TowerHeight = 2,
        HoleCount = 0,
        Apm = 1.0,
        TetrisDucking = false,
        GameOverPitch = 1.0
    };

    public AdaptiveAudioContext ToAdaptiveContext() => new()
    {
        IntensityLevel = Level,
        Tension = Math.Clamp(TowerHeight / 15.0f, 0.0f, 1.0f),
        Dissonance = Math.Clamp(HoleCount / 10.0f, 0.0f, 1.0f),
        ActivityRate = Apm,
        IsDucking = TetrisDucking,
        PitchScale = GameOverPitch
    };
}
