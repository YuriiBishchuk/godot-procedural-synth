using System;

namespace GodotProceduralSynth;

public enum BassPattern
{
    DoomArgentChug = 0,    // Mick Gordon rhythmic palm-mute chugs with accent leaps
    Rolling16th = 1,       // Cyberpunk 2077 driving 16th note synthwave bass
    HeavyHalfTimeGrowl = 2,// Mid-tempo industrial slow devastating stomp
    WalkingMoog = 3,       // Melodic retro-arcade walking synth bass
    PulsingEighths = 4     // Hypnotic pulsing 8th notes with filter sweeps
}

public enum DrumPattern
{
    FourOnTheFloor = 0,    // Punchy 4x4 electro kick + fat snare on 2 & 4
    HeavyHalfTime = 1,     // Stomp on 1, Huge industrial clap on 3
    DoubleKickPanic = 2,   // Fast double-kick Doom groove with syncopated snare
    ArcadeBreakbeat = 3,   // Syncopated classic arcade electro break
    MinimalistDrive = 4    // Driving kick + tight high-velocity closed hats
}

public enum LeadStyle
{
    None = 0,
    CyberArp = 1,          // 16th note ascending/descending cyberpunk arpeggios
    LaserStabs = 2,        // Dark resonant laser synth stabs on off-beats
    UkrainianMotif = 3,    // Hypnotic melodic bell/folk motifs in minor scales
    ArcadeChiptune = 4,    // Nostalgic retro square/pulse counter-melodies
    AtmosphericDrone = 5   // Detuned analog saw strings with stereo chorus
}

/// <summary>
/// Declarative recipe for instant track generation.
/// Any developer or procedural game system can create a track in 1 line.
/// </summary>
public record TrackRecipe
{
    public int Id { get; init; } = 1;
    public string Title { get; init; } = "Cyber Genesis";
    public string Category { get; init; } = "DoomCyberpunk"; // DoomCyberpunk, Darksynth2077, ArcadeRetro, IndustrialMidtempo, UkrainianCyber
    public string Description { get; init; } = "";
    public double Bpm { get; init; } = 126.0;
    public double Duration { get; init; } = 7.0; // 7 seconds showcase per sample
    public double RootFreq { get; init; } = 73.42; // D2 (Standard dark cyberpunk key)
    public double BassDrive { get; init; } = 1.6;
    public double FilterCutoff { get; init; } = 2200.0;
    public double FilterResonance { get; init; } = 1.6;
    public BassPattern BassPattern { get; init; } = BassPattern.Rolling16th;
    public DrumPattern DrumPattern { get; init; } = DrumPattern.FourOnTheFloor;
    public LeadStyle LeadStyle { get; init; } = LeadStyle.CyberArp;
    public double SidechainDepth { get; init; } = 0.85;
    public int ScaleMode { get; init; } = 0; // 0 = Natural Minor, 1 = Phrygian Dominant, 2 = Dorian
}
