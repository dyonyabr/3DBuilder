using Godot;
using System;
using System.Data.SqlTypes;

public partial class Tools : Node
{
	private Tools() {}
	private static Tools Ints = new Tools(); 

	public static int CP2IV(Vector3 Pos)
	{
		int x = (int)Pos.X; int y = (int)Pos.Y; int z = (int)Pos.Z;
		return (int)(x + y * Global.ChunkSize.X + z * Global.ChunkSize.Y * Global.ChunkSize.X);
	}

	public static int CP2I(int x, int y, int z)
	{
		return (int)(x + y * Global.ChunkSize.X + z * Global.ChunkSize.Y * Global.ChunkSize.X);
	}
}

public struct Block
{
	public ushort Id { get; set; } 
	public byte Light { get; set; } 

	public Block(ushort _Id = 65535, byte _Light = 255)
	{
		Id = _Id;
		Light = _Light;
	}
}

public enum BComp{
	Unvisible,
	Solid,
	Transparent,
	Dualcolor,
	Unculled,
	Liquid
}

public struct BlockData
{
	public string Name { get; set; } 
	public BComp[] Comp { get; set; } 
	public byte[] Textures { get; set; } 

	public BlockData(string _Name, BComp[] _Comp, byte[] _Textures)
	{
		Name = _Name;
		Comp = _Comp;
		Textures = _Textures;
	}
}

public struct BlockItem
{
	public ushort Id { get; set; }

	public BlockItem(ushort _Id)
	{
		Id = _Id;
	}
}