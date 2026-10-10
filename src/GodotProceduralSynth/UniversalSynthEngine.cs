using System;

namespace GodotProceduralSynth;

/// <summary>
/// Universal Procedural Music & Synthesizer Engine.
/// Pure C# engine used identically by Godot in-game audio and standalone test benches.
/// Eliminates harsh clipping, speaker distortion, and buzzing through:
///   - Bandlimited anti-aliased oscillators (PolyBLEP)
///   - 4-pole resonant Moog Ladder filter (24 dB/oct)
///   - Analog soft-saturation (hyperbolic tangent)
///   - Dynamic gameplay adaptation (Level, Height, Holes, APM, Ducking)
/// </summary>
public class UniversalSynthEngine
{
    public const int SampleRate = 44100;

    // Filters for synth layers
    private readonly MoogLadderFilter _filterBassL = new();
    private readonly MoogLadderFilter _filterBassR = new();
    private readonly MoogLadderFilter _filterLeadL = new();
    private readonly MoogLadderFilter _filterLeadR = new();
    private readonly MoogLadderFilter _filterCounterL = new();
    private readonly MoogLadderFilter _filterCounterR = new();

    private readonly Random _rand = new(1337);

    // Oscillator phases
    private double _phaseBass = 0.0;
    private double _phaseSub = 0.0;
    private double _phaseLead = 0.0;
    private double _phaseLead2 = 0.0;
    private double _phaseCounter = 0.0;
    private double _phaseCounter2 = 0.0;

    // Stereo delay lines
    private readonly double[] _delayBufferL = new double[SampleRate];
    private readonly double[] _delayBufferR = new double[SampleRate];
    private int _delayIndex = 0;

    // Streaming state (for real-time AudioStreamGenerator)
    private double _streamBeatPhase = 0.0;
    private double _streamCurrentBpm = 120.0;
    private double _streamSmoothedTension = 0.15;
    private double _streamSmoothedCutoff = 1200.0;
    private double _streamCurrentDuckGain = 1.0;
    private int _streamCurrentStep = 0;
    private double _streamNoteTime = 0.0;

    public void Reset()
    {
        _filterBassL.Reset();
        _filterBassR.Reset();
        _filterLeadL.Reset();
        _filterLeadR.Reset();
        _filterCounterL.Reset();
        _filterCounterR.Reset();
        _phaseBass = _phaseSub = _phaseLead = _phaseLead2 = _phaseCounter = _phaseCounter2 = 0.0;
        Array.Clear(_delayBufferL, 0, _delayBufferL.Length);
        Array.Clear(_delayBufferR, 0, _delayBufferR.Length);
        _streamBeatPhase = 0.0;
        _streamCurrentBpm = 120.0;
        _streamSmoothedTension = 0.15;
        _streamSmoothedCutoff = 1200.0;
        _streamCurrentDuckGain = 1.0;
        _streamCurrentStep = 0;
        _streamNoteTime = 0.0;
    }

    /// <summary>
    /// Synthesize a complete block or track for a given aesthetic and gameplay state.
    /// </summary>
    public (float[] Left, float[] Right) RenderTrack(
        SynthAesthetic aesthetic,
        GameplayAudioContext ctx,
        double durationSeconds)
    {
        Reset();
        int totalSamples = (int)(durationSeconds * SampleRate);
        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        switch (aesthetic)
        {
            case SynthAesthetic.CyberpunkDarksynth:
                RenderCyberpunkDarksynth(left, right, ctx, totalSamples);
                break;
            case SynthAesthetic.IndustrialMidTempo:
                RenderIndustrialMidTempo(left, right, ctx, totalSamples);
                break;
            case SynthAesthetic.UkrainianCyberShchedryk:
                RenderUkrainianCyberShchedryk(left, right, ctx, totalSamples);
                break;
        }

        return (left, right);
    }

    /// <summary>
    /// Render a track directly from a declarative recipe.
    /// Used to generate tracks in 1 line of code.
    /// </summary>
    public (float[] Left, float[] Right) RenderFromRecipe(TrackRecipe recipe)
    {
        Reset();
        int totalSamples = (int)(recipe.Duration * SampleRate);
        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        double bpm = recipe.Bpm;
        double beatSec = 60.0 / bpm;
        double sixteenthSec = beatSec / 4.0;
        int sixteenthSamples = (int)(SampleRate * sixteenthSec);
        if (sixteenthSamples < 50) sixteenthSamples = 50;

        double root = recipe.RootFreq;
        double fifth = root * 1.4983;
        double minThird = root * 1.1892;
        double octave = root * 2.0;
        double flatSeven = root * 1.7818;
        double flatFive = root * 1.4142;

        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)SampleRate;
            int step16 = (int)(i / sixteenthSamples);
            double noteTime16 = (i % sixteenthSamples) / (double)SampleRate;
            double beatTime = (i % (int)(SampleRate * beatSec)) / (double)SampleRate;
            int stepBeat = (int)(i / (SampleRate * beatSec)) % 4;

            // --- 1. BASS SYNTHESIS ---
            double noteFreq = root;
            double bassEnv = 1.0;
            switch (recipe.BassPattern)
            {
                case BassPattern.DoomArgentChug:
                {
                    double[] doomScale = { root, root, fifth, root, octave, 0.0, root, flatFive };
                    noteFreq = doomScale[step16 % doomScale.Length];
                    bassEnv = Math.Exp(-noteTime16 * 26.0); // Tight palm-mute
                    break;
                }
                case BassPattern.Rolling16th:
                {
                    double[] rollScale = { root, root, minThird, root, fifth, root, flatSeven, root };
                    noteFreq = rollScale[step16 % rollScale.Length];
                    bassEnv = Math.Exp(-noteTime16 * 14.0);
                    break;
                }
                case BassPattern.HeavyHalfTimeGrowl:
                {
                    double[] growlScale = { root, 0.0, minThird, 0.0, flatFive, 0.0, root, 0.0 };
                    noteFreq = growlScale[(step16 / 2) % growlScale.Length];
                    bassEnv = Math.Exp(-noteTime16 * 6.0);
                    break;
                }
                case BassPattern.WalkingMoog:
                {
                    double[] walkScale = { root, root, minThird, fifth, octave, flatSeven, fifth, minThird };
                    noteFreq = walkScale[(step16 / 2) % walkScale.Length];
                    bassEnv = Math.Exp(-noteTime16 * 8.0);
                    break;
                }
                case BassPattern.PulsingEighths:
                {
                    noteFreq = (step16 % 8 < 4) ? root : flatSeven;
                    bassEnv = Math.Exp(-noteTime16 * 10.0);
                    break;
                }
            }

            double bassRaw = 0.0;
            if (noteFreq > 0.0)
            {
                double saw = DspUtils.BandlimitedSaw(ref _phaseBass, noteFreq, SampleRate) * 0.70;
                double sub = DspUtils.Sine(ref _phaseSub, noteFreq * 0.5, SampleRate) * 0.55;
                bassRaw = (saw + sub) * bassEnv;
            }

