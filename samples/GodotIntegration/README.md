# Godot 4 Integration Guide

Integrating `GodotProceduralSynth` into your Godot 4 C# (.NET) game enables 100% real-time procedural sound and music with zero audio assets on disk.

---

## 1. Add Library Reference
Reference `GodotProceduralSynth.csproj` in your Godot project's `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="path/to/GodotProceduralSynth.csproj" />
</ItemGroup>
```

---

## 2. Real-Time Procedural Music
Add `AdaptiveMusicEngine.cs` as an Autoload Singleton or attach it to your main scene tree.

### Modulating Music with Game State
Call `UpdateGameState` whenever game conditions change:
```csharp
AdaptiveMusicEngine.Instance.UpdateGameState(
    intensityLevel: playerLevel,  // Modulates BPM & drum arrangement
    tension: dangerRatio,         // Opens Moog low-pass filter (0.0 = calm, 1.0 = panic)
    dissonance: hazardRatio,      // Introduces harmonic grit & distortion
    activityRate: playerApm,      // Modulates arpeggio velocity and sub-bass groove
    isDucking: scoredHit,         // Sidechain ducking dip (-8dB) on hits/combos
    pitchScale: timeDilationScale // 1.0 = normal, 0.0 = analog tape-stop on game over
);
```

---

## 3. Instant Procedural Sound Effects (SFX)
Synthesize instant game sound effects directly in code without loading audio files:

```csharp
using Godot;
using GodotProceduralSynth;

public partial class MyPlayer : CharacterBody2D
{
    public void ShootLaser()
    {
        float[] samples = ProceduralSfx.Generate(SfxType.Laser);
        PlayRawPcm(samples);
    }

    public void OnExplosion()
    {
        float[] samples = ProceduralSfx.Generate(SfxType.Explosion);
        PlayRawPcm(samples);
    }
}
```

---

## 4. Custom Parameterized Sound Effects
Design unique sounds mathematically with `SfxParameters`:

```csharp
var customWhoosh = ProceduralSfx.GenerateCustom(new SfxParameters
{
    StartFreq = 600.0,
    EndFreq = 100.0,
    Duration = 0.2,
    NoiseMix = 0.7,
    Distortion = 0.3,
    DecayRate = 14.0
});
```
