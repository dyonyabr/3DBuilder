// using Godot;
// using System;
// using System.Linq;
// using System.Runtime.InteropServices;

// public partial class FancyPlacing : Node3D
// {
// 	Player _Player;

// 	private SurfaceTool St;
// 	MeshInstance3D MeshInst; 
// 	Node3D MeshPivot;

// 	float Bump = 0.0f;

// 	public FancyPlacing(Player __Player)
// 	{
// 		Name = "FancyPlacing";
// 		_Player = __Player;
// 	}

// 	public override void _Ready()
// 	{
// 		St = new SurfaceTool();
// 		MeshPivot = new Node3D();
// 		AddChild(MeshPivot);
// 		MeshPivot.Name = "MeshPivot";
// 		MeshPivot.TopLevel = true;
// 		MeshInst = new MeshInstance3D();
// 		MeshPivot.AddChild(MeshInst);
// 		MeshInst.Name = "MeshInst";
// 		MeshInst.MaterialOverride = _Player._World.ChunkMaterial;
// 	}

// 	private void CreateBlock(BlockData BData)
// 	{
// 		St.Begin(Mesh.PrimitiveType.Triangles);
// 		St.GenerateNormals(false);

// 		for (int i = 0; i < 6; i++)
// 		{
// 			Vector3[] Verts1 = {CubeVerts[i * 4], CubeVerts[i * 4 + 1], CubeVerts[i * 4 + 2]};
// 			Vector3[] Verts2 = {CubeVerts[i * 4], CubeVerts[i * 4 + 2], CubeVerts[i * 4 + 3]};
// 			Vector2[] UVs1 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
// 							  new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
// 							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize};
// 			Vector2[] UVs2 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
// 							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
// 							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize};
// 			Color Shade1 = CubeShade[i * 2 + Convert.ToInt32(BData.Comp.Contains(BComp.Dualcolor))];
// 			Color Shade2 = CubeShade[i * 2];
// 			Color[] Colors1 = {Shade1,
// 							   Shade1,
// 							   Shade1};
// 			Color[] Colors2 = {Shade2,
// 							   Shade2, 
// 							   Shade2};
			
// 			St.AddTriangleFan(Verts1, UVs1, Colors1);
// 			St.AddTriangleFan(Verts2, UVs2, Colors2);
// 		}

// 		MeshInst.Mesh = St.Commit();
// 	}

// 	public override void _Process(double delta)
// 	{
// 		Bump = Mathf.Lerp(Bump, 0, (float)delta * 20);
// 		MeshInst.Position = MeshInst.Position.Lerp(new Vector3(0, Bump, 0), (float)delta * 20);
// 	}

// 	public async void Place(Vector3 GlobalPos, Block _Blcok)
// 	{
// 		BlockData BData = Blocks.AllBlocks[_Blcok.Id];
// 		CreateBlock(BData);
// 		MeshInst.Visible = true;
// 		MeshPivot.GlobalPosition = _Player.GunEnd.Position;
// 		Bump = 2;

// 		Tween _Tween = CreateTween();
// 		_Tween.TweenProperty(MeshPivot, "global_position", GlobalPos, .2f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
// 		await ToSignal(_Tween, "finished");
// 		_Player.PlaceBlock(GlobalPos, _Blcok);
// 		MeshInst.Visible = false;
// 	}

// 	private Vector3[] CubeVerts = {
// 		new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
// 		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0),
// 		new Vector3(0, 1, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0),
// 		new Vector3(1, 1, 0), new Vector3(1, 0 ,0), new Vector3(1, 0, 1), new Vector3(1, 1, 1),
// 		new Vector3(0, 1, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0),
// 		new Vector3(1, 1, 1), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 1)
// 	};

// 	private Color[] CubeShade = {
// 		new Color(1,1,1,1),       new Color(.9f,.9f,.9f,1),
// 		new Color(.4f,.4f,.4f,1), new Color(.3f,.3f,.3f,1),
// 		new Color(.8f,.8f,.8f,1), new Color(.7f,.7f,.7f,1),
// 		new Color(.8f,.8f,.8f,1), new Color(.7f,.7f,.7f,1),
// 		new Color(.6f,.6f,.6f,1), new Color(.5f,.5f,.5f,1),
// 		new Color(.6f,.6f,.6f,1), new Color(.5f,.5f,.5f,1)
// 	};
// }
