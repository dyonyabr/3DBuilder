using Godot;
using System;

public partial class Main : Node
{
	public Node3D CurrScene;	
	public World _World;

	public override void _Ready()
	{
		CurrScene = new Node3D();
		AddChild(CurrScene);
		CurrScene.Name = "CurrScene";
		_World = new World();
		CurrScene.AddChild(_World);
	}

    public override void _Process(double delta)
    {
		if (Input.IsActionJustPressed("UiPlus")) { Global.SetUiScale(.25f); }
		else if (Input.IsActionJustPressed("UiMinus")) { Global.SetUiScale(-.25f); }
    }
}
