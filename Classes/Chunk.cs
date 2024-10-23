using Godot;
using System;

public partial class Chunk : Node3D
{
	private World _World;
	
	public Vector2 ChunkPos;

	public MeshInstance3D MeshInst;
	public StaticBody3D SolidBody;
	public CollisionShape3D SolidShape;

	public Chunk(World __World)
	{
		_World = __World;
	}

	public void SetVisibility(bool Togle, bool Deferred)
	{
		if (Deferred) { SetDeferred("visible", Togle); }
		else { Visible = Togle; }
	}

	public void SetChunkPos(Vector2 _ChunkPos)
	{
		ChunkPos = _ChunkPos;
		SetDeferred("position", new Vector3(ChunkPos.X * Global.ChunkSize.X, 0, ChunkPos.Y * Global.ChunkSize.Z));
		Name = "x"+ChunkPos.X+"y"+ChunkPos.Y;
	}

	public override void _Ready()
	{
		MeshInst = new MeshInstance3D();
		AddChild(MeshInst);
		MeshInst.Name = "MeshInst";

		SolidBody = new StaticBody3D();
		AddChild(SolidBody);
		SolidBody.Name = "SolidBody";
		SolidBody.SetCollisionLayerValue(1, false);
		SolidBody.SetCollisionMaskValue(1, false);
		SolidBody.SetCollisionLayerValue(2, true);
		SolidBody.SetCollisionLayerValue(3, true);

		SolidShape = new CollisionShape3D();
		SolidBody.AddChild(SolidShape);
		SolidShape.Name = "SolidShape";
	}

	public override void _Process(double delta)
	{
	}
}
