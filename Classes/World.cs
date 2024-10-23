using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

public partial class World : Node3D
{
	public WorldEnvironment WorldEnv;
	public int DrawDist = 16;

	public Node3D ChunksNode;
	public RandomNumberGenerator Rng;
	public ulong WorldSeed;

	public Player _Player;
	public Vector2 PlayerPos =  new Vector2(0, 0);

	public StandardMaterial3D ChunkMaterial;
	public StandardMaterial3D ChunkMaterialTransparent;

	public ChunkGenerator _ChunkGenerator; 
	public ChunkRemesher _ChunkRemesher; 

	public Dictionary<Vector2, Block[]> ChunkData;

	public System.Threading.Mutex Mut = new System.Threading.Mutex();
	private List<Vector2> ToGenerate = new List<Vector2>();
	private List<Vector2> ToRemesh = new List<Vector2>();
	private List<Thread> GeneratePool = new List<Thread>();
	private List<Thread> RemeshPool = new List<Thread>();
	private int MaxGenerateThreads = 13;
	private int MaxRemeshThreads = 13;

	private async void SetPlayerPos(Vector2 Value)
	{
		if (PlayerPos != Value)
		{
			PlayerPos = Value;

			GenerateChunks(true, Value);
			while (ToGenerate.Count() > 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			RemeshChunks(true, Value);
		}
	}

	public override void _Ready()
	{
		Name = "World";
		WorldSeed = 1;
		Rng = new RandomNumberGenerator();
		Rng.Seed = WorldSeed;

		AtlasTexture Atlas = new AtlasTexture();
		Atlas.Atlas = GD.Load<Texture2D>("res://Assets/Textures/ChunkAtlas.png");

		ChunkMaterial = new StandardMaterial3D();
		ChunkMaterial.AlbedoTexture = Atlas;
		ChunkMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
		ChunkMaterial.VertexColorUseAsAlbedo = true;

		ChunkMaterialTransparent = new StandardMaterial3D();
		ChunkMaterialTransparent.AlbedoTexture = Atlas;
		ChunkMaterialTransparent.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
		ChunkMaterialTransparent.VertexColorUseAsAlbedo = true;
		ChunkMaterialTransparent.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

		_ChunkGenerator = new ChunkGenerator(this);
		_ChunkRemesher = new ChunkRemesher(this);
		
		WorldEnv = new WorldEnvironment();
		AddChild(WorldEnv);
		WorldEnv.Name = "WorldEnv";
		WorldEnv.Environment = new Godot.Environment();
		WorldEnv.Environment.BackgroundMode = Godot.Environment.BGMode.Color;
		WorldEnv.Environment.BackgroundColor = new Color(.7f, .85f, 1, 1);
		WorldEnv.Environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		WorldEnv.Environment.AmbientLightColor = new Color(1,1,1,1);
		WorldEnv.Environment.FogEnabled = true;
		WorldEnv.Environment.FogLightColor = WorldEnv.Environment.BackgroundColor;
		WorldEnv.Environment.FogMode = Godot.Environment.FogModeEnum.Depth;
		WorldEnv.Environment.FogDepthBegin = (DrawDist/2 + DrawDist/4) * Global.ChunkSize.X;
		WorldEnv.Environment.FogDepthEnd = (DrawDist-1) * Global.ChunkSize.X;

		ChunksNode = new Node3D();
		AddChild(ChunksNode);
		ChunksNode.Name = "ChunksNode";

		ChunkData = new Dictionary<Vector2, Block[]>();

		GenerateChunks(false, Vector2.Zero);
		RemeshChunks(true, Vector2.Zero);

		_Player = new Player(this);
		AddChild(_Player);
		_Player.Position = new Vector3(0, Global.ChunkSize.Y, 0);
	}

	public override void _Process(double delta)
	{
		SetPlayerPos(new Vector2(Mathf.Floor(_Player.Position.X / Global.ChunkSize.X), Mathf.Floor(_Player.Position.Z / Global.ChunkSize.Z)));
		
		ThreadGenerationProcess();
		ThreadRemeshProcess();
	}

	private async void ThreadGenerationProcess()
	{
		if (ToGenerate.Count() > 0 && GeneratePool.Count() < MaxGenerateThreads - 1)
		{
			Vector2 ChunkPos = ToGenerate[0];
			ToGenerate.RemoveAt(0);
			Thread T = new Thread(() => _ChunkGenerator.Generate(ChunkPos));
			GeneratePool.Add(T);
			T.Start();
			while (T.IsAlive)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			T.Join();
			GeneratePool.Remove(T);
		}
	} 

	private async void ThreadRemeshProcess()
	{
		if (ToRemesh.Count() > 0 && RemeshPool.Count() < MaxRemeshThreads - 1)
		{
			Vector2 ChunkPos = ToRemesh[0];
			ToRemesh.RemoveAt(0);
			Thread T = new Thread(() => _ChunkRemesher.Remesh(ChunkPos));
			RemeshPool.Add(T);
			T.Start();
			while (T.IsAlive)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			T.Join();
			RemeshPool.Remove(T);
		}
	}

	private async void ImmidieteRemesh(Vector2 ChunkPos)
	{
		Thread T = new Thread(() => _ChunkRemesher.Remesh(ChunkPos));
		RemeshPool.Add(T);
		T.Start();
		while (T.IsAlive)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		T.Join();
		RemeshPool.Remove(T);
	}

	public Block[] CreateChunkData()
	{
		Block[] CData = new Block[Global.CDataSize]; 
		Array.Fill(CData, new Block(65535, 255));
		return CData;
	}

	public void SetBlockLocal(Vector2 ChunkPos, Vector3 BlockPos, Block B, bool Replace)
	{
		ushort Id = ChunkData[ChunkPos][Tools.CP2IV(BlockPos)].Id;
		if (Id == 65535 || Id == 0 || Replace)
		{
			ChunkData[ChunkPos][Tools.CP2IV(BlockPos)] = B;
		}
	}

	public void SetBlockGlobal(Vector3 GlobalBlockPos, Block B, bool Replace)
	{
		Vector2 ChunkPos = new Vector2(Mathf.Floor(GlobalBlockPos.X/Global.ChunkSize.X), Mathf.Floor(GlobalBlockPos.Z/Global.ChunkSize.Z));
		Vector3 BlockPos = new Vector3(Mathf.PosMod(GlobalBlockPos.X, Global.ChunkSize.X), GlobalBlockPos.Y, Mathf.PosMod(GlobalBlockPos.Z, Global.ChunkSize.Z)); 
		if (!ChunkData.ContainsKey(ChunkPos)) { ChunkData[ChunkPos] = CreateChunkData(); }
		SetBlockLocal(ChunkPos, BlockPos, B, Replace);
	}

	public Block GetBlockLocal(Vector2 ChunkPos, Vector3 BlockPos)
	{
		return ChunkData[ChunkPos][Tools.CP2IV(BlockPos)];
	}

	public Block GetBlockGlobal(Vector3 GlobalBlockPos)
	{
		Vector2 ChunkPos = new Vector2(Mathf.Floor(GlobalBlockPos.X/Global.ChunkSize.X), Mathf.Floor(GlobalBlockPos.Z/Global.ChunkSize.Z));
		Vector3 BlockPos = new Vector3(Mathf.PosMod(GlobalBlockPos.X, Global.ChunkSize.X), GlobalBlockPos.Y, Mathf.PosMod(GlobalBlockPos.Z, Global.ChunkSize.Z)); 
		return ChunkData[ChunkPos][Tools.CP2IV(BlockPos)];
	}

	public void ApplyRemesh(Vector2 ChunkPos, ArrayMesh ArrMesh, ArrayMesh ArrMeshSolid)
	{
		Chunk _Chunk = ChunksNode.GetNodeOrNull<Chunk>("x"+ChunkPos.X+"y"+ChunkPos.Y);
		if (_Chunk != null)
		{
			_Chunk.MeshInst.Mesh = ArrMesh;
			_Chunk.SolidShape.Shape = ArrMeshSolid.CreateTrimeshShape();
			for (int i = 0; i < _Chunk.MeshInst.GetSurfaceOverrideMaterialCount(); i++)
			{	
				if (i == 0) { _Chunk.MeshInst.SetSurfaceOverrideMaterial(i, ChunkMaterial); }
				else if (i == 1) { _Chunk.MeshInst.SetSurfaceOverrideMaterial(i, ChunkMaterialTransparent); }
			}
			_Chunk.SetVisibility(true, false);
		}
	}

	public void GenerateChunks(bool Threaded, Vector2 PlPos)
	{
		List<Vector2> ChunkPosses = new List<Vector2>();
		for (int x = -DrawDist - 2; x < DrawDist + 2; x++)
		{
			for (int y = -DrawDist - 2; y < DrawDist + 2; y++)
			{
				Vector2 ChunkPos = new Vector2(x, y) + PlPos;
				bool Contains = ChunkData.ContainsKey(ChunkPos);
				// bool HasDefault = !Contains;
				bool HasDefault = true;
				if (Contains)
				{
					for (int i = 0; i < ChunkData[ChunkPos].Count(); i++)
					{
						if (ChunkData[ChunkPos][i].Id == 65535) { HasDefault = true; break; }
					}
				}
				if (ChunkPos.DistanceTo(PlPos) <= DrawDist+2 && HasDefault)
				{ 
					ChunkPosses.Add(ChunkPos);
				}
			}
		}
		ChunkPosses = ChunkPosses.OrderBy(Pos => Pos.DistanceSquaredTo(PlPos)).ToList();
		for (int i = 0; i < ChunkPosses.Count(); i++)
		{
			if (Threaded) { if (!ToGenerate.Contains(ChunkPosses[i])) { ToGenerate.Add(ChunkPosses[i]); } }
			else { _ChunkGenerator.Generate(ChunkPosses[i]); }
		}
	}

	public void RemeshChunks(bool Threaded, Vector2 PlPos)
	{

		List<Vector2> NeededChunkPosses = new List<Vector2>();
		for (int x = -DrawDist; x < DrawDist; x++)
		{
			for (int y = -DrawDist; y < DrawDist; y++)
			{
				Vector2 ChunkPos = new Vector2(x, y);
				if (ChunkPos.Length() < DrawDist) { ChunkPos += PlPos; NeededChunkPosses.Add(ChunkPos); }
			}
		}
		List<Chunk> MisplacedChunkPosses = new List<Chunk>();
		for (int i = 0; i < ChunksNode.GetChildCount(); i++)
		{
			Chunk _Chunk = ChunksNode.GetChild<Chunk>(i);
			Vector2 ChunkPos = new Vector2(_Chunk.ChunkPos.X, _Chunk.ChunkPos.Y);
			if (!NeededChunkPosses.Contains(ChunkPos)) { MisplacedChunkPosses.Add(_Chunk); }
			else { NeededChunkPosses.Remove(ChunkPos); }
		}
		NeededChunkPosses = NeededChunkPosses.OrderBy(Pos => Pos.DistanceSquaredTo(PlPos)).ToList();
		for (int i = 0; i < NeededChunkPosses.Count(); i++)
		{
			Chunk _Chunk;
			if (MisplacedChunkPosses.Count() > 0)
			{
				_Chunk = MisplacedChunkPosses[0];
				MisplacedChunkPosses.RemoveAt(0);
			}
			else { _Chunk = new Chunk(this); ChunksNode.AddChild(_Chunk); }

			_Chunk.SetVisibility(false, false);
			_Chunk.SetChunkPos(NeededChunkPosses[i]);

			if (ChunkData.ContainsKey(NeededChunkPosses[i]) && //!ChunkData[NeededChunkPosses[i]].Contains(default) &&
			ChunkData.ContainsKey(NeededChunkPosses[i] + Vector2.Left) && //!ChunkData[NeededChunkPosses[i] + Vector2.Left].Contains(default) &&
			ChunkData.ContainsKey(NeededChunkPosses[i] + Vector2.Right) && //!ChunkData[NeededChunkPosses[i] + Vector2.Right].Contains(default) && 
			ChunkData.ContainsKey(NeededChunkPosses[i] + Vector2.Up) && //!ChunkData[NeededChunkPosses[i] + Vector2.Up].Contains(default) &&
			ChunkData.ContainsKey(NeededChunkPosses[i] + Vector2.Down)) //&& !ChunkData[NeededChunkPosses[i] + Vector2.Down].Contains(default))
			{
				Remesh(NeededChunkPosses[i], Threaded);
			}
		}
	}

	public void Remesh(Vector2 ChunkPos, bool Threaded, bool Immidiete = false)
	{
		if (Threaded) { if (!ToRemesh.Contains(ChunkPos)) 
		{
			if (Immidiete) { ImmidieteRemesh(ChunkPos); }
			else { ToRemesh.Add(ChunkPos); }
		}}
		else { _ChunkRemesher.Remesh(ChunkPos); }
	}

	// public override void _ExitTree()
    // {
	// 	Task.WaitAll(GeneratePool.ToArray());
	// 	Task.WaitAll(RemeshPool.ToArray());
    // }
}
