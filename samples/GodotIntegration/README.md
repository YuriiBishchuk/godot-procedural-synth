# Godot 4 Integration Guide

Integrating `GodotProceduralSynth` into your Godot 4 C# (.NET) project is as simple as adding an `AudioStreamGenerator`.

## 1. Add Project Reference / DLL
Add a reference to `GodotProceduralSynth.csproj` in your game's `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="path/to/GodotProceduralSynth.csproj" />
</ItemGroup>
```

## 2. Attach `AdaptiveMusicEngine.cs`
1. Add `AdaptiveMusicEngine.cs` to your project tree or as an Autoload Singleton in `Project Settings -> Globals -> Autoload`.
2. Connect your game signals (speed, player score, danger state) to `AdaptiveMusicEngine.Instance.UpdateGameState(...)`.

## 3. Real-Time Parameters
| Parameter | Description |
|-----------|-------------|
| `Level` | Controls base tempo and rhythm intensity |
| `TowerHeight` | Opens Moog low-pass filter cutoff, introducing high-register tension |
| `HoleCount` | Increases harmonic distortion and dissonance |
| `Apm` | Modulates arpeggiator velocity and sub-bass groove |
| `TetrisDucking` | Smooth -8dB sidechain ducking on line clears / combo hits |
| `GameOverPitch` | Smooth analog tape-stop pitch dive when the match ends |
