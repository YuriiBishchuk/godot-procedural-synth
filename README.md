# GodotProceduralSynth 🎹🇺🇦

[![.NET Version](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Godot Engine](https://img.shields.io/badge/Godot-4.x%20C%23-478CBF?logo=godotengine&logoColor=white)](https://godotengine.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero-brightgreen)](#)

A high-performance, real-time procedural audio synthesizer library in C# for **Godot 4** and standalone .NET applications.

Synthesizes interactive, adaptive music in 100% real-time using Godot's `AudioStreamGenerator` with **zero static audio assets**, zero WAV/MP3 files, and zero disk overhead. Features the iconic **Ukrainian Cyber-Shchedryk** (Carol of the Bells motif in D-minor), darksynth/cyberpunk aesthetics, warm analog Juno-106 chorus filters, Moog ladders, and dynamic gameplay modulation.

---

## ✨ Features

- **⚡ 100% Real-Time Synthesis**: Streams audio blocks directly into Godot's `AudioStreamGeneratorPlayback` without pre-rendering or disk I/O.
- **🇺🇦 Cyber-Shchedryk Motif**: Authentic procedural arrangement of Mykola Leontovych's *Shchedryk* in D-minor modulated by electronic kick/snare grooves, sub-bass, and polyphonic arpeggios.
- **🎮 Adaptive Gameplay Modulation**:
  - **Dynamic BPM & Intensity**: Scales tempo and drum complexity with game level / speed.
  - **Tension Cutoff Filter**: Real-time Juno-106 low-pass filter opening as danger/tower height increases.
  - **APM Arp Density**: Fast player input dynamically drives arpeggiator velocity and sub-bass groove.
  - **Sidechain Ducking**: Instant -8 dB dip on line clears / combo hits with smooth analog release.
  - **Game Over Tape-Stop**: Realistic analog tape-stop pitch dive when the match concludes.
- **🎛️ Studio-Grade DSP Pipeline**:
  - Bandlimited PolyBLEP oscillators (Saw, Pulse/PWM, Triangle, Sine).
  - 4-pole cascaded resonant filters with drive saturation.
  - Stereo spread chorus and multi-tap spatial delay diffusion.
  - Headroom-protected soft saturation limiter preserving 0 dBFS margin without digital clipping.
- **📦 Zero Dependencies**: Pure managed C# (.NET 8/9/10). No native C++ wrappers, external DLLs, or NuGet bloat.

---

## 📂 Repository Structure

```
godot-procedural-synth/
├── src/
│   └── GodotProceduralSynth/        # Core synthesizer library (.NET 8/9/10)
│       ├── UniversalSynthEngine.cs  # Real-time stream generation & voice manager
│       ├── DspUtils.cs              # PolyBLEP oscillators, Moog/Juno filters, DSP analyzer
│       ├── SynthTypes.cs            # Aesthetic modes, context data structures
│       └── TrackRecipe.cs           # 50-track procedural catalog recipes
├── samples/
│   ├── AudioSynthHarness/           # Standalone CLI runner to test & export WAVs
│   │   └── Program.cs
│   └── GodotIntegration/            # Copy-paste Godot 4 Node example
│       ├── AdaptiveMusicEngine.cs   # AudioStreamGenerator integration
│       └── README.md
├── tests/
│   └── GodotProceduralSynth.Tests/  # xUnit test suite (Headroom, Harshness, DSP)
│       └── AudioSynthesisTests.cs
└── GodotProceduralSynth.sln
```

---

## 🚀 Quick Start in Godot 4 (C#)

### 1. Add Reference to your Godot project
Reference `GodotProceduralSynth.csproj` or compile it into a DLL and drop it into your Godot project folder:

```xml
<ItemGroup>
  <ProjectReference Include="../godot-procedural-synth/src/GodotProceduralSynth/GodotProceduralSynth.csproj" />
</ItemGroup>
```

### 2. Create the Stream Player Node
In your Godot C# script:

```csharp
using Godot;
using GodotProceduralSynth;

public partial class MyMusicController : Node
{
    private AudioStreamGeneratorPlayback _playback;
    private readonly UniversalSynthEngine _synth = new();
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

        int count = System.Math.Min(available, _bufL.Length);

        var context = new GameplayAudioContext
        {
            Level = 3,
            TowerHeight = 8,
            HoleCount = 2,
            Apm = 2.4,
            TetrisDucking = false
        };

        // Generates continuous stereo frames with zero phase clicks
        _synth.GenerateStreamBlock(_bufL, _bufR, count, context);

        for (int i = 0; i < count; i++)
        {
            _playback.PushFrame(new Vector2(_bufL[i], _bufR[i]));
        }
    }
}
```

---

## 🧪 Testing & Verification

Run the test suite covering headroom verification, spectral harshness analysis, and buffer continuity:

```bash
dotnet test
```

Run the standalone audio demo harness to generate sample audio files:

```bash
dotnet run --project samples/AudioSynthHarness/AudioSynthHarness.csproj
```

---

## 📄 License

Licensed under the [MIT License](LICENSE). Free for commercial and non-commercial game development.