            // Moog filter on bass
            double cutoff = recipe.FilterCutoff * (0.35 + 0.65 * bassEnv);
            double filtBassL = _filterBassL.Process(bassRaw, cutoff, recipe.FilterResonance, SampleRate);
            double filtBassR = _filterBassR.Process(bassRaw, cutoff * 1.03, recipe.FilterResonance, SampleRate);

            filtBassL = DspUtils.SoftSat(filtBassL, recipe.BassDrive);
            filtBassR = DspUtils.SoftSat(filtBassR, recipe.BassDrive);

            // --- 2. DRUMS SYNTHESIS ---
            double kick = 0.0, snare = 0.0, hat = 0.0;
            switch (recipe.DrumPattern)
            {
                case DrumPattern.FourOnTheFloor:
                    kick = SynthKick(beatTime);
                    if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                    hat = SynthHiHat(noteTime16, isAccented: (step16 % 2 == 1));
                    break;
                case DrumPattern.HeavyHalfTime:
                    if (stepBeat == 0 || stepBeat == 2) kick = SynthHeavyIndustrialKick(beatTime);
                    if (stepBeat == 1 || stepBeat == 3) snare = SynthIndustrialClapSnare(beatTime);
                    hat = SynthMetallicClick(beatTime);
                    break;
                case DrumPattern.DoubleKickPanic:
                    kick = SynthKick(beatTime);
                    if (noteTime16 < 0.05 && (step16 % 4 == 2)) kick += SynthKick(noteTime16) * 0.7;
                    if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                    hat = SynthHiHat(noteTime16, isAccented: true);
                    break;
                case DrumPattern.ArcadeBreakbeat:
                    if (stepBeat == 0 || (stepBeat == 2 && noteTime16 < 0.05)) kick = SynthKick(beatTime);
                    if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                    hat = SynthHiHat(noteTime16, isAccented: (step16 % 4 == 2));
                    break;
                case DrumPattern.MinimalistDrive:
                    kick = SynthKick(beatTime);
                    hat = SynthHiHat(noteTime16, isAccented: true);
                    break;
            }

            // Sidechain Ducking
            double sidechain = Math.Min(1.0, (beatTime / 0.08) * recipe.SidechainDepth + (1.0 - recipe.SidechainDepth));
            filtBassL *= sidechain;
            filtBassR *= sidechain;

            // --- 3. LEAD / MELODY SYNTHESIS ---
            double leadL = 0.0, leadR = 0.0;
            switch (recipe.LeadStyle)
            {
                case LeadStyle.CyberArp:
                {
                    double[] arp = { root * 4, minThird * 4, fifth * 4, octave * 4, flatSeven * 4, fifth * 4 };
                    double lFreq = arp[step16 % arp.Length];
                    double p1 = DspUtils.BandlimitedPulse(ref _phaseLead, lFreq, 0.40, SampleRate);
                    double p2 = DspUtils.BandlimitedSaw(ref _phaseLead2, lFreq * 1.004, SampleRate);
                    double lRaw = (p1 + p2) * 0.35 * Math.Exp(-noteTime16 * 12.0);
                    leadL = _filterLeadL.Process(lRaw, 2400.0, 1.2, SampleRate);
                    leadR = _filterLeadR.Process(lRaw, 2350.0, 1.2, SampleRate);
                    break;
                }
                case LeadStyle.LaserStabs:
                {
                    if (stepBeat == 2 && beatTime < 0.22)
                    {
                        double sFreq = (fifth * 4.0) * Math.Exp(-beatTime * 14.0);
                        double stab = DspUtils.BandlimitedSaw(ref _phaseLead, sFreq, SampleRate) * Math.Exp(-beatTime * 8.0) * 0.50;
                        leadL = stab;
                        leadR = stab * 0.7;
                    }
                    break;
                }
                case LeadStyle.UkrainianMotif:
                {
                    double[] shchedryk = { root * 4, (root * 4) * 0.89, root * 4, (root * 4) * 0.79 };
                    double sFreq = shchedryk[step16 % shchedryk.Length];
                    double p1 = DspUtils.BandlimitedPulse(ref _phaseLead, sFreq, 0.50, SampleRate);
                    double p2 = DspUtils.BandlimitedSaw(ref _phaseLead2, sFreq * 1.004, SampleRate);
                    double lRaw = (p1 + p2) * 0.38 * Math.Exp(-noteTime16 * 5.0);
                    leadL = _filterLeadL.Process(lRaw, 2200.0, 1.3, SampleRate);
                    leadR = _filterLeadR.Process(lRaw, 2200.0, 1.3, SampleRate);
                    break;
                }
                case LeadStyle.ArcadeChiptune:
                {
                    double[] chipMelody = { root * 4, fifth * 4, minThird * 4, octave * 4, fifth * 4, root * 4 };
                    double cFreq = chipMelody[step16 % chipMelody.Length];
                    double chip = DspUtils.BandlimitedPulse(ref _phaseLead, cFreq, 0.25, SampleRate) * 0.30 * Math.Exp(-noteTime16 * 10.0);
                    leadL = chip;
                    leadR = chip;
                    break;
                }
                case LeadStyle.AtmosphericDrone:
                {
                    double d1 = DspUtils.BandlimitedSaw(ref _phaseLead, fifth * 2.0, SampleRate) * 0.20;
                    double d2 = DspUtils.BandlimitedSaw(ref _phaseLead2, fifth * 2.008, SampleRate) * 0.20;
                    leadL = d1;
                    leadR = d2;
                    break;
                }
            }

            // Stereo Delay Line
            int delaySamples = (int)(sixteenthSamples * 3);
            int readIdx = (_delayIndex - delaySamples + SampleRate) % SampleRate;
            double delayWetL = _delayBufferL[readIdx] * 0.30;
            double delayWetR = _delayBufferR[readIdx] * 0.30;
            _delayBufferL[_delayIndex] = leadL + filtBassL * 0.10;
            _delayBufferR[_delayIndex] = leadR + filtBassR * 0.10;
            _delayIndex = (_delayIndex + 1) % SampleRate;

            // Final Sum & Soft Saturation
            double mixL = (filtBassL * 0.65) + (kick * 0.85) + (snare * 0.70) + (hat * 0.35) + leadL + delayWetL;
            double mixR = (filtBassR * 0.65) + (kick * 0.85) + (snare * 0.70) + (hat * 0.35) + leadR + delayWetR;

