using Godot;

namespace Flock;

public partial class World : Node3D
{
    [ExportCategory("Initial")]
    [Export] Input.MouseModeEnum MouseMode = Input.MouseModeEnum.Captured;

    public override void _Ready()
    {
        Input.MouseMode = MouseMode;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
            GetTree()?.Quit();
    }
}
