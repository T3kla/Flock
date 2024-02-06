using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

namespace Flock;

struct Boid
{
    public Transform3D Transform = Transform3D.Identity;
    public Vector3 Velocity = Vector3.Zero;
    public Color Color = Colors.White;

    public Vector3I Voxel = Vector3I.Zero;

    public Boid() { }

    public readonly bool IsAdjacentTo(Boid other, int distance = 1)
        => Math.Abs(Voxel.X - other.Voxel.X) <= distance
        && Math.Abs(Voxel.Y - other.Voxel.Y) <= distance
        && Math.Abs(Voxel.Z - other.Voxel.Z) <= distance;
}

public partial class Boids : MultiMeshInstance3D
{
    [Export] Node3D player = null;

    [ExportGroup("Void Parameters")]

    [Export] int amount = 1000;

    [ExportSubgroup("Speed")]
    [Export] float targetSpeed = 10f;
    [Export(PropertyHint.Range, "0,1")] float speedCorrection = 10f;

    [ExportSubgroup("Bounds")]
    [Export] Vector3 bounds = new(40f, 20f, 40f);
    [Export] float boundsYOffset = 20f;
    [Export(PropertyHint.Range, "0,10")] float boundsAvoidanceStrength = 10f;
    [Export(PropertyHint.Range, "0,10")] float boundsDeflectionStrength = 3f;

    [ExportSubgroup("Neighbour Influence")]
    [Export] float neighbourInfluenceRadius = 10f;
    [Export(PropertyHint.Range, "0,1")] float neighbourInfluenceStrength = 10f;

    [ExportGroup("Optimisations")]
    [Export] bool parallelUpdate = true;
    [Export] bool spatialHashing = true;
    [Export] bool showCost = false;

    private readonly List<Boid> boids = new();

    private int hashVoxelSize = 10;

    public override void _Ready()
    {
        player ??= GetNode<Node3D>("/root/Player");

        if (player == null)
        {
            GD.PrintErr("Player node not found!");
            return;
        }

        if (spatialHashing)
        {
            hashVoxelSize = (int)Math.Ceiling(neighbourInfluenceRadius * 3f / 4f);
            GD.Print($"Hash Voxel Size: {hashVoxelSize}");
        }

        Multimesh.InstanceCount = amount;
        boids.Capacity = amount;

        for (int a = 0; a < amount; a++)
        {
            float x = (float)GD.RandRange(-bounds.X, bounds.X);
            float y = (float)GD.RandRange(-bounds.Y, bounds.Y) + boundsYOffset;
            float z = (float)GD.RandRange(-bounds.Z, bounds.Z);

            float i = (float)GD.RandRange(-1d, 1d);
            float j = (float)GD.RandRange(-1d, 1d);
            float k = (float)GD.RandRange(-1d, 1d);

            boids.Add(new Boid
            {
                Transform = new(Basis.Identity, new(x, y, z)),
                Velocity = new Vector3(i, j, k).Normalized() * targetSpeed,
                Color = new Color(1f, 1f, 1f)
            });
        }
    }

    public override void _Process(double delta)
    {
        var d = (float)delta;

        var sw = new Stopwatch();
        sw.Start();
        if (parallelUpdate)
            Parallel.For(0, Multimesh.InstanceCount, i => { UpdateBoid(i, d); });
        else
            for (int i = 0; i < Multimesh.InstanceCount; i++) UpdateBoid(i, d);
        sw.Stop();

        if (showCost) GD.Print($"Update cost: {sw.ElapsedMilliseconds}ms");
    }

    private void UpdateBoid(int i, float d)
    {
        var b0 = boids[i];

        // Update voxel

        if (spatialHashing)
            b0.Voxel = new(
                (int)(b0.Transform.Origin.X / hashVoxelSize),
                (int)(b0.Transform.Origin.Y / hashVoxelSize),
                (int)(b0.Transform.Origin.Z / hashVoxelSize));

        // Neighbour Influence

        if (parallelUpdate)
            Parallel.For(0, Multimesh.InstanceCount, i => { CheckNeighbours(d, i, ref b0); });
        else
            for (int j = 0; j < Multimesh.InstanceCount; j++) CheckNeighbours(d, j, ref b0);

        void CheckNeighbours(float d, int i, ref Boid b0)
        {
            var b1 = boids[i];

            if (!b0.IsAdjacentTo(b1))
                return;

            var dis = b0.Transform.Origin.DistanceTo(b1.Transform.Origin);

            if (dis > neighbourInfluenceRadius)
                return;

            var neighbourInfluence = neighbourInfluenceStrength * (1f - dis / neighbourInfluenceRadius);

            b0.Velocity = b0.Velocity.Lerp(b1.Velocity, neighbourInfluence * d);
        }

        // Apply velocity and look forward

        b0.Transform = b0.Transform.Translated(b0.Velocity * d);
        if (b0.Velocity.Length() > 0f)
        {
            b0.Transform = b0.Transform.LookingAt(b0.Velocity + b0.Transform.Origin, -Vector3.Forward);
            b0.Transform.Basis = b0.Transform.Basis.Rotated(b0.Transform.Basis.X, -Mathf.Pi / 2f);
        }

        // Avoid bounds

        float x = b0.Velocity.X, y = b0.Velocity.Y, z = b0.Velocity.Z;

        if (b0.Transform.Origin.X < -bounds.X)
            x = Mathf.Lerp(b0.Velocity.X, boundsDeflectionStrength, boundsAvoidanceStrength * d);
        if (b0.Transform.Origin.X > bounds.X)
            x = Mathf.Lerp(b0.Velocity.X, -boundsDeflectionStrength, boundsAvoidanceStrength * d);
        if (b0.Transform.Origin.Y < -bounds.Y + boundsYOffset)
            y = Mathf.Lerp(b0.Velocity.Y, boundsDeflectionStrength, boundsAvoidanceStrength * d);
        if (b0.Transform.Origin.Y > bounds.Y + boundsYOffset)
            y = Mathf.Lerp(b0.Velocity.Y, -boundsDeflectionStrength, boundsAvoidanceStrength * d);
        if (b0.Transform.Origin.Z < -bounds.Z)
            z = Mathf.Lerp(b0.Velocity.Z, boundsDeflectionStrength, boundsAvoidanceStrength * d);
        if (b0.Transform.Origin.Z > bounds.Z)
            z = Mathf.Lerp(b0.Velocity.Z, -boundsDeflectionStrength, boundsAvoidanceStrength * d);

        b0.Velocity = new(x, y, z);

        // Direct velocity towards target speed

        b0.Velocity = b0.Velocity.Lerp(b0.Velocity.Normalized() * targetSpeed, speedCorrection * d);

        // Save changes

        boids[i] = b0;
        Multimesh.SetInstanceTransform(i, b0.Transform);
    }

    private static Vector3 RandomCone(Vector3 v, float a)
    {
        var m = v.Length();

        var x = v.X;
        var y = v.Y;
        var z = v.Z;

        var r = new Vector3(x, y, z);
        var r2 = new Vector3(x, y, z);
        var r3 = new Vector3(x, y, z);

        r = r.Rotated(Vector3.Up, GD.Randf() * a);
        r2 = r2.Rotated(Vector3.Right, GD.Randf() * a);
        r3 = r3.Rotated(Vector3.Forward, GD.Randf() * a);

        return (r + r2 + r3).Normalized() * m;
    }
}