            left[i] = (float)DspUtils.SoftSat(mixL * 0.72, 1.0);
            right[i] = (float)DspUtils.SoftSat(mixR * 0.72, 1.0);
        }

        return (left, right);
    }

    /// <summary>
    /// Generates 50 distinct, curated track recipes across the 5 requested aesthetics.
    /// </summary>
    public static TrackRecipe[] GetDefault50Catalog()
    {
        var list = new System.Collections.Generic.List<TrackRecipe>();
        int id = 1;

        // Group 1: DOOM 2016 x Cyberpunk 2077 (Argent Metal meets Cyber Darksynth) - 10 tracks
        string[] doomTitles = {
            "Argent Chainsaw Overdrive", "BFG Cyber-Pulse", "Night City Hellgate",
            "Slayer's Cyberdeck", "Argent Reactor Meltdown", "Cyber-Demon Arcade",
            "Praetor Neon Armor", "Chainsaw Neon Stomp", "Glory Kill Matrix", "Mick Gordon Neon Synth"
        };
        for (int i = 0; i < doomTitles.Length; i++)
        {
            list.Add(new TrackRecipe
            {
                Id = id++,
                Title = doomTitles[i],
                Category = "DoomCyberpunk",
                Description = "Argent metal palm-mute chug + neon darksynth pulse. Важкий ударний панч без кліпінгу.",
                Bpm = 124.0 + (i % 5) * 3.0,
                RootFreq = 73.42, // D2
                BassDrive = 1.8 + (i % 3) * 0.3,
                FilterCutoff = 2200.0 + (i % 4) * 300.0,
                BassPattern = BassPattern.DoomArgentChug,
                DrumPattern = (i % 2 == 0) ? DrumPattern.DoubleKickPanic : DrumPattern.FourOnTheFloor,
                LeadStyle = (i % 2 == 0) ? LeadStyle.LaserStabs : LeadStyle.CyberArp
            });
        }

        // Group 2: Cyberpunk 2077 Night City / Darksynth - 10 tracks
        string[] cyberpunkTitles = {
            "Night City Underpass", "Cyberpsycho Pursuit", "Afterlife Neon Pulse",
            "Kiroshi Optics Overclock", "Sandevistan Warp", "Militech Drone Patrol",
            "Arasaka Tower Infiltration", "Johnny's Silver Drive", "Chrome & Wire Rebellion", "Badlands Neon Highway"
        };
        for (int i = 0; i < cyberpunkTitles.Length; i++)
        {
            list.Add(new TrackRecipe
            {
                Id = id++,
                Title = cyberpunkTitles[i],
                Category = "Darksynth2077",
                Description = "Клубний darksynth Night City: швидкий 16-нотний аналоговий бас, неонові арпеджіо, сайдчейн.",
                Bpm = 120.0 + (i % 5) * 2.5,
                RootFreq = (i % 2 == 0) ? 73.42 : 87.31, // D2 or F2
                BassDrive = 1.4 + (i % 3) * 0.2,
                FilterCutoff = 2000.0 + (i % 3) * 400.0,
                BassPattern = BassPattern.Rolling16th,
                DrumPattern = DrumPattern.FourOnTheFloor,
                LeadStyle = LeadStyle.CyberArp
            });
        }

        // Group 3: Arcade Retro / Cyber-Tetris - 10 tracks
        string[] arcadeTitles = {
            "Neon Matrix 1989", "Tetromino Overdrive", "Insert Coin to Survive",
            "Cyber-Arcade Highscore", "Pixelated Adrenaline", "Cathode Ray Neon",
            "Turbo Line Clear", "Chiptune Cyber-Stomp", "Ghost Block Glitch", "Arcade Champion Rave"
        };
        for (int i = 0; i < arcadeTitles.Length; i++)
        {
            list.Add(new TrackRecipe
            {
                Id = id++,
                Title = arcadeTitles[i],
                Category = "ArcadeRetro",
                Description = "Енергійний аркадний синтвейв: чиптюн-хвилі, крокуючий Moog бас, ретро-брейкбіт.",
                Bpm = 122.0 + (i % 5) * 3.0,
                RootFreq = (i % 2 == 0) ? 110.0 : 73.42, // A2 or D2
                BassDrive = 1.3,
                FilterCutoff = 2400.0,
                BassPattern = (i % 2 == 0) ? BassPattern.WalkingMoog : BassPattern.Rolling16th,
                DrumPattern = (i % 2 == 0) ? DrumPattern.ArcadeBreakbeat : DrumPattern.FourOnTheFloor,
                LeadStyle = LeadStyle.ArcadeChiptune
            });
        }

        // Group 4: Industrial Mid-Tempo / Dark Electro - 10 tracks
        string[] industrialTitles = {
            "Gesaffelstein Obsidian", "Tron Cyber-Grid Stomp", "Rezz Dark Pulse",
            "Hydraulic Cyber-Forge", "Neon Monolith", "Sub-Atomic Devastation",
            "Industrial Meat-Grinder", "Cyber-Stalker in Shadows", "Piston & Iron Heartbeat", "Reactor Core Silence"
        };
        for (int i = 0; i < industrialTitles.Length; i++)
        {
            list.Add(new TrackRecipe
            {
                Id = id++,
                Title = industrialTitles[i],
                Category = "IndustrialMidtempo",
                Description = "100-106 BPM важкий халф-тайм стомп, глибокий саб-гроул з ламповим насиченням, лазерні стеби.",
                Bpm = 98.0 + (i % 5) * 2.0,
                RootFreq = (i % 2 == 0) ? 82.41 : 73.42, // E2 or D2
                BassDrive = 2.0,
                FilterCutoff = 1600.0 + (i % 3) * 300.0,
                BassPattern = BassPattern.HeavyHalfTimeGrowl,
                DrumPattern = DrumPattern.HeavyHalfTime,
                LeadStyle = LeadStyle.LaserStabs
            });
        }

        // Group 5: Ukrainian Cyber-Heritage - 10 tracks
        string[] ukrainianTitles = {
            "Cyber-Shchedryk Bell Strike", "Kozak Neon Steppe", "Kyiv Night-Sky Defense",
            "Dnipro Cyber-Current", "Chornobyl Exclusion Core", "Carpathian Shadow Arp",
            "Brave Iron Cossack", "Volya Cyber-Anthem", "Khortytsia Neon Fortress", "Golden Gate Cyber-Dawn"
        };
        for (int i = 0; i < ukrainianTitles.Length; i++)
        {
            list.Add(new TrackRecipe
            {
                Id = id++,
                Title = ukrainianTitles[i],
                Category = "UkrainianCyber",
                Description = "Українська мелодика («Щедрик», козацькі мотиви) у неоновому кіберпанк-саунді Juno-106.",
                Bpm = 124.0 + (i % 5) * 2.0,
                RootFreq = 73.42, // D2
                BassDrive = 1.4,
                FilterCutoff = 2100.0,
                BassPattern = (i % 2 == 0) ? BassPattern.Rolling16th : BassPattern.WalkingMoog,
                DrumPattern = DrumPattern.FourOnTheFloor,
                LeadStyle = LeadStyle.UkrainianMotif
            });
        }

        return list.ToArray();
    }

    #region Aesthetic 1: Cyberpunk / Darksynth (Hotline Miami, Carpenter Brut style)
    private void RenderCyberpunkDarksynth(float[] left, float[] right, GameplayAudioContext ctx, int totalSamples)
    {
        double baseBpm = 124.0 + (ctx.Level - 1) * 2.5 + Math.Clamp(ctx.Apm * 4.0, 0.0, 15.0);
        double beatSec = 60.0 / baseBpm;
        double sixteenthSec = beatSec / 4.0;
        int sixteenthSamples = (int)(SampleRate * sixteenthSec);

        // Rolling 16th darksynth bass riff in D-minor
        // D2, D2, F2, G2, D2, D2, C3, A#2
        double[] bassRiff = { 73.42, 73.42, 87.31, 98.00, 73.42, 73.42, 130.81, 116.54 };

        float towerNorm = Math.Clamp(ctx.TowerHeight / 15.0f, 0.0f, 1.0f);
        float apmNorm = Math.Clamp((float)ctx.Apm / 3.5f, 0.0f, 1.0f);
        float tension = Math.Clamp(towerNorm * 0.6f + apmNorm * 0.4f, 0.0f, 1.0f);

        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)SampleRate;
            int step16 = (int)(i / sixteenthSamples);
            int stepPattern = step16 % bassRiff.Length;
            double noteTime = (i % sixteenthSamples) / (double)SampleRate;

            // 1. Bassline (Analog Saw + Sub Octave)
            double noteFreq = bassRiff[stepPattern];
            double bassOsc = DspUtils.BandlimitedSaw(ref _phaseBass, noteFreq, SampleRate) * 0.70;
            double subOsc = DspUtils.Sine(ref _phaseSub, noteFreq * 0.5, SampleRate) * 0.50;
            double bassRaw = bassOsc + subOsc;

            // Filter envelope: snappy punch on each 16th note
            double envBass = Math.Exp(-noteTime * (14.0 - tension * 4.0));
            double cutoffBass = 350.0 + (1200.0 * envBass) + (tension * 1600.0);
            double filteredBassL = _filterBassL.Process(bassRaw, cutoffBass, 1.8, SampleRate);
            double filteredBassR = _filterBassR.Process(bassRaw, cutoffBass * 1.02, 1.8, SampleRate);

            // Warm analog saturation (NO hard clipping)
            filteredBassL = DspUtils.SoftSat(filteredBassL, 1.4);
            filteredBassR = DspUtils.SoftSat(filteredBassR, 1.4);

            // 2. Drums
            int stepBeat = (int)(i / (SampleRate * beatSec)) % 4;
            double beatTime = (i % (int)(SampleRate * beatSec)) / (double)SampleRate;

            // Kick (Four on the floor)
            double kick = SynthKick(beatTime);

            // Snare (on beats 2 and 4)
            double snare = 0.0;
            if (stepBeat == 1 || stepBeat == 3)
            {
                snare = SynthSnare(beatTime);
            }

            // Hi-Hat (Every 16th note)
            double hat = SynthHiHat(noteTime, isAccented: (step16 % 2 == 1));

            // Sidechain Ducking from Kick
            double sidechain = Math.Min(1.0, beatTime / 0.07);
            filteredBassL *= sidechain;
            filteredBassR *= sidechain;

            // 3. Arpeggiator Lead (Enters as level / tension rises)
            double leadL = 0.0, leadR = 0.0;
            if (ctx.Level >= 2 || tension > 0.3f)
            {
                double[] arpNotes = { 293.66, 349.23, 440.00, 523.25, 440.00, 349.23 }; // D4, F4, A4, C5
                int arpIdx = (step16 * 2) % arpNotes.Length;
                double leadFreq = arpNotes[arpIdx];
                double leadRaw = DspUtils.BandlimitedPulse(ref _phaseLead, leadFreq, 0.40, SampleRate);
                double leadRaw2 = DspUtils.BandlimitedSaw(ref _phaseLead2, leadFreq * 1.003, SampleRate);
                double leadMix = (leadRaw + leadRaw2) * 0.35 * Math.Exp(-noteTime * 10.0);

                double cutoffLead = 1200.0 + tension * 2200.0;
                leadL = _filterLeadL.Process(leadMix, cutoffLead, 1.2, SampleRate);
                leadR = _filterLeadR.Process(leadMix, cutoffLead * 0.98, 1.2, SampleRate);
            }

            // Delay effect for depth
            int delaySamples = (int)(sixteenthSamples * 3);
            int readIdx = (_delayIndex - delaySamples + SampleRate) % SampleRate;
            double delayWetL = _delayBufferL[readIdx] * 0.35;
            double delayWetR = _delayBufferR[readIdx] * 0.35;

            _delayBufferL[_delayIndex] = leadL + filteredBassL * 0.15;
            _delayBufferR[_delayIndex] = leadR + filteredBassR * 0.15;
            _delayIndex = (_delayIndex + 1) % SampleRate;

            // Mix
            double mixL = (filteredBassL * 0.65) + (kick * 0.85) + (snare * 0.70) + (hat * 0.40) + leadL + delayWetL;
            double mixR = (filteredBassR * 0.65) + (kick * 0.85) + (snare * 0.70) + (hat * 0.40) + leadR + delayWetR;

            // Tetris line clear ducking
            if (ctx.TetrisDucking && t >= 3.5 && t <= 3.9)
            {
                mixL *= 0.30;
                mixR *= 0.30;
            }

            // Master soft limit (guarantees safe headroom and zero clipping)
            left[i] = (float)DspUtils.SoftSat(mixL * 0.75, 1.0);
            right[i] = (float)DspUtils.SoftSat(mixR * 0.75, 1.0);
        }
    }
    #endregion

    #region Aesthetic 2: Industrial Mid-Tempo / Dark Electro (Gesaffelstein, Tron style)
    private void RenderIndustrialMidTempo(float[] left, float[] right, GameplayAudioContext ctx, int totalSamples)
    {
        double baseBpm = 104.0 + (ctx.Level - 1) * 1.5;
        double beatSec = 60.0 / baseBpm;
        int beatSamples = (int)(SampleRate * beatSec);

        // Heavy, minimalist industrial notes (E1, G1, Bb1)
        double[] bassNotes = { 41.20, 41.20, 0.0, 49.00, 41.20, 0.0, 58.27, 41.20 };
        int barSamples = beatSamples * 4;
        int stepSamples = barSamples / bassNotes.Length;

        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)SampleRate;
            int stepIdx = (int)((i % barSamples) / (double)stepSamples);
            double stepTime = (i % stepSamples) / (double)SampleRate;
            double beatTime = (i % beatSamples) / (double)SampleRate;
            int beatNumber = (int)(i / beatSamples) % 4;

            // 1. Deep Sub-Growl Bass (Half-time industrial stomp)
            double noteFreq = bassNotes[stepIdx];
            double bassSample = 0.0;
            if (noteFreq > 0.0)
            {
                double saw = DspUtils.BandlimitedSaw(ref _phaseBass, noteFreq, SampleRate);
                double pulse = DspUtils.BandlimitedPulse(ref _phaseSub, noteFreq * 0.5, 0.5, SampleRate);
                double raw = saw * 0.6 + pulse * 0.7;

                double env = Math.Exp(-stepTime * 5.0);
                double cutoff = 180.0 + 900.0 * env;
                bassSample = _filterBassL.Process(raw, cutoff, 2.2, SampleRate);
                bassSample = DspUtils.SoftSat(bassSample, 2.0); // Warm tube drive
            }

            // 2. Heavy Half-Time Drums (Kick on 1 & 3, Huge Snare on 2 & 4)
            double kick = (beatNumber == 0 || beatNumber == 2) ? SynthHeavyIndustrialKick(beatTime) : 0.0;
            double snare = (beatNumber == 1 || beatNumber == 3) ? SynthIndustrialClapSnare(beatTime) : 0.0;
            double clickPerc = SynthMetallicClick(beatTime);

            // Sidechain ducking on kick
            double sidechain = (kick > 0.05) ? Math.Min(1.0, beatTime / 0.10) : 1.0;
            bassSample *= sidechain;

            // 3. Dark Synth Laser Stabs
            double stab = 0.0;
            if (beatNumber == 2 && beatTime < 0.25)
            {
                double stabFreq = 330.0 * Math.Exp(-beatTime * 12.0);
                stab = DspUtils.BandlimitedSaw(ref _phaseLead, stabFreq, SampleRate) * Math.Exp(-beatTime * 8.0) * 0.45;
            }

            // Stereo spreading
            double outL = bassSample * 0.70 + kick * 0.90 + snare * 0.75 + clickPerc * 0.25 + stab * 0.8;
            double outR = bassSample * 0.70 + kick * 0.90 + snare * 0.75 + clickPerc * 0.35 + stab * 0.5;

            left[i] = (float)DspUtils.SoftSat(outL * 0.72, 1.0);
            right[i] = (float)DspUtils.SoftSat(outR * 0.72, 1.0);
        }
    }
    #endregion

    #region Aesthetic 3: Ukrainian Cyber-Shchedryk (Щедрик / Leontovych Cyber-Synthwave)
    public void RenderUkrainianCyberShchedryk(float[] left, float[] right, GameplayAudioContext ctx, int totalSamples)
    {
        float heightNorm = Math.Clamp(ctx.TowerHeight / 15.0f, 0.0f, 1.0f);
        float holesNorm = Math.Clamp(ctx.HoleCount / 6.0f, 0.0f, 1.0f);
        float apmNorm = Math.Clamp((float)ctx.Apm / 3.5f, 0.0f, 1.0f);
        float tension = Math.Clamp(heightNorm * 0.55f + holesNorm * 0.25f + apmNorm * 0.20f, 0.0f, 1.0f);

        // Dynamic BPM: 120 (Calm) -> 136 (Danger)
        double baseBpm = 120.0 + (ctx.Level - 1) * 2.0 + apmNorm * 8.0 + tension * 6.0;
        double beatSec = 60.0 / baseBpm;
        int stepSamples = (int)(SampleRate * (beatSec / 3.0)); // 3/4 or 6/8 rhythmic feel of Shchedryk
        if (stepSamples < 50) stepSamples = 50;

        double pitchMult = Math.Clamp(ctx.GameOverPitch, 0.001, 1.5);

        // Iconic Ukrainian Masterpiece: "Щедрик" (Mykola Leontovych) in D-minor:
        double[] shchedrykMelody =
        {
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,

            698.46, 659.25, 698.46, 587.33,
            698.46, 659.25, 698.46, 587.33,
            783.99, 698.46, 783.99, 659.25,
            880.00, 783.99, 880.00, 698.46,

            1174.66, 1046.50, 932.33, 880.00,
            783.99, 698.46, 659.25, 587.33,
            587.33, 523.25, 587.33, 466.16,
            587.33, 0.0,    587.33, 0.0
        };

        double[] bassProgression =
        {
            73.42, 73.42, 73.42, 73.42,
            58.27, 58.27, 48.99, 55.00,
            73.42, 58.27, 55.00, 73.42
        };

        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)SampleRate;
            int stepIdx = (int)(i / stepSamples);
            int melodyStep = stepIdx % shchedrykMelody.Length;
            int bassStep = (stepIdx / 4) % bassProgression.Length;

            double noteTime = (i % stepSamples) / (double)SampleRate;
            double beatTime = (i % (int)(SampleRate * beatSec)) / (double)SampleRate;
            int stepBeat = (int)(i / (SampleRate * beatSec)) % 4;

            // 1. Melodic Lead: Warm Juno-106 style poly-synth
            double leadFreq = shchedrykMelody[melodyStep] * pitchMult;
            if (tension > 0.65f && melodyStep < 8) leadFreq *= 2.0; // Octave lift in danger zone!

            double leadSignal = 0.0;
            if (leadFreq > 0.0)
            {
                double osc1 = DspUtils.BandlimitedPulse(ref _phaseLead, leadFreq, 0.50, SampleRate);
                double osc2 = DspUtils.BandlimitedSaw(ref _phaseLead2, leadFreq * 1.004, SampleRate);
                double raw = (osc1 * 0.5 + osc2 * 0.5) * Math.Exp(-noteTime * 4.5);

                // Filter opens with tension: 1200 Hz (Calm) -> 2900 Hz (Danger)
                double cutoff = (1200.0 + tension * 1700.0) + 900.0 * Math.Exp(-noteTime * 8.0);
                leadSignal = _filterLeadL.Process(raw, cutoff, 1.4, SampleRate) * (0.60 + tension * 0.15);
            }

            // 2. Warm Walking Synthwave Bassline
            double bassFreq = bassProgression[bassStep] * pitchMult;
            double sawBass = DspUtils.BandlimitedSaw(ref _phaseBass, bassFreq, SampleRate) * 0.65;
            double subBass = DspUtils.Sine(ref _phaseSub, bassFreq * 0.5, SampleRate) * 0.55;
            double bassRaw = sawBass + subBass;

            double bassCutoff = 550.0 + tension * 600.0;
            double bassSignal = _filterBassL.Process(bassRaw, bassCutoff, 1.3, SampleRate);
            bassSignal = DspUtils.SoftSat(bassSignal, 1.2 + tension * 0.4);

            // 3. Dynamic Drum Kit based on tension
            double kick = 0.0, snare = 0.0, hat = 0.0;
            if (tension < 0.35f)
            {
                // Calm Start (Kick on 1 & 3, soft snare on 2 & 4)
                if (stepBeat == 0 || stepBeat == 2) kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime) * 0.7;
                hat = SynthHiHat(noteTime, isAccented: false) * 0.6;
            }
            else if (tension < 0.70f)
            {
                // Mid-game Speed (4-on-the-floor + driving hats)
                kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                hat = SynthHiHat(noteTime, isAccented: (stepIdx % 2 == 1));
            }
            else
            {
                // Critical Danger (Frantic Double-Kick panic!)
                kick = SynthKick(beatTime);
                if (noteTime < 0.05 && (stepIdx % 4 == 2)) kick += SynthKick(noteTime) * 0.8;
                if (stepBeat == 1 || stepBeat == 3) snare = SynthIndustrialClapSnare(beatTime) * 0.9;
                hat = SynthHiHat(noteTime, isAccented: true);
            }

            // Sidechain Ducking from Kick
            double sidechain = Math.Min(1.0, beatTime / 0.075);
            bassSignal *= sidechain;

            // Stereo chorus simulation
            double leadL = leadSignal * 0.9;
            double leadR = leadSignal * 1.1;

            double mixL = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadL;
            double mixR = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadR;

            // Tetris 4-Line Clear Ducking
            if (ctx.TetrisDucking && t >= 3.5 && t <= 3.9)
            {
                mixL *= 0.28;
                mixR *= 0.28;
            }

            left[i] = (float)DspUtils.SoftSat(mixL * 0.70, 1.0);
            right[i] = (float)DspUtils.SoftSat(mixR * 0.70, 1.0);
        }
    }

    /// <summary>
    /// Generates audio frames in real time without phase resets.
    /// Used by Godot AudioStreamGenerator for continuous procedural audio.
    /// </summary>
    public void GenerateStreamBlock(float[] left, float[] right, int count, GameplayAudioContext ctx)
        => GenerateStreamBlock(left, right, count, ctx.ToAdaptiveContext());

    /// <summary>
    /// Generates audio frames in real time without phase resets using universal adaptive context.
    /// </summary>
    public void GenerateStreamBlock(float[] left, float[] right, int count, AdaptiveAudioContext ctx)
    {
        double dt = 1.0 / SampleRate;
        double rawTension = Math.Clamp(ctx.Tension * 0.6f + ctx.Dissonance * 0.2f + (float)(ctx.ActivityRate / 3.5) * 0.2f, 0.0f, 1.0f);
        double targetDuckGain = ctx.IsDucking ? 0.35 : 1.0;
        double pitchMult = Math.Clamp(ctx.PitchScale, 0.001, 1.5);

        double[] shchedrykMelody =
        {
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,

            698.46, 659.25, 698.46, 587.33,
            698.46, 659.25, 698.46, 587.33,
            783.99, 698.46, 783.99, 659.25,
            880.00, 783.99, 880.00, 698.46,

            1174.66, 1046.50, 932.33, 880.00,
            783.99, 698.46, 659.25, 587.33,
            587.33, 523.25, 587.33, 466.16,
            587.33, 0.0,    587.33, 0.0
        };

        double[] bassProgression =
        {
            73.42, 73.42, 73.42, 73.42,
            58.27, 58.27, 48.99, 55.00,
            73.42, 58.27, 55.00, 73.42
        };

        for (int i = 0; i < count; i++)
        {
            _streamSmoothedTension += (rawTension - _streamSmoothedTension) * (1.0 - Math.Exp(-dt / 0.50));
            double targetBpm = 120.0 + (ctx.IntensityLevel - 1) * 1.5 + (ctx.ActivityRate / 3.5) * 6.0 + _streamSmoothedTension * 6.0;
            _streamCurrentBpm += (targetBpm - _streamCurrentBpm) * (1.0 - Math.Exp(-dt / 0.80));
            _streamCurrentDuckGain += (targetDuckGain - _streamCurrentDuckGain) * (1.0 - Math.Exp(-dt / 0.04));

            double beatsPerSec = (_streamCurrentBpm / 60.0) * pitchMult;
            _streamBeatPhase += beatsPerSec * dt;

            double stepPhase = _streamBeatPhase * 3.0;
            int stepIdx = (int)Math.Floor(stepPhase);
            if (stepIdx != _streamCurrentStep)
            {
                _streamCurrentStep = stepIdx;
                _streamNoteTime = 0.0;
            }
            else
            {
                _streamNoteTime += dt;
            }

            int melodyStep = Math.Abs(_streamCurrentStep) % shchedrykMelody.Length;
            int bassStep = Math.Abs(_streamCurrentStep / 4) % bassProgression.Length;

            double beatFraction = _streamBeatPhase - Math.Floor(_streamBeatPhase);
            double beatTime = beatFraction * (60.0 / _streamCurrentBpm);
            int stepBeat = Math.Abs((int)Math.Floor(_streamBeatPhase)) % 4;

            // Lead Melody (Juno Bell)
            double leadFreq = shchedrykMelody[melodyStep] * pitchMult;
            if (_streamSmoothedTension > 0.72 && melodyStep < 8) leadFreq *= 2.0;

            double leadSignal = 0.0;
            if (leadFreq > 0.0)
            {
                double osc1 = DspUtils.BandlimitedPulse(ref _phaseLead, leadFreq, 0.50, SampleRate);
                double osc2 = DspUtils.BandlimitedSaw(ref _phaseLead2, leadFreq * 1.004, SampleRate);
                double raw = (osc1 * 0.5 + osc2 * 0.5) * Math.Exp(-_streamNoteTime * 4.2);

                double targetCutoff = (1200.0 + _streamSmoothedTension * 1700.0) + 900.0 * Math.Exp(-_streamNoteTime * 8.0);
                _streamSmoothedCutoff += (targetCutoff - _streamSmoothedCutoff) * (1.0 - Math.Exp(-dt / 0.01));

                leadSignal = _filterLeadL.Process(raw, _streamSmoothedCutoff, 1.4, SampleRate) * (0.60 + _streamSmoothedTension * 0.15);
            }

            // Bass (Moog walking bass)
            double bassFreq = bassProgression[bassStep] * pitchMult;
            double sawBass = DspUtils.BandlimitedSaw(ref _phaseBass, bassFreq, SampleRate) * 0.65;
            double subBass = DspUtils.Sine(ref _phaseSub, bassFreq * 0.5, SampleRate) * 0.55;
            double bassRaw = sawBass + subBass;
            double bassCutoff = 550.0 + _streamSmoothedTension * 600.0;
            double bassSignal = _filterBassL.Process(bassRaw, bassCutoff, 1.3, SampleRate);
            bassSignal = DspUtils.SoftSat(bassSignal, 1.2 + _streamSmoothedTension * 0.4);

            // Dynamic Drum Kit
            double kick = 0.0, snare = 0.0, hat = 0.0;
            if (_streamSmoothedTension < 0.35)
            {
                if (stepBeat == 0 || stepBeat == 2) kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime) * 0.7;
                hat = SynthHiHat(_streamNoteTime, isAccented: false) * 0.6;
            }
            else if (_streamSmoothedTension < 0.70)
            {
                kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                hat = SynthHiHat(_streamNoteTime, isAccented: (stepIdx % 2 == 1));
            }
            else
            {
                kick = SynthKick(beatTime);
                if (_streamNoteTime < 0.05 && (stepIdx % 4 == 2)) kick += SynthKick(_streamNoteTime) * 0.8;
                if (stepBeat == 1 || stepBeat == 3) snare = SynthIndustrialClapSnare(beatTime) * 0.9;
                hat = SynthHiHat(_streamNoteTime, isAccented: true);
            }

            double sidechain = Math.Min(1.0, beatTime / 0.075);
            bassSignal *= sidechain;

            double leadL = leadSignal * 0.9;
            double leadR = leadSignal * 1.1;

            double mixL = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadL;
            double mixR = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadR;

            mixL *= _streamCurrentDuckGain;
            mixR *= _streamCurrentDuckGain;

            left[i] = (float)DspUtils.SoftSat(mixL * 0.70, 1.0);
            right[i] = (float)DspUtils.SoftSat(mixR * 0.70, 1.0);
        }
    }

    /// <summary>
    /// Simulates a continuous 20-second live game evolution of Cyber-Shchedryk:
    ///   - 0-4.5s: Calm Start (Level 1, Height 2, APM 0.8)
    ///   - 4.5-9s: Speedup / Momentum (Level 5, Height 6, APM 2.6)
    ///   - 9-10s: Tetris 4-Line Clear Drop (deep ducking -> recovery surge)
    ///   - 10-16s: Red Alert Critical Danger (Height 14, APM 4.2, double kicks)
    ///   - 16-20s: Game Over Tape-Stop Pitch Dive
    /// </summary>
    public (float[] Left, float[] Right) RenderLiveGameplaySimulationShchedryk(double durationSec = 20.0)
    {
        Reset();
        int totalSamples = (int)(durationSec * SampleRate);
        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        double[] shchedrykMelody =
        {
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,
            587.33, 523.25, 587.33, 466.16,

            698.46, 659.25, 698.46, 587.33,
            698.46, 659.25, 698.46, 587.33,
            783.99, 698.46, 783.99, 659.25,
            880.00, 783.99, 880.00, 698.46,

            1174.66, 1046.50, 932.33, 880.00,
            783.99, 698.46, 659.25, 587.33,
            587.33, 523.25, 587.33, 466.16,
            587.33, 0.0,    587.33, 0.0
        };

        double[] bassProgression =
        {
            73.42, 73.42, 73.42, 73.42,
            58.27, 58.27, 48.99, 55.00,
            73.42, 58.27, 55.00, 73.42
        };

        double beatPhase = 0.0;
        double currentBpm = 120.0;
        double targetBpm = 120.0;
        double smoothedTension = 0.15;
        double smoothedCutoff = 1200.0;
        double currentDuckGain = 1.0;
        double targetDuckGain = 1.0;

        int currentStep = 0;
        double lastStepPhase = 0.0;
        double noteTime = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double t = i / (double)SampleRate;
            double dt = 1.0 / SampleRate;

            // Live gameplay timeline (simulating a match progression with smooth targets)
            float targetHeight = 0.15f;
            float targetApm = 0.20f;
            int level = 1;
            double pitchMult = 1.0;

            if (t < 4.5)
            {
                // Phase 1: Calm Start
                targetHeight = 0.15f;
                targetApm = 0.25f;
                level = 1;
            }
            else if (t < 9.0)
            {
                // Phase 2: Smooth Speedup
                double p = (t - 4.5) / 4.5;
                targetHeight = (float)(0.15 + p * 0.35);
                targetApm = (float)(0.25 + p * 0.55);
                level = 1 + (int)(p * 4.0); // 1 -> 5 smoothly
            }
            else if (t < 10.2)
            {
                // Phase 3: Tetris 4-Line Clear Drop (-8dB ducking on line drop)
                targetHeight = 0.30f;
                targetApm = 0.70f;
                level = 5;
                if (t >= 9.1 && t <= 9.6) targetDuckGain = 0.38; // Smooth -8.4 dB ducking dip
                else targetDuckGain = 1.0;
            }
            else if (t < 16.0)
            {
                // Phase 4: Red Alert Critical Danger (Height climbing high, panic double-kicks)
                double p = (t - 10.2) / 5.8;
                targetHeight = (float)(0.55 + p * 0.40);
                targetApm = (float)(0.75 + p * 0.25);
                level = 6 + (int)(p * 3.0); // 6 -> 9
            }
            else
            {
                // Phase 5: Game Over Analog Tape-Stop Pitch Dive
                double p = (t - 16.0) / 4.0;
                targetHeight = 1.0f;
                pitchMult = Math.Max(0.001, 1.0 - Math.Pow(p, 1.6));
                targetDuckGain = Math.Max(0.0, 1.0 - p * 0.85);
            }

            // Smooth parameter glide (EMA / LERP) — NO discontinuous jumps
            double rawTension = Math.Clamp(targetHeight * 0.6f + targetApm * 0.4f, 0.0f, 1.0f);
            smoothedTension += (rawTension - smoothedTension) * (1.0 - Math.Exp(-dt / 0.50)); // 500ms smooth response

            targetBpm = 120.0 + (level - 1) * 1.5 + targetApm * 6.0 + smoothedTension * 6.0;
            currentBpm += (targetBpm - currentBpm) * (1.0 - Math.Exp(-dt / 0.80)); // 800ms smooth tempo glide

            currentDuckGain += (targetDuckGain - currentDuckGain) * (1.0 - Math.Exp(-dt / 0.04)); // 40ms smooth ducking curve

            // Continuous phase accumulation: guarantees zero note glitches or time stuttering
            double beatsPerSec = currentBpm / 60.0;
            beatPhase += beatsPerSec * dt;

            // Shchedryk 16th-triplet step clock (3 steps per beat)
            double stepPhase = beatPhase * 3.0;
            int stepIdx = (int)Math.Floor(stepPhase);
            if (stepIdx != currentStep)
            {
                currentStep = stepIdx;
                noteTime = 0.0;
            }
            else
            {
                noteTime += dt;
            }

            int melodyStep = currentStep % shchedrykMelody.Length;
            int bassStep = (currentStep / 4) % bassProgression.Length;

            double beatFraction = beatPhase - Math.Floor(beatPhase);
            double beatTime = beatFraction * (60.0 / currentBpm);
            int stepBeat = ((int)Math.Floor(beatPhase)) % 4;

            // Lead Melody (Juno Bell)
            double leadFreq = shchedrykMelody[melodyStep] * pitchMult;
            // Octave lift in high danger
            if (smoothedTension > 0.72 && melodyStep < 8 && t < 16.0) leadFreq *= 2.0;

            double leadSignal = 0.0;
            if (leadFreq > 0.0)
            {
                double osc1 = DspUtils.BandlimitedPulse(ref _phaseLead, leadFreq, 0.50, SampleRate);
                double osc2 = DspUtils.BandlimitedSaw(ref _phaseLead2, leadFreq * 1.004, SampleRate);
                double raw = (osc1 * 0.5 + osc2 * 0.5) * Math.Exp(-noteTime * 4.2);
                
                double targetCutoff = (1200.0 + smoothedTension * 1700.0) + 900.0 * Math.Exp(-noteTime * 8.0);
                smoothedCutoff += (targetCutoff - smoothedCutoff) * (1.0 - Math.Exp(-dt / 0.01));
                
                leadSignal = _filterLeadL.Process(raw, smoothedCutoff, 1.4, SampleRate) * (0.60 + smoothedTension * 0.15);
            }

            // Bass (Moog walking bass)
            double bassFreq = bassProgression[bassStep] * pitchMult;
            double sawBass = DspUtils.BandlimitedSaw(ref _phaseBass, bassFreq, SampleRate) * 0.65;
            double subBass = DspUtils.Sine(ref _phaseSub, bassFreq * 0.5, SampleRate) * 0.55;
            double bassRaw = sawBass + subBass;
            double bassCutoff = 550.0 + smoothedTension * 600.0;
            double bassSignal = _filterBassL.Process(bassRaw, bassCutoff, 1.3, SampleRate);
            bassSignal = DspUtils.SoftSat(bassSignal, 1.2 + smoothedTension * 0.4);

            // Drums
            double kick = 0.0, snare = 0.0, hat = 0.0;
            if (t >= 16.0)
            {
                // Drums fade smoothly on tape-stop
                kick = 0.0;
                snare = 0.0;
                hat = 0.0;
            }
            else if (smoothedTension < 0.38)
            {
                // Calm: Light 808
                if (stepBeat == 0 || stepBeat == 2) kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime) * 0.7;
                hat = SynthHiHat(noteTime, isAccented: false) * 0.6;
            }
            else if (smoothedTension < 0.68)
            {
                // Speed: 4x4 Electro
                kick = SynthKick(beatTime);
                if (stepBeat == 1 || stepBeat == 3) snare = SynthSnare(beatTime);
                hat = SynthHiHat(noteTime, isAccented: (currentStep % 2 == 1));
            }
            else
            {
                // Panic: Double-kick drive
                kick = SynthKick(beatTime);
                if (noteTime < 0.05 && (currentStep % 4 == 2)) kick += SynthKick(noteTime) * 0.8;
                if (stepBeat == 1 || stepBeat == 3) snare = SynthIndustrialClapSnare(beatTime) * 0.9;
                hat = SynthHiHat(noteTime, isAccented: true);
            }

            double sidechain = Math.Min(1.0, beatTime / 0.075);
            bassSignal *= sidechain;

            double leadL = leadSignal * 0.9;
            double leadR = leadSignal * 1.1;

            double mixL = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadL;
            double mixR = (bassSignal * 0.60) + (kick * 0.80) + (snare * 0.65) + (hat * 0.35) + leadR;

            // Apply smooth ducking
            mixL *= currentDuckGain;
            mixR *= currentDuckGain;

            left[i] = (float)DspUtils.SoftSat(mixL * 0.70, 1.0);
            right[i] = (float)DspUtils.SoftSat(mixR * 0.70, 1.0);
        }

        return (left, right);
    }
    #endregion

    #region Clean Drum Synthesis (Zero Fuzz, Zero Blown-Speaker Clipping)
    private double SynthKick(double timeSec)
    {
        if (timeSec >= 0.25) return 0.0;
        // Smooth exponential pitch glide 130 Hz -> 48 Hz
        double pitch = 48.0 + 82.0 * Math.Exp(-timeSec * 45.0);
        double phase = 2.0 * Math.PI * (48.0 * timeSec + (82.0 / 45.0) * (1.0 - Math.Exp(-timeSec * 45.0)));
        double click = (_rand.NextDouble() * 2.0 - 1.0) * Math.Exp(-timeSec * 160.0) * 0.15;
        double env = Math.Exp(-timeSec * 18.0);
        return (Math.Sin(phase) + click) * env * 0.95;
    }

    private double SynthHeavyIndustrialKick(double timeSec)
    {
        if (timeSec >= 0.30) return 0.0;
        double phase = 2.0 * Math.PI * (42.0 * timeSec + (110.0 / 50.0) * (1.0 - Math.Exp(-timeSec * 50.0)));
        double subBody = Math.Sin(phase) * Math.Exp(-timeSec * 14.0);
        double punch = Math.Sin(phase * 2.0) * Math.Exp(-timeSec * 40.0) * 0.3;
        return DspUtils.SoftSat((subBody + punch) * 1.1, 1.2);
    }

    private double SynthSnare(double timeSec)
    {
        if (timeSec >= 0.22) return 0.0;
        double bodyTone = Math.Sin(2.0 * Math.PI * 190.0 * timeSec) * Math.Exp(-timeSec * 32.0) * 0.45;
        double noise = (_rand.NextDouble() * 2.0 - 1.0) * Math.Exp(-timeSec * 22.0) * 0.40;
        return (bodyTone + noise) * 0.85;
    }

    private double SynthIndustrialClapSnare(double timeSec)
    {
        if (timeSec >= 0.28) return 0.0;
        double tone = Math.Sin(2.0 * Math.PI * 175.0 * timeSec) * Math.Exp(-timeSec * 26.0) * 0.5;
        double clap1 = (_rand.NextDouble() * 2.0 - 1.0) * Math.Exp(-Math.Max(0, timeSec - 0.015) * 30.0) * 0.35;
        double clap2 = (_rand.NextDouble() * 2.0 - 1.0) * Math.Exp(-Math.Max(0, timeSec - 0.030) * 25.0) * 0.45;
        return (tone + clap1 + clap2) * 0.80;
    }

    private double SynthHiHat(double timeSec, bool isAccented)
    {
        if (timeSec >= 0.06) return 0.0;
        double noise = (_rand.NextDouble() * 2.0 - 1.0);
        double decayRate = isAccented ? 70.0 : 110.0;
        double gain = isAccented ? 0.30 : 0.18;
        return noise * Math.Exp(-timeSec * decayRate) * gain;
    }

    private double SynthMetallicClick(double timeSec)
    {
        if (timeSec >= 0.03) return 0.0;
        double ring = Math.Sin(2.0 * Math.PI * 3200.0 * timeSec) * Math.Exp(-timeSec * 140.0);
        return ring * 0.25;
    }
    #endregion
}
