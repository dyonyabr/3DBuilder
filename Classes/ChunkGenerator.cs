using Godot;
using System;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Collections.Generic;

public partial class ChunkGenerator : Node
{
	private FastNoiseLite TerrainNoise;
	private World _World;

	public ChunkGenerator(World __World)
	{
		_World = __World;
		TerrainNoise = new FastNoiseLite();
		TerrainNoise.Seed = (int)_World.WorldSeed;
		TerrainNoise.Frequency = .0035f;
		TerrainNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		TerrainNoise.FractalLacunarity = 2.5f;
	} 

	void SetBlock(Vector2 ChunkPos, Vector3 BlockPos, Block B, bool Replace)
	{
		if (BlockPos.Y >= 0 && BlockPos.Y < Global.ChunkSize.Y)
		{
			if (BlockPos.X < 0 || BlockPos.X >= Global.ChunkSize.X || BlockPos.Z < 0 || BlockPos.Z >= Global.ChunkSize.Z)
			{
				Vector3 GlobalBlockPos = new Vector3(ChunkPos.X * Global.ChunkSize.X, 0, ChunkPos.Y * Global.ChunkSize.Z) + BlockPos;
				ChunkPos = new Vector2(Mathf.Floor(GlobalBlockPos.X/Global.ChunkSize.X), Mathf.Floor(GlobalBlockPos.Z/Global.ChunkSize.Z));
				BlockPos = new Vector3(Mathf.PosMod(GlobalBlockPos.X, Global.ChunkSize.X), GlobalBlockPos.Y, Mathf.PosMod(GlobalBlockPos.Z, Global.ChunkSize.Z)); 
				if (!_World.ChunkData.ContainsKey(ChunkPos)) { _World.ChunkData[ChunkPos] = _World.CreateChunkData(); }
				_World.SetBlockGlobal(GlobalBlockPos, B, Replace);
			}
			else
			{
				_World.SetBlockLocal(ChunkPos, BlockPos, B, Replace);
			}
		}
	}

	public void Generate(Vector2 ChunkPos)
	{
		_World.ChunkData[ChunkPos] = _World.CreateChunkData();
		 
		List<Vector3> TreePosses = new List<Vector3>();
		for (int x = 0; x < Global.ChunkSize.X; x++)
		{
			for (int z = 0; z < Global.ChunkSize.Z; z++)
			{
				Vector2 GlobalPlPos = new Vector2(ChunkPos.X * Global.ChunkSize.X + x, ChunkPos.Y * Global.ChunkSize.Z + z);
				float TerrV = TerrainNoise.GetNoise2Dv(GlobalPlPos);
				TerrV = Mathf.Pow(TerrV, 2) * Math.Sign(TerrV);
				if (TerrV >= 0) { TerrV *= 150; }
				else { TerrV *= 50; }
				TerrV = (int)Mathf.Floor(TerrV + 100);
				for (int y = 0; y < Global.ChunkSize.Y; y++)
				{
					ushort BId = 0;
					if (y < Global.WaterLevel) { BId = 23; }; 
					if (y <= TerrV)
					{
						BId = 1;
						if (y == TerrV) { BId = 3; if (x == 0 && z == 0) { TreePosses.Add(new Vector3(x, y, z)); } }
						else if (y == TerrV - 1) { BId = 2; }
					}
					Block B = new Block(BId, 0);

					SetBlock(ChunkPos, new Vector3(x,y,z), B, true);
				}
			}
		}
		for  (int i = 0; i < TreePosses.Count; i++)
        {
            Vector3 TreePos = TreePosses[i];
            int Height = _World.Rng.RandiRange(4, 8);
            SetBlock(ChunkPos, TreePos, new Block(2, 0), true);
            for (int h = 0; h < Height; h++)
            {
                SetBlock(ChunkPos, TreePos + new Vector3(0, h+1, 0), new Block(13, 0), false);  
            }
            SetBlock(ChunkPos, TreePos + new Vector3(0, Height + 1, 0), new Block(12, 0), false);  
			// SetBlock(ChunkPos, TreePos + new Vector3(1, 1, 0), new Block(13, 0), false);
			// SetBlock(ChunkPos, TreePos + new Vector3(-1, 1, 0), new Block(13, 0), false);
			// SetBlock(ChunkPos, TreePos + new Vector3(0, 1, -1), new Block(13, 0), false);
			// SetBlock(ChunkPos, TreePos + new Vector3(0, 1, 1), new Block(13, 0), false);
            {// int Spheres = Mathf.Clamp(Height - 3 + World.rng.RandiRange(-1, 1), 1, 8);
            // List<Vector2I> ds = new List<Vector2I>(){new Vector2I(1, 1), new Vector2I(1, -1), new Vector2I(-1, 1), new Vector2I(-1, -1)};
            // for (int s = 0; s < Spheres; s++)
            // {
            //     int Width = World.rng.RandiRange(4, 5);
            //     Vector3I Center;
            //     if (s != 0)
            //     {
            //         Width = World.rng.RandiRange(2, 4);
            //         Vector2I d = ds[World.rng.RandiRange(0, ds.Count-1)];
            //         ds.Remove(d);
            //         if (ds.Count == 0) { ds = new List<Vector2I>(){new Vector2I(1, 1), new Vector2I(1, -1), new Vector2I(-1, 1), new Vector2I(-1, -1)}; }
            //         Center = new Vector3I(d.X + TreePos.X,
            //         TreePos.Y + Height + World.rng.RandiRange(-Height+3, -1),
            //         d.Y + TreePos.Z);
            //     } else { Center = TreePos + Vector3I.Up * Height; }
            //     for (int x = -Width/2; x < Width/2; x++)
            //     {
            //         for (int y = -Width/2; y < Width/2; y++)
            //         {
            //             for (int z = -Width/2; z < Width/2; z++)
            //             {
            //                 Vector3I LPos = new Vector3I(x + Center.X, y + Center.Y, z + Center.Z);
            //                 if (((Vector3)LPos+Vector3.One*.5f).DistanceTo(Center) < Width/2) { SetBlock(Pos, Blocks, LPos.X, LPos.Y, LPos.Z, LeavesB); }
            //             }
            //         }
            //     }
            // }
			}
        }
	}
}
