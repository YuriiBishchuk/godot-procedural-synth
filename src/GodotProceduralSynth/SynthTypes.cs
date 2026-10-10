using System;

namespace GodotProceduralSynth;

/// <summary>
/// Aesthetic styles supported by the universal procedural music engine.
/// </summary>
public enum SynthAesthetic
{
    CyberpunkDarksynth = 0,
    IndustrialMidTempo = 1,
    UkrainianCyberShchedryk = 2
}

/// <summary>
/// Runtime gameplay context passed to the universal synthesizer.
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
}
