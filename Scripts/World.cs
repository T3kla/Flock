using Godot;

public partial class World : Node3D
{
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
            GetTree()?.Quit();
    }
}
