# GodotProceduralSynth 🎹 🇺🇦

[![Made in Ukraine](https://img.shields.io/badge/Made%20in-Ukraine%20%F0%9F%87%BA%F0%9F%87%A6-ffd700?labelColor=0057b7)](https://github.com/YuriiBishchuk/godot-procedural-synth)
[![.NET Version](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Godot Engine](https://img.shields.io/badge/Godot-4.x%20C%23-478CBF?logo=godotengine&logoColor=white)](https://godotengine.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero-brightgreen)](#)
[![Live Demo](https://img.shields.io/badge/Live%20Demo-Web%20Audio%20Playground-06b6d4?logo=googlechrome&logoColor=white)](https://yuriibishchuk.github.io/godot-procedural-synth/)

> 🎮 **[Launch Live Web Audio Playground](https://yuriibishchuk.github.io/godot-procedural-synth/)** — Test the real-time DSP filters, adaptive sliders, and sound effects right in your browser!

**GodotProceduralSynth** is a universal, high-performance procedural audio and music synthesis library for **Godot 4** and modern C# (.NET 8 / 9 / 10), proudly developed as an open-source project in Ukraine 🇺🇦.

It allows game developers to generate **any sound effect, ambient atmosphere, or full adaptive dynamic soundtrack** in 100% real time — with **zero audio files on disk**, zero sample loading lag, and zero memory overhead.

---

## 🌟 Why Procedural Audio?

| Traditional Audio Assets | GodotProceduralSynth |
|---|---|
| Hundreds of megabytes of `.wav` / `.mp3` files | **Zero audio files** (pure mathematical DSP in code) |
| Repetitive, static loops | **Endlessly evolving**, reactive to player action |
| Disk read latency and memory caches | **Immediate streaming** into Godot's audio bus |
| Difficult to pitch/modulate without artifacts | **Real-time filter sweeps, ducking, and tempo changes** |

---

## 🇺🇦 Flagship Showcase Example: Cyber-Shchedryk

To demonstrate the full adaptive capabilities of the engine, the library includes a complete reference implementation of **Cyber-Shchedryk** — a dark electronic procedural re-imagining of Mykola Leontovych's iconic Ukrainian *Shchedryk* (*Carol of the Bells* motif in D-minor).

This showcase demonstrates how the library handles complex multi-layered procedural audio:
- **Procedural 4-Note Ostinato**: Mathematically synthesized bell lead voices with Juno-106 analog chorus diffusion.
- **Adaptive Groove Escalation**: Transitions seamlessly from a subtle sub-bass pulse to full 4-on-the-floor electro and high-velocity double-kicks as tension rises.
- **Dynamic Filter Sweeps**: 4-pole Moog ladder low-pass filters open dynamically with player speed and in-game danger.
- **Zero Phase Discontinuities**: Completely click-free streaming across arbitrary block boundaries.

```csharp
// Instant 1-line generation of the Cyber-Shchedryk showcase:
var (left, right) = engine.RenderTrack(
    SynthAesthetic.UkrainianCyberShchedryk, 
    AdaptiveAudioContext.Default, 
    durationSeconds: 15.0
);
```

---

## 🚀 Key Modules

### 1. 💥 `ProceduralSfx` — Instant Sound Effects
Generate classic arcade and modern sound effects with a single line of code, or customize them parametrically:

```csharp
using GodotProceduralSynth;

// Preset SFX (Lasers, Explosions, Hits, Jumps, Coins, UI Clicks, Whooshes)
float[] laser = ProceduralSfx.Generate(SfxType.Laser);
float[] boom  = ProceduralSfx.Generate(SfxType.Explosion);
float[] coin  = ProceduralSfx.Generate(SfxType.Coin);

// Custom parameterized sound design
float[] customSfx = ProceduralSfx.GenerateCustom(new SfxParameters
{
    StartFreq = 900.0,
    EndFreq = 120.0,
    Duration = 0.15,
    NoiseMix = 0.4,
    Distortion = 0.3,
    DecayRate = 22.0
});
```

### 2. 🎵 `UniversalSynthEngine` — Adaptive Music Synthesis
Synthesizes continuous stereo audio blocks with studio-grade DSP:
- **Bandlimited PolyBLEP Oscillators** (Saw, Pulse/PWM, Triangle, Sine, Noise)
- **4-Pole Resonant Filters** (Warm analog Juno-106 sweeps, Moog ladder low-pass)
- **Stereo Chorus & Delay Diffusion** (Rich wide stereo field without mono phase cancellation)
- **Soft Saturation Limiter** (Guarantees zero digital clipping and preserves 0 dBFS headroom)
- **Dynamic Ducking & Tape-Stop** (-8 dB sidechain dips and analog turntable spin-downs)

### 3. 🎮 `AdaptiveAudioContext` — Reactive Game Modulation
Feed your game state into the music engine to morph the soundtrack in real time:

```csharp
var context = new AdaptiveAudioContext
{
    IntensityLevel = playerLevel,  // Modulates BPM & drum complexity
    Tension = dangerRatio,         // Opens resonant filter cutoff (0.0 = calm, 1.0 = panic)
    Dissonance = hazardRatio,      // Introduces harmonic grit & dark sub-harmonics
    ActivityRate = inputApm,       // Drives arpeggio velocity and sub-bass groove
    IsDucking = scoredHit,         // Smooth -8dB sidechain dip on hits/combos
    PitchScale = timeScale         // Pitch bend (1.0 = normal, 0.0 = tape-stop game over)
};

engine.GenerateStreamBlock(leftBuffer, rightBuffer, count, context);
```

### 4. 📜 `TrackRecipe` — Declarative Recipe System
Create entirely new musical styles declaratively or choose from the **50+ built-in catalog recipes**:
- **Darksynth & Cyberpunk 2077**: Driving rolling 16th-note basslines, aggressive lead stabs
- **Industrial & Mid-Tempo**: Devastating half-time stomps, heavy distortion growls
- **Retro Arcade**: Nostalgic chiptune arpeggios, walking Moog basslines
- **Ambient & Atmospheric**: Evolving detuned pads and deep drone soundscapes
- **Folk & Ukrainian Themes**: Includes the Cyber-Shchedryk showcase recipe and variations

```csharp
var recipe = new TrackRecipe
{
    Title = "Neon Highway",
    Category = "Synthwave",
    Bpm = 128.0,
    RootFreq = 65.41, // C2
    BassPattern = BassPattern.Rolling16th,
    DrumPattern = DrumPattern.FourOnTheFloor,
    LeadStyle = LeadStyle.CyberArp,
    FilterCutoff = 2400.0,
    FilterResonance = 1.4
};

var (left, right) = engine.RenderFromRecipe(recipe);
```

---

## 🔌 Installation & Integration with Godot 4 (C#)

### Option A: Godot Addon (Recommended for Godot Editor)
1. Copy the `addons/godot_procedural_synth/` folder into your Godot project's `res://addons/` directory.
2. In Godot, go to **Project Settings -> Plugins** and enable **GodotProceduralSynth**.
3. You now have access to the custom **`ProceduralAudioStreamPlayer`** node directly in Godot's "Create New Node" dialog with full Inspector property controls!

### Option B: C# Project Reference / NuGet
Add a reference to `GodotProceduralSynth.csproj` in your game's `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="path/to/GodotProceduralSynth/src/GodotProceduralSynth/GodotProceduralSynth.csproj" />
</ItemGroup>
```

---

### Streaming Audio via `AudioStreamGenerator` (Pure Code)
Drop `samples/GodotIntegration/AdaptiveMusicEngine.cs` into your project:

```csharp
using Godot;
using GodotProceduralSynth;

public partial class MyAudioController : Node
{
    private AudioStreamGeneratorPlayback _playback;
    private readonly UniversalSynthEngine _engine = new();
    private readonly float[] _bufL = new float[1024];
    private readonly float[] _bufR = new float[1024];

    public override void _Ready()
    {
        var generator = new AudioStreamGenerator
        {
            MixRate = UniversalSynthEngine.SampleRate, // 44100 Hz
            BufferLength = 0.15f // 150ms buffer for seamless playback
        };

        var player = new AudioStreamPlayer { Stream = generator, Bus = "Music" };
        AddChild(player);
        player.Play();
        _playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback();
    }

    public override void _Process(double delta)
    {
        int available = _playback.GetFramesAvailable();
        if (available <= 0) return;

        int toGenerate = System.Math.Min(available, _bufL.Length);

        // Continuous procedural streaming with zero phase clicks
        _engine.GenerateStreamBlock(_bufL, _bufR, toGenerate, AdaptiveAudioContext.Default);

        for (int i = 0; i < toGenerate; i++)
        {
            _playback.PushFrame(new Vector2(_bufL[i], _bufR[i]));
        }
    }
}
```

---

## 🛠️ Standalone CLI Demo Harness

You can test and render samples on desktop without running Godot:

```bash
# Run the demo harness (renders SFX and music samples to ./output/)
dotnet run --project samples/AudioSynthHarness/AudioSynthHarness.csproj
```

---

## 🧪 Unit Tests

Run the test suite verifying mathematical safety, headroom preservation, and zero digital clipping:

```bash
dotnet test
```

---

## 📂 Project Structure

```
godot-procedural-synth/
├── src/
│   └── GodotProceduralSynth/        # Core library (.NET 8/9/10, zero dependencies)
│       ├── UniversalSynthEngine.cs  # Real-time multi-voice polyphonic audio generator
│       ├── ProceduralSfx.cs         # Universal SFX generator (Lasers, Hits, Explosions...)
│       ├── DspUtils.cs              # PolyBLEP oscillators, 4-pole filters, limiter
│       ├── SynthTypes.cs            # Universal AdaptiveAudioContext & aesthetic enums
│       └── TrackRecipe.cs           # Declarative 50-track catalog recipe engine
├── samples/
│   ├── AudioSynthHarness/           # Standalone CLI test harness
│   └── GodotIntegration/            # Copy-paste Godot 4 C# streaming node & docs
├── tests/
│   └── GodotProceduralSynth.Tests/  # Comprehensive xUnit test suite (10/10 passing)
└── GodotProceduralSynth.sln
```

---

## 📄 License

Licensed under the [MIT License](LICENSE). Free for both commercial and open-source game development.
