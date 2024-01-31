using Godot;

public partial class Player : RigidBody3D
{
    [Export] public float Speed = 1000f;

    [Export] public float CameraVerticalSensitivity = 0.001f;
    [Export] public float CameraHorizontalSensitivity = 0.001f;
    [Export] public Node3D CameraPivotYaw = null;
    [Export] public Node3D CameraPivotPitch = null;
    [Export] public Node3D Camera = null;
    [Export] public Vector2 CameraPitchLimit = new(-1.5f, 1.5f);

    Vector3 input = Vector3.Zero;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        Move(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouse)
            Look(mouse);

        base._UnhandledInput(@event);
    }

    //

    private void Move(double delta)
    {
        input.Z = -Input.GetAxis("move_b", "move_f");
        input.X = Input.GetAxis("move_l", "move_r");

        ApplyCentralForce(CameraPivotYaw.Basis * input.Normalized() * Speed * (float)delta);
    }

    private void Look(InputEventMouseMotion mouse)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            return;

        var camYaw = -mouse.Relative.X * CameraHorizontalSensitivity;
        var camPitch = -mouse.Relative.Y * CameraVerticalSensitivity;

        if (CameraPivotPitch.Rotation.X + camPitch < CameraPitchLimit.X)
            camPitch = CameraPitchLimit.X - CameraPivotPitch.Rotation.X;
        else if (CameraPivotPitch.Rotation.X + camPitch > CameraPitchLimit.Y)
            camPitch = CameraPitchLimit.Y - CameraPivotPitch.Rotation.X;

        CameraPivotYaw.RotateY(camYaw);
        CameraPivotPitch.RotateX(camPitch);
    }
}
