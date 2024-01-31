using System;
using System.Collections.Generic;
using Godot;

struct VoidsInstance
{
    public Transform3D Transform = Transform3D.Identity;
    public Vector3 Velocity = Vector3.Zero;
    public Color Color = Colors.White;

    public VoidsInstance()
    {
    }
}

public partial class Voids : MultiMeshInstance3D
{
    [Export] public Node3D player = null;

    [Export] public int amount = 1000;
    [Export] public float speed = 1000f;
    [Export] public float distance = 1000f;

    private List<VoidsInstance> voids = new();

    public override void _Ready()
    {
        player ??= GetNode<Node3D>("/root/Player");

        if (player == null)
        {
            GD.PrintErr("Player node not found!");
            return;
        }

        Multimesh.InstanceCount = amount;
        voids.Capacity = amount;

        for (int a = 0; a < amount; a++)
        {
            float x = (float)GD.RandRange(-distance, distance);
            float y = (float)GD.RandRange(-distance, distance) + player.Position.Y;
            float z = (float)GD.RandRange(-distance, distance);

            float i = (float)GD.RandRange(-1d, 1d);
            float j = (float)GD.RandRange(-1d, 1d);
            float k = (float)GD.RandRange(-1d, 1d);

            voids.Add(new VoidsInstance
            {
                Transform = new(Basis.Identity, new(x, y, z)),
                Velocity = new Vector3(i, j, k).Normalized() * speed,
                Color = new Color(1f, 1f, 1f)
            });
        }
    }

    public override void _Process(double delta)
    {
        var d = (float)delta;
        var v = new VoidsInstance();

        for (int i = 0; i < Multimesh.InstanceCount; i++)
        {
            v = voids[i];
            v.Transform.Origin += voids[i].Velocity * d;
            voids[i] = v;

            Multimesh.SetInstanceTransform(i, v.Transform);
        }
    }
}
