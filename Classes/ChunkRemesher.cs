using Godot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class ChunkRemesher : Node
{
	private World _World;

	public ChunkRemesher(World __World)
	{
		_World = __World;
	} 

	public override void _Process(double delta)
	{
	}

	public void Remesh(Vector2 ChunkPos)
	{

		SurfaceTool St = new SurfaceTool();
		SurfaceTool StT = new SurfaceTool();
		SurfaceTool StS = new SurfaceTool();
		SurfaceTool StTS = new SurfaceTool();

		St.Begin(Mesh.PrimitiveType.Triangles);
		StT.Begin(Mesh.PrimitiveType.Triangles);
		StS.Begin(Mesh.PrimitiveType.Triangles);
		StTS.Begin(Mesh.PrimitiveType.Triangles);

		for (int x = 0; x < Global.ChunkSize.X; x++)
		{
			for (int z = 0; z < Global.ChunkSize.Z; z++)
			{
				for (int y = 0; y < Global.ChunkSize.Y; y++)
				{
					Vector3 BlockPos = new Vector3(x, y, z);
					Block B = _World.GetBlockLocal(ChunkPos, BlockPos);
					BlockData BData = Blocks.AllBlocks[B.Id];

					if (!BData.Comp.Contains(BComp.Unvisible)) { DrawCube(St, StT, StS, StTS, ChunkPos, BlockPos, BData); }
				}
			}
		}

		St.GenerateNormals(false);
		StT.GenerateNormals(false);
		StS.GenerateNormals(false);
		StTS.GenerateNormals(false);

		ArrayMesh ArrMeshSolid = StS.Commit();
		StTS.Commit(ArrMeshSolid);

		if (ArrMeshSolid.GetSurfaceCount() > 0)
		{
			St.AppendFrom(ArrMeshSolid, 0, Transform3D.Identity);
			if (ArrMeshSolid.GetSurfaceCount() > 1)
			{
				StT.AppendFrom(ArrMeshSolid, 1, Transform3D.Identity);
			}
		}

		ArrayMesh ArrMesh = St.Commit();
		StT.Commit(ArrMesh);
		

		St.Clear();
		StT.Clear();
		StS.Clear();
		StTS.Clear();

		_World.CallDeferred("ApplyRemesh", ChunkPos, ArrMesh, ArrMeshSolid);
	}

	private void DrawCube(SurfaceTool St, SurfaceTool StT, SurfaceTool StS, SurfaceTool StTS, Vector2 ChunkPos, Vector3 BlockPos, BlockData BData)
	{
		for (int i = 0; i < 6; i++)
		{
			BlockData NBData = GetNeighbour(ChunkPos, BlockPos, Dirs[i]); 
			bool Draw = false;
			if (BData.Comp.Contains(BComp.Transparent))
			{
				Draw = NBData.Comp.Contains(BComp.Unvisible) || (NBData.Comp.Contains(BComp.Transparent) && NBData.Comp.Contains(BComp.Unculled) && NBData.Name == BData.Name);
			}
			else
			{
				Draw = NBData.Comp.Contains(BComp.Unvisible) || NBData.Comp.Contains(BComp.Transparent);
			}

			if (Draw)
			{
				int[] AO = GetAo(ChunkPos, BlockPos, Dirs[i]);

				Vector3[] Verts1 = {CubeVerts[i * 4] + BlockPos, CubeVerts[i * 4 + 1] + BlockPos, CubeVerts[i * 4 + 2] + BlockPos};
				Vector3[] Verts2 = {CubeVerts[i * 4] + BlockPos, CubeVerts[i * 4 + 2] + BlockPos, CubeVerts[i * 4 + 3] + BlockPos};
				Vector2[] UVs1 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
								  new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
								  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize};
				Vector2[] UVs2 = {new Vector2(BData.Textures[i]%Global.AtlasSize + 1, BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize,
								  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize + 1)/Global.AtlasSize,
								  new Vector2(BData.Textures[i]%Global.AtlasSize    , BData.Textures[i]/Global.AtlasSize    )/Global.AtlasSize};
				Color Shade1 = CubeShade[i * 2 + Convert.ToInt32(BData.Comp.Contains(BComp.Dualcolor))];
				Color Shade2 = CubeShade[i * 2];
				Color[] Colors1 = {Shade1 - Shade1 * AO[0] * Global.AOIntencity,
								   Shade1 - Shade1 * AO[1] * Global.AOIntencity,
								   Shade1 - Shade1 * AO[2] * Global.AOIntencity};
				Color[] Colors2 = {Shade2 - Shade2 * AO[0] * Global.AOIntencity,
								   Shade2 - Shade2 * AO[2] * Global.AOIntencity, 
								   Shade2 - Shade2 * AO[3] * Global.AOIntencity};
				for (int c = 0; c < 3; c++) { Colors1[c].A = 1; Colors2[c].A = 1; }

				if (BData.Comp.Contains(BComp.Solid))
				{
					if (BData.Comp.Contains(BComp.Transparent)) { StTS.AddTriangleFan(Verts1, UVs1, Colors1); StTS.AddTriangleFan(Verts2, UVs2, Colors2); }
					else { StS.AddTriangleFan(Verts1, UVs1, Colors1); StS.AddTriangleFan(Verts2, UVs2, Colors2); }
				}
				else
				{
					if (BData.Comp.Contains(BComp.Transparent)) { StT.AddTriangleFan(Verts1, UVs1, Colors1); StT.AddTriangleFan(Verts2, UVs2, Colors2); }
					else { St.AddTriangleFan(Verts1, UVs1, Colors1); St.AddTriangleFan(Verts2, UVs2, Colors2); }
				}
			}
		}
	}

	private BlockData GetNeighbour(Vector2 ChunkPos, Vector3 BlockPos, Vector3 Dir)
	{
		Vector3 NBlockPos = BlockPos + Dir;
		BlockData NBData = Blocks.AllBlocks[0];
		if (NBlockPos.Y >= 0 && NBlockPos.Y < Global.ChunkSize.Y)
		{
			if (NBlockPos.X >= 0 && NBlockPos.X < Global.ChunkSize.X &&
			NBlockPos.Z >= 0 && NBlockPos.Z < Global.ChunkSize.Z)
			{
				NBData = Blocks.AllBlocks[_World.GetBlockLocal(ChunkPos, NBlockPos).Id];
			}
			else
			{
				NBData = Blocks.AllBlocks[_World.GetBlockGlobal(NBlockPos + new Vector3(ChunkPos.X, 0, ChunkPos.Y) * Global.ChunkSize).Id];
			}
		}
		return NBData;
	}

	private Vector3[] Dirs = {
		Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back 
	};

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

	private Vector3[] CrossVerts = {
		new Vector3(.15f, 1, .85f), new Vector3(.15f, 0, .85f), new Vector3(.85f, 0, .15f), new Vector3(.85f, 1, .15f),
		new Vector3(.85f, 1, .85f), new Vector3(.85f, 0, .85f), new Vector3(.15f, 0, .15f), new Vector3(.15f, 1, .15f)
	};

	private Color[] CrossShade = {
		new Color(.8f,.8f,.8f,1),
		new Color(.6f,.6f,.6f,1)
	};

	public int[] GetAo(Vector2 ChunkPos, Vector3 PlPos, Vector3 Dir)
	{
		Vector3 NBlockPos = PlPos + Dir;

		int a=0, b=0, c=0 ,d=0, e=0, f=0, g=0, h=0;

		if (Dir == Vector3I.Up)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0, -1)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0, -1)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  0)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  1)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0,  1)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  1)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  0)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0, -1)).Comp.Contains(BComp.Unvisible));
		} 
		else if (Dir == Vector3I.Down)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  0)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0, -1)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0, -1)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0, -1)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  0)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  1)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0,  1)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  1)).Comp.Contains(BComp.Unvisible));
		} 
		else if (Dir == Vector3I.Right)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0, -1)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1, -1)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  0)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  1)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0,  1)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  1)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  0)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1, -1)).Comp.Contains(BComp.Unvisible));
		} 
		else if (Dir == Vector3I.Left)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0,  1)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  1)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  0)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1, -1)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  0, -1)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1, -1)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  0)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  1)).Comp.Contains(BComp.Unvisible));
		} 
		else if (Dir == Vector3I.Forward)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  0)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  1,  0)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  0)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  1,  0)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  0)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1, -1,  0)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  0)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1, -1,  0)).Comp.Contains(BComp.Unvisible));
		} 
		else if (Dir == Vector3I.Back)
		{
			a = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  0,  0)).Comp.Contains(BComp.Unvisible));
			b = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1,  1,  0)).Comp.Contains(BComp.Unvisible));
			c = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0,  1,  0)).Comp.Contains(BComp.Unvisible));
			d = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  1,  0)).Comp.Contains(BComp.Unvisible));
			e = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1,  0,  0)).Comp.Contains(BComp.Unvisible));
			f = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I(-1, -1,  0)).Comp.Contains(BComp.Unvisible));
			g = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 0, -1,  0)).Comp.Contains(BComp.Unvisible));
			h = Convert.ToInt32(!GetNeighbour(ChunkPos, NBlockPos, new Vector3I( 1, -1,  0)).Comp.Contains(BComp.Unvisible));
		}

		int ul = a+b+c;
		int dl = c+d+e;
		int ur = a+h+g;
		int dr = g+f+e;
		int[] ao = {ul, ur, dr, dl};
		return ao;
	}
}
