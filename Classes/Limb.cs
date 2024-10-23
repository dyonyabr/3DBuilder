using Godot;
using System;

public partial class Limb : Node3D
{
	public Node3D B { get; set; }
	float Radius { get; set; }
	StandardMaterial3D Material { get; set; }
	MeshInstance3D MeshInst;
	CylinderMesh Cylinder;

	public Limb(Node3D _B, float _Radius, StandardMaterial3D _Material, string _Name = "")
	{
		Name = "Limb" + _Name;
		B = _B;
		Radius = _Radius;
		Material = _Material;
	}

	public override void _Ready()
	{
		MeshInst = new MeshInstance3D();
		AddChild(MeshInst);
		Cylinder = new CylinderMesh(); 
		Cylinder.TopRadius = Radius;
		Cylinder.BottomRadius = Radius;
		MeshInst.Mesh = Cylinder;
		MeshInst.MaterialOverride = Material;
	}

	public override void _Process(double delta)
	{
		if (B.GlobalPosition.DirectionTo(GlobalPosition) != Vector3.Up)
		{
			LookAt(B.GlobalPosition);
		}
		Cylinder.Height = GlobalPosition.DistanceTo(B.GlobalPosition);
		MeshInst.Position = new Vector3(0, 0, -Cylinder.Height/2);
		MeshInst.Rotation = new Vector3(-Mathf.Pi/2, 0, 0);
	}
}
