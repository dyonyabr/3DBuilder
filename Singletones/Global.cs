using Godot;
using System;
using System.Runtime.Serialization;

public partial class Global : Node
{
	private Global() {}
	private static Global Inst = new Global();

	public static Vector3 ChunkSize = new Vector3(16, 256, 16);
	public static int CDataSize = (int)(ChunkSize.X * ChunkSize.Y * ChunkSize.Z);
	public static int WaterLevel = 100;
	public static int AtlasSize = 16;
	public static float AOIntencity = 0.275F;

	public static float UiMaxScale = 2;
	public static float UiMinScale = 1;
	public static float UiScale = 1;
	public static void SetUiScale(float Value)
	{
		UiScale = Mathf.Clamp(UiScale + Value, UiMinScale, UiMaxScale);
	}
}
