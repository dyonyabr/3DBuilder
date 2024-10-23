using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BlockSelector : SubViewportContainer
{
	Player _Player;
	private SurfaceTool St = new SurfaceTool();

	private SubViewport Viewport;
	private Node3D CameraPivot;
	private Camera3D Camera;
	private Node3D Icons;
	private Node2D Shadows;
	private Texture2D ShadowTexture = GD.Load<Texture2D>("res://Assets/Textures/BlockItemShadow.tres");

	public List<BlockItem> BlockList = new List<BlockItem>();

	public BlockSelector(Player __Player)
	{
		Name = "BlockSelector";
		_Player = __Player;
	}

	public int CurBlock = 0;
	public ushort CurBlockId = 0;
	public void SetCurBlock(int Value)
	{
		CurBlock = Mathf.PosMod(Value, BlockList.Count());
		CurBlockId = BlockList[CurBlock].Id;
		_Player.RArmImpact = -1f;
		_Player.ItemMesh.Mesh = Icons.GetChild<MeshInstance3D>(CurBlock).Mesh;
	}

	public void AddBlock(ushort Id)
	{
		BlockList.Add(new BlockItem(Id));
		CreateBlock(Blocks.AllBlocks[Id], BlockList.Count() - 1);
	}

	public void DeleteBlock(int Index)
	{
		BlockList.RemoveAt(Index);
		MeshInstance3D Icon = Icons.GetNode<MeshInstance3D>("Icon" + Index);
		Icon.QueueFree();
	}

	public override void _Ready()
	{
		Shadows = new Node2D();
		AddChild(Shadows);
		Shadows.ZIndex = -1;

		Viewport = new SubViewport();
		AddChild(Viewport);
		Viewport.Name = "Viewport";
		Viewport.OwnWorld3D = true;
		Viewport.TransparentBg = true;

		Stretch = true;
		
		CameraPivot = new Node3D();
		Viewport.AddChild(CameraPivot);
		CameraPivot.Name = "CameraPivot";
		CameraPivot.Rotation = new Vector3(Mathf.DegToRad(-35), Mathf.Pi/4, 0);

		Camera = new Camera3D();
		CameraPivot.AddChild(Camera);
		Camera.Name = "Camera";
		Camera.Projection = Camera3D.ProjectionType.Orthogonal;
		Camera.Size = 4;
		Camera.Position = new Vector3(0, 0, 100);
		Camera.CullMask = 2;
		Camera.Environment = new Godot.Environment();
		Camera.Environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		Camera.Environment.AmbientLightColor = new Color(1,1,1,1);

		Icons = new Node3D();
		Viewport.AddChild(Icons);
		Icons.Name = "Icons";

		AddBlock(1);
		AddBlock(2);
		AddBlock(3);
		AddBlock(4);
		AddBlock(5);
		AddBlock(6);
		AddBlock(7);
		AddBlock(8);
		AddBlock(9);
		AddBlock(10);
		AddBlock(11);
		AddBlock(12);
		AddBlock(13);
		AddBlock(14);
		AddBlock(15);
		AddBlock(16);
		AddBlock(17);
		AddBlock(18);
		AddBlock(19);
		AddBlock(20);
		AddBlock(21);
		AddBlock(22);

		SetCurBlock(0);
	}

	public override void _Process(double delta)
	{
		Size = new Vector2(10000, 100 * Global.UiScale);

		Vector2 WindowSize = GetViewport().GetVisibleRect().Size;
		Position = new Vector2(WindowSize.X/2 - Size.X/2, WindowSize.Y - Size.Y);
		CameraPivot.Position = new Vector3(BlockList.Count()/2 - .5f, 0, -BlockList.Count()/2 + .5f);

		for (int i = 0; i < BlockList.Count(); i++)
		{
			MeshInstance3D MeshInst = Icons.GetChild<MeshInstance3D>(i);
			MeshInst.Scale = MeshInst.Scale.Lerp(Vector3.One + Vector3.One * Convert.ToInt32(i == CurBlock) * 0.25f, (float)delta * 20);
			MeshInst.Position = MeshInst.Position.Lerp(new Vector3(i, 0, -i) + Vector3.Up * Convert.ToInt32(i == CurBlock) * 0.5f, (float)delta * 20);

			Sprite2D Shadow = Shadows.GetChild<Sprite2D>(i);
			Shadow.Position = Camera.UnprojectPosition(MeshInst.Position);
			Shadow.Scale = Vector2.One * .25f * Global.UiScale;
		}

		if ((!Input.IsActionPressed("CamSwitch") && Input.IsActionJustPressed("ZoomOut")) || Input.IsActionJustPressed("NextItem")) { SetCurBlock(CurBlock + 1); }
		else if ((!Input.IsActionPressed("CamSwitch") && Input.IsActionJustPressed("ZoomIn")) || Input.IsActionJustPressed("PrevItem")) { SetCurBlock(CurBlock - 1); }
	}

	private void CreateBlock(BlockData BData, int Index)
	{
		MeshInstance3D MeshInst = new MeshInstance3D();
		Icons.AddChild(MeshInst);
		MeshInst.MaterialOverride = _Player._World.ChunkMaterialTransparent;
		MeshInst.Layers = 2;
		St.Begin(Mesh.PrimitiveType.Triangles);
		St.GenerateNormals(false);


		for (int i = 0; i < 6; i++)
		{
			Vector3[] Verts1 = {CubeVerts[i * 4], CubeVerts[i * 4 + 1], CubeVerts[i * 4 + 2]};
			Vector3[] Verts2 = {CubeVerts[i * 4], CubeVerts[i * 4 + 2], CubeVerts[i * 4 + 3]};
			Vector2[] UVs1 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
							  new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize};
			Vector2[] UVs2 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
							  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize};
			Color Shade1 = CubeShade[i * 2 + Convert.ToInt32(BData.Comp.Contains(BComp.Dualcolor))];
			Color Shade2 = CubeShade[i * 2];
			Color[] Colors1 = {Shade1,
							   Shade1,
							   Shade1};
			Color[] Colors2 = {Shade2,
							   Shade2, 
							   Shade2};
			
			St.AddTriangleFan(Verts1, UVs1, Colors1);
			St.AddTriangleFan(Verts2, UVs2, Colors2);
		}

		MeshInst.Mesh = St.Commit();
		MeshInst.Name = "Icon" + Index;

		Sprite2D Shadow = new Sprite2D();
		Shadows.AddChild(Shadow);
		Shadow.Texture = ShadowTexture;
	}

	private Vector3[] CubeVerts = {
		new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0),
		new Vector3(0, 1, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0),
		new Vector3(1, 1, 0), new Vector3(1, 0 ,0), new Vector3(1, 0, 1), new Vector3(1, 1, 1),
		new Vector3(0, 1, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0),
		new Vector3(1, 1, 1), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 1)
	};

	private Color[] CubeShade = {
		new Color(1,1,1,1),       new Color(.9f,.9f,.9f,1),
		new Color(.4f,.4f,.4f,1), new Color(.3f,.3f,.3f,1),
		new Color(.8f,.8f,.8f,1), new Color(.7f,.7f,.7f,1),
		new Color(.8f,.8f,.8f,1), new Color(.7f,.7f,.7f,1),
		new Color(.6f,.6f,.6f,1), new Color(.5f,.5f,.5f,1),
		new Color(.6f,.6f,.6f,1), new Color(.5f,.5f,.5f,1)
	};
}
