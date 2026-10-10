#if TOOLS
using Godot;

namespace GodotProceduralSynth.Addon;

[Tool]
public partial class ProceduralAudioPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("res://addons/godot_procedural_synth/ProceduralAudioStreamPlayer.cs");
        var icon = GD.Load<Texture2D>("res://addons/godot_procedural_synth/icon.svg");
        AddCustomType("ProceduralAudioStreamPlayer", "AudioStreamPlayer", script, icon);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("ProceduralAudioStreamPlayer");
    }
}
#endif
