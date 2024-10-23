using Godot;
using System;

public partial class Blocks : Node
{
	private Blocks() {}
	private static Blocks Inst = new Blocks();

	public static BlockData[] AllBlocks = new BlockData[] {
		new BlockData("air", new BComp[]{BComp.Unvisible}, new byte[]{}), //0
		new BlockData("stone", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{3, 3, 3, 3, 3, 3}), //1
		new BlockData("dirt", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{0, 0, 0, 0, 0, 0}), //2
		new BlockData("grass", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{2, 0, 1, 1, 1, 1}), //3
		new BlockData("wood", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{5, 5, 4, 4, 4, 4}), //4
		new BlockData("bricks", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{6, 6, 6, 6, 6, 6}), //5
		new BlockData("leaves", new BComp[]{BComp.Solid, BComp.Transparent, BComp.Dualcolor, BComp.Unculled}, new byte[]{7, 7, 7, 7, 7, 7}), //6
		new BlockData("orange", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{16 ,16, 16, 16, 16, 16}), //7
		new BlockData("yellow", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{17, 17, 17, 17, 17, 17}), //8
		new BlockData("lightgreen", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{18, 18, 18, 18, 18, 18}), //9
		new BlockData("blue", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{19, 19, 19, 19, 19, 19}), //10
		new BlockData("lilac", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{20, 20, 20, 20, 20, 20}), //11
		new BlockData("pink", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{21, 21, 21, 21, 21, 21}), //12
		new BlockData("skin", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{22, 22, 22, 22, 22, 22}), //13
		new BlockData("black", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{23, 23, 23, 23, 23, 23}), //14
		new BlockData("white", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{24, 24, 24, 24, 24, 24}), //15
		new BlockData("red", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{25, 25, 25, 25, 25, 25}), //16
		new BlockData("darkblue", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{26, 26, 26, 26, 26, 26}), //17
		new BlockData("purple", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{27, 27, 27, 27, 27, 27}), //18
		new BlockData("green", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{28, 28, 28, 28, 28, 28}), //19
		new BlockData("brown", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{29, 29, 29, 29, 29, 29}), //20
		new BlockData("gray", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{30, 30, 30, 30, 30, 30}), //21
		new BlockData("lightgray", new BComp[]{BComp.Solid, BComp.Dualcolor}, new byte[]{31, 31, 31, 31, 31, 31}), //22
		new BlockData("water", new BComp[]{BComp.Transparent, BComp.Liquid}, new byte[]{8, 8, 8, 8, 8, 8}), //23
	};
}
