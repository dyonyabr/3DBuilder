using Godot;
using Godot.Collections;
using System;
using System.Runtime.InteropServices;

public partial class Player : CharacterBody3D
{
	public bool IsFly = false;

	Node3D ThirdPerson;
	Node3D CameraPivot;
	Camera3D Camera;
	CollisionShape3D Collider;
	StandardMaterial3D SphereMaterial;
	StandardMaterial3D SphereMaterialShade;
	StandardMaterial3D EyesMaterial;
	RayCast3D FloorRay;
	RayCast3D CameraRay;
	RayCast3D CameraUpRay;
	MeshInstance3D Shadow;
	StandardMaterial3D ShadowMaterial;
	Timer StepTimer;

	PackedScene StepDustScene = GD.Load<PackedScene>("res://Assets/Particles/StepDust.tscn");
	PackedScene LandDustScene = GD.Load<PackedScene>("res://Assets/Particles/LandDust.tscn");

	BlockSelector _BlockSelector;

	float MaxCamDist = 11;
	float CamDist = 11;
	float CamDistVel = 0;

	float TurnAngle; 

	Node3D Model;
	Node3D Body;
	Node3D Head;
	Node3D Legs;
	Node3D Arms;
	Node3D LLeg;
	Node3D RLeg;
	Node3D LArm;
	Node3D RArm;
	Node3D LPelvis;
	Node3D RPelvis;
	Node3D LShoulder;
	Node3D RShoulder;
	Limb LLegLimb;
	Limb RLegLimb;
	Limb LArmLimb;
	Limb RArmLimb;
	public MeshInstance3D ItemMesh;

	bool Aimed = false;
	Vector3 AimedPos = new Vector3();

	float LLegImpact = 0;
	float RLegImpact = 0;
	float LArmImpact = 0;
	float CrosshairImpact = 1.0f;
	public float RArmImpact = 0;
	Vector3 LArmOffset = new Vector3();
	Vector3 RArmOffset = new Vector3();
	Vector3 ThirdPersonPos;
	Vector3 CameraUpRayPos;
	float ArmsAngle = Mathf.Pi/8;
	Vector3 BodyRotation = new Vector3();
	Vector3 BodyLerpAngle = new Vector3();
	int CurLeg = 0;
	float StepCull = .1f;
	float FootRadius = .125f;
	private void ChangeCurLeg()
	{
		CurLeg = (CurLeg + 1)%2;
	}
	private async void MakeStep(int Count = 1, bool Impact = true)
	{
		StepTimer.Stop();
		for (int i = 0; i < Count; i++)
		{
			Legs.GetChild<Node3D>(CurLeg).Position = Position + new Vector3((CurLeg*2-1) * .2f, FootRadius, 0).Rotated(Vector3.Up, LastAngle);
			if (Impact)
			{
				if (CurLeg == 0) { LLegImpact = .5f; }
				if (CurLeg == 1) { RLegImpact = .5f; }
				// DoStepDust(Legs.GetChild<Node3D>(CurLeg).Position + Vector3.Down * FootRadius);
			}
			ChangeCurLeg();
			ArmsAngle = (Convert.ToInt32(CurLeg)*2-1) * Mathf.Pi/6; 

		}
		TurnAngle = ThirdPerson.Rotation.Y;
		StepTimer.Start(StepCull);
		await ToSignal(GetTree().CreateTimer(StepCull), SceneTreeTimer.SignalName.Timeout);
	}

	const float Height = 1.9f;

	public World _World;

	float Speed = 6f;
	float FlySpeed = 10f;
	float Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	float JumpStr = 10f;
	float MouseSpeed = .005f;
	
	float LastAngle;
	Vector2 InputDirection;
	Vector2 Direction;
	Vector2 MouseDir = new Vector2();
	Vector3 MoveVel = new Vector3();
	Vector3 GravityVel = new Vector3();
	Vector2 MouseVel = new Vector2();

	Sprite2D Crosshair;

	MeshInstance3D Outline;

	public Player(World __World)
	{
		_World = __World;
	}

	public override void _Ready()
	{
		Name  = "Player";
		SetCollisionMaskValue(2, true);
		SetCollisionMaskValue(1, false);

		{ThirdPerson = new Node3D();
		AddChild(ThirdPerson);
		ThirdPerson.Name = "ThirdPerson";
		ThirdPerson.TopLevel = true;}

		{CameraPivot = new Node3D();
		ThirdPerson.AddChild(CameraPivot);
		CameraPivot.Name = "CameraPivot";}
		
		{Camera = new Camera3D();
		CameraPivot.AddChild(Camera);
		Camera.Name = "Camera";
		Camera.Current = true;
		Camera.Fov = 45.0f;
		Camera.SetCullMaskValue(2, false);
		Camera.Position = new Vector3(0, 0, 10);}

		{Collider = new CollisionShape3D();
		AddChild(Collider);
		Collider.Name = "Collider";
		CylinderShape3D ColliderShape = new CylinderShape3D();
		Collider.Shape = ColliderShape;
		ColliderShape.Height = Height;
		ColliderShape.Radius = .25f;
		Collider.Position = new Vector3(0, ColliderShape.Height/2, 0);}

		{FloorRay = new RayCast3D();
		AddChild(FloorRay);
		FloorRay.Name = "FloorRay";
		FloorRay.SetCollisionMaskValue(1, false);
		FloorRay.SetCollisionMaskValue(2, true);
		FloorRay.TargetPosition = Vector3.Down * 5;
		FloorRay.TopLevel = true;}

		{CameraRay = new RayCast3D();
		AddChild(CameraRay);
		CameraRay.Name = "CameraRay";
		CameraRay.TopLevel = true;
		CameraRay.SetCollisionMaskValue(1, false);
		CameraRay.SetCollisionMaskValue(2, true);
		CameraRay.TargetPosition = new Vector3(0,0,1);}

		{Shadow = new MeshInstance3D();
		AddChild(Shadow);
		Shadow.Name = "Shadow";
		PlaneMesh ShadowMesh = new PlaneMesh();
		Shadow.Mesh = ShadowMesh;
		ShadowMesh.Size = new Vector2(.5f, .5f);
		ShadowMaterial = new StandardMaterial3D();
		Shadow.MaterialOverride = ShadowMaterial;
		ShadowMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		ShadowMaterial.AlbedoTexture = GD.Load<Texture2D>("res://Assets/Textures/Shadow.png");
		Shadow.TopLevel = true;}

		{StepTimer = new Timer();
		AddChild(StepTimer);
		StepTimer.Name = "StepTimer";
		StepTimer.WaitTime = StepCull;
		StepTimer.OneShot = true;}

		{SphereMaterial = new StandardMaterial3D();
		SphereMaterial.AlbedoColor = new Color("ffccaa");
		SphereMaterial.DistanceFadeMode = BaseMaterial3D.DistanceFadeModeEnum.PixelDither;
		SphereMaterial.DistanceFadeMaxDistance = 4;
		SphereMaterial.DistanceFadeMinDistance = 1;}

		{SphereMaterialShade = new StandardMaterial3D();
		SphereMaterialShade.AlbedoColor = new Color("1d2b53");
		SphereMaterialShade.DistanceFadeMode = BaseMaterial3D.DistanceFadeModeEnum.PixelDither;
		SphereMaterialShade.DistanceFadeMaxDistance = 4;
		SphereMaterialShade.DistanceFadeMinDistance = 1;}

		{EyesMaterial = new StandardMaterial3D();
		EyesMaterial.AlbedoColor = new Color(1, 1, 1, 1);
		EyesMaterial.DistanceFadeMode = BaseMaterial3D.DistanceFadeModeEnum.PixelDither;
		EyesMaterial.DistanceFadeMaxDistance = 4;
		EyesMaterial.DistanceFadeMinDistance = 1;}

		{Model = new Node3D();
		AddChild(Model);
		Model.Name = "Model";}

		{Body = new Node3D();
		Model.AddChild(Body);
		Body.Name = "Body";
		CreateSphere(Body, .3f, SphereMaterialShade);
		Body.TopLevel = true;}

		{Head = new Node3D();
		Body.AddChild(Head);
		Head.Name = "Head";
		CreateSphere(Head, .25f, SphereMaterial);
		Head.Position = new Vector3(0, .55f, 0);
		CreateSphere(Head.GetChild<MeshInstance3D>(0), .1f, EyesMaterial);
		CreateSphere(Head.GetChild<MeshInstance3D>(0), .1f, EyesMaterial);
		Head.GetChild<MeshInstance3D>(0).GetChild<MeshInstance3D>(0).TopLevel = false;
		Head.GetChild<MeshInstance3D>(0).GetChild<MeshInstance3D>(1).TopLevel = false;
		Head.GetChild<MeshInstance3D>(0).GetChild<MeshInstance3D>(0).Position = new Vector3(-.11f, 0, -.175f);
		Head.GetChild<MeshInstance3D>(0).GetChild<MeshInstance3D>(1).Position = new Vector3(.11f, 0, -.175f);}

		{Legs = new Node3D();
		Model.AddChild(Legs);
		Legs.Name = "Legs";
		LLeg = new Node3D();
		Legs.AddChild(LLeg);
		LLeg.Name = "LLeg";
		LLeg.TopLevel = true;
		CreateSphere(LLeg, FootRadius, SphereMaterialShade);
		RLeg = new Node3D();
		Legs.AddChild(RLeg);
		RLeg.Name = "RLeg";
		RLeg.TopLevel = true;
		CreateSphere(RLeg, FootRadius, SphereMaterialShade);}

		{Arms = new Node3D();
		Body.GetChild(0).AddChild(Arms);
		Arms.Name = "Arms";}

		{LArm = new Node3D();
		Arms.AddChild(LArm);
		LArm.Name = "LArm";
		LArm.Position = new Vector3(-.325f, -.075f, 0);
		CreateSphere(LArm, FootRadius, SphereMaterial);
		LArm.GetChild<MeshInstance3D>(0).TopLevel = false;
		RArm = new Node3D();
		Arms.AddChild(RArm);
		RArm.Name = "RArm";
		RArm.Position = new Vector3(.325f, 0f, .4f);
		CreateSphere(RArm, FootRadius, SphereMaterial);
		RArm.GetChild<MeshInstance3D>(0).TopLevel = false;}

		{ItemMesh = new MeshInstance3D();
		RArm.GetChild<MeshInstance3D>(0).AddChild(ItemMesh);
		ItemMesh.Name = "ItemMesh";
		ItemMesh.MaterialOverride = _World.ChunkMaterialTransparent;
		ItemMesh.Position = new Vector3(-.125f, 0, -.2f);
		ItemMesh.Rotation = new Vector3(0, Mathf.Pi/4, 0);
		ItemMesh.Scale = Vector3.One * .25f;}

		{LPelvis = new Node3D();
		Body.GetChild(0).AddChild(LPelvis);
		LPelvis.Name = "LPelvis";
		LPelvis.Position = new Vector3(-.124f, 0, 0);
		RPelvis = new Node3D();
		Body.GetChild(0).AddChild(RPelvis);
		RPelvis.Name = "RPelvis";
		RPelvis.Position = new Vector3(.125f, 0, 0);}

		{LShoulder = new Node3D();
		Arms.AddChild(LShoulder);
		LShoulder.Name = "LShoulder";
		LShoulder.Position = new Vector3(-.2f, .15f, 0);
		CreateSphere(LShoulder, FootRadius, SphereMaterial);
		LShoulder.GetChild<MeshInstance3D>(0).TopLevel = false;
		RShoulder = new Node3D();
		Arms.AddChild(RShoulder);
		RShoulder.Name = "RShoulder";
		RShoulder.Position = new Vector3(.2f, .15f, 0);
		CreateSphere(RShoulder, FootRadius, SphereMaterial);
		RShoulder.GetChild<MeshInstance3D>(0).TopLevel = false;}

		{LLegLimb = new Limb(LLeg.GetChild<Node3D>(0), FootRadius, SphereMaterialShade, "LLeg");
		LPelvis.AddChild(LLegLimb);
		RLegLimb = new Limb(RLeg.GetChild<Node3D>(0), FootRadius, SphereMaterialShade, "RLeg");
		RPelvis.AddChild(RLegLimb);}

		{LArmLimb = new Limb(LArm.GetChild<Node3D>(0), FootRadius, SphereMaterial, "LArm");
		LShoulder.AddChild(LArmLimb);
		RArmLimb = new Limb(RArm.GetChild<Node3D>(0), FootRadius, SphereMaterial, "RArm");
		RShoulder.AddChild(RArmLimb);}

		{CameraUpRay = new RayCast3D();
		AddChild(CameraUpRay);
		CameraUpRay.Name = "CameraUpRay";
		CameraUpRay.TopLevel = true;
		CameraUpRay.SetCollisionMaskValue(1, false);
		CameraUpRay.SetCollisionMaskValue(2, true);
		CameraUpRay.TargetPosition = new Vector3(0,.5f,0);}

		{_BlockSelector = new BlockSelector(this);
		AddChild(_BlockSelector);}

		{Crosshair = new Sprite2D();
		AddChild(Crosshair);
		Crosshair.Name = "Crosshair";
		Crosshair.Texture = GD.Load<Texture2D>("res://Assets/Textures/Crosshair.png");
		Crosshair.Scale = Vector2.One * .25f;}

		{Sprite2D CrosshairShadow = new Sprite2D();
		Crosshair.AddChild(CrosshairShadow);
		CrosshairShadow.Name = "CrosshairShadow";
		CrosshairShadow.Texture = GD.Load<Texture2D>("res://Assets/Textures/CrosshairShadow.tres");
		CrosshairShadow.ShowBehindParent = true;}

		{Outline = new MeshInstance3D();
		AddChild(Outline);
		Outline.Name = "Outline";
		Outline.TopLevel = true;
		ShaderMaterial OutlineMaterial = new ShaderMaterial();
		OutlineMaterial.Shader = GD.Load<Shader>("res://Assets/Shaders/Outline.gdshader");
		Outline.MaterialOverride = OutlineMaterial;}

		{BoxMesh OutlineMesh = new BoxMesh();
		Outline.Mesh = OutlineMesh;
		OutlineMesh.Size = new Vector3(1.001f, 1.001f, 1.001f);}
	}

	private async void DoStepDust(Vector3 Pos)
	{
		await ToSignal(GetTree().CreateTimer(.1f, false), SceneTreeTimer.SignalName.Timeout);
		GpuParticles3D StepDust = StepDustScene.Instantiate<GpuParticles3D>();
		AddChild(StepDust);
		StepDust.TopLevel = true;
		StepDust.Position = Pos;
		StepDust.OneShot = true;
		StepDust.Emitting = true;
		await ToSignal(StepDust, "finished");
		StepDust.QueueFree();
	}

	private async void DoLandDust(Vector3 Pos)
	{
		GpuParticles3D LandDust = LandDustScene.Instantiate<GpuParticles3D>();
		AddChild(LandDust);
		LandDust.TopLevel = true;
		LandDust.Position = Pos;
		LandDust.OneShot = true;
		LandDust.Emitting = true;
		await ToSignal(LandDust, "finished");
		LandDust.QueueFree();
	}

	private void CreateSphere(Node Parent, float Radius, StandardMaterial3D _Material, int Qality = 24)
	{
		MeshInstance3D Sphere = new MeshInstance3D();
		Parent.AddChild(Sphere);
		Sphere.Name = "Sphere";
		Sphere.MaterialOverride = _Material;
		Sphere.TopLevel = true;
		SphereMesh Mesh = new SphereMesh();
		Sphere.Mesh = Mesh;
		Mesh.Height = Radius * 2;
		Mesh.Radius = Radius;
		Mesh.RadialSegments = Qality * 2;
		Mesh.Rings = Qality;
	}

    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventMouseButton && Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event.IsAction("Esc"))
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}

			if (@event is InputEventMouseMotion)
			{
				InputEventMouseMotion M = (InputEventMouseMotion)@event;
				MouseDir = -M.Relative;
			}
		}
    }

    public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("FlySwitch")) { IsFly = !IsFly; }

		InputDirection = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBack");
		Direction = InputDirection.Rotated(-ThirdPerson.Rotation.Y).LimitLength(1);

		float Sp = Speed; if (IsFly) { Sp = FlySpeed; }
		MoveVel = MoveVel.Lerp(new Vector3(Direction.X, 0, Direction.Y)  * Sp, (float)delta * (20 - Convert.ToInt32(!IsOnFloor()) * 17));
		if (IsOnFloor() || IsFly)
		{
			LastAngle = ThirdPerson.Rotation.Y;
			if (!IsFly)
			{
				if (GravityVel.Y < -5)
				{
					BodyRotation.Y = ThirdPerson.Rotation.Y;
					MakeStep(2, false);
					// DoLandDust(Position + Vector3.Up * .25f); ArmsAngle = 0;
				}
				GravityVel.Y = 0;
				if (Input.IsActionPressed("Jump"))
				{
					GravityVel.Y = JumpStr;
				}
			}
			else 
			{
				ArmsAngle = 0;
				GravityVel = GravityVel.Lerp(new Vector3(0, Input.GetAxis("MoveDown", "MoveUp") * FlySpeed, 0), (float)delta * 20);
			}
		}
		else
		{
			ArmsAngle = 0;
			if (IsOnCeiling()) { GravityVel.Y = 0; }
			GravityVel.Y = (float)Mathf.Clamp(GravityVel.Y - Gravity * delta, -70, 100);
		}


		Velocity = MoveVel + GravityVel;
		MoveAndSlide();
		
		MouseDir.X += -Input.GetJoyAxis(0, JoyAxis.RightX) * 8;
		MouseDir.Y += -Input.GetJoyAxis(0, JoyAxis.RightY) * 8;
		MouseVel = MouseVel.Lerp(MouseDir * MouseSpeed, (float)delta * 20);
		MouseDir = Vector2.Zero;	
		ThirdPerson.RotateY(MouseVel.X);
		Vector3 CameraPivotRot = CameraPivot.Rotation;
		CameraPivotRot.X = Mathf.Clamp(CameraPivotRot.X + MouseVel.Y, -Mathf.Pi/2, Mathf.Pi/2);
		CameraPivot.Rotation = CameraPivotRot;


		{CameraRay.Position = ThirdPerson.Position;
		CameraRay.Rotation = new Vector3(CameraPivot.Rotation.X, ThirdPerson.Rotation.Y, 0);
		CameraRay.TargetPosition = new Vector3(0,0,1000);
		CameraRay.ForceRaycastUpdate();
		if (Input.IsActionPressed("CamSwitch"))
		{
			if (Input.IsActionJustPressed("ZoomIn")) { CamDist-=2; }
			if (Input.IsActionJustPressed("ZoomOut")) { CamDist+=2; }
		}
		CamDist = Mathf.Clamp(CamDist, 0, MaxCamDist);
		CamDistVel = Mathf.Lerp(CamDistVel, CamDist, (float)delta * 10);
		Camera.Position = new Vector3(0, 0, Mathf.Pow(Mathf.Abs(CameraPivot.Rotation.X - Mathf.Pi/2) / Mathf.Pi, .5f) * (CamDistVel+1) - 1);

		if (CameraRay.IsColliding() && CameraRay.GetCollisionPoint().DistanceTo(CameraRay.Position) - .25f < Camera.Position.Z)
		{
			Camera.Position = new Vector3(0,0,CameraRay.GetCollisionPoint().DistanceTo(CameraRay.Position) - .25f);
		}}

		{CameraUpRayPos.X = Mathf.Lerp(CameraUpRayPos.X, Body.Position.X, (float)delta * 5);
		CameraUpRayPos.Z = Mathf.Lerp(CameraUpRayPos.Z, Body.Position.Z, (float)delta * 5);
		CameraUpRayPos.Y = Position.Y + Height - (Head.Position.Y - Position.Y) + Mathf.Abs(CamDist)/MaxCamDist * (Head.Position.Y - Position.Y);
		CameraUpRay.Position = CameraUpRayPos;

		CameraUpRay.ForceRaycastUpdate();
		if (CameraUpRay.IsColliding())
		{
			ThirdPersonPos = CameraUpRay.Position + Vector3.Up * (CameraUpRay.GetCollisionPoint().DistanceTo(CameraUpRay.Position) - .1f);
		}
		else
		{
			ThirdPersonPos.X = CameraUpRay.Position.X;
			ThirdPersonPos.Z = CameraUpRay.Position.Z;
			ThirdPersonPos.Y = Mathf.Lerp(ThirdPersonPos.Y, CameraUpRay.Position.Y + CameraUpRay.TargetPosition.Y, (float)delta * 5);
		}
		ThirdPerson.Position = ThirdPersonPos;}

		Camera.Fov = 60 - Mathf.Abs(Camera.Position.Z)/MaxCamDist * 15;

		ModelHandle(delta);
	}

	private void ModelHandle(double delta)
	{
		RLegImpact = Mathf.Lerp(RLegImpact, 0, (float)delta * 20);
		LLegImpact = Mathf.Lerp(LLegImpact, 0, (float)delta * 20);

		RArmImpact = Mathf.Lerp(RArmImpact, 0, (float)delta * 20);
		LArmImpact = Mathf.Lerp(LArmImpact, 0, (float)delta * 20);

		CrosshairImpact = Mathf.Lerp(CrosshairImpact, 1.0f, (float)delta * 20);
		Crosshair.Scale = Crosshair.Scale.Lerp(Vector2.One * .25f * Global.UiScale, (float)delta * 20);

		LArmOffset.X = Mathf.Lerp(LArmOffset.X, LArmImpact / 2, (float)delta * 10);
		LArmOffset.Y = Mathf.Lerp(LArmOffset.Y, -LArmImpact, (float)delta * 10);
		LArmOffset.Z = Mathf.Lerp(LArmOffset.Z, -LArmImpact * 2, (float)delta * 20);

		RArmOffset.X = Mathf.Lerp(RArmOffset.X, -RArmImpact / 2, (float)delta * 10);
		RArmOffset.Y = Mathf.Lerp(RArmOffset.Y, -RArmImpact, (float)delta * 10);
		RArmOffset.Z = Mathf.Lerp(RArmOffset.Z, -RArmImpact * 2, (float)delta * 20);

		if (IsOnFloor())
		{
			if (StepTimer.IsStopped() && Direction != Vector2.Zero) { MakeStep(); }
			Body.Position = RLeg.Position - (RLeg.Position - LLeg.Position)/2 + Vector3.Up * .5f;
			BodyRotation.Z = -InputDirection.X * Mathf.Pi/8;
			BodyRotation.X = -InputDirection.Y * Mathf.Pi/8;
			Head.Position = Body.Position + new Vector3(Direction.X * .2f, .45f, Direction.Y * .2f);
			LArm.Position = new Vector3(-.325f, -.075f, 0);
			RArm.Position = new Vector3(.3f, 0, -.2f);
		}
		else
		{
			LLeg.Position = Position + new Vector3(-.2f, 0, 0).Rotated(Vector3.Up, BodyRotation.Y) - new Vector3(Direction.X, 0, Direction.Y) * .25f * (Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1);
			RLeg.Position = Position + new Vector3( .2f, 0, 0).Rotated(Vector3.Up, BodyRotation.Y) - new Vector3(Direction.X, 0, Direction.Y) * .25f * (Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1);
			Body.Position = Position + Vector3.Up * .5f;
			BodyRotation.Z = -(Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1)*InputDirection.X * Mathf.Pi/8;
			BodyRotation.X = -(Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1)*InputDirection.Y * Mathf.Pi/8;
			Head.Position = Body.Position + new Vector3((Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1)*Direction.X * .2f, .45f, (Convert.ToInt32(IsFly || GravityVel.Y > 0)*2-1)*Direction.Y * .2f);
			LArm.Position = new Vector3(-.4f, -.0f, 0);
			RArm.Position = new Vector3(.375f, .075f, -.225f);
		}

		LArm.Position += LArmOffset;
		RArm.Position += RArmOffset;

		if (InputDirection != Vector2.Zero )
		{
			BodyRotation.Y = ThirdPerson.Rotation.Y;
		}

		Arms.Rotation = Arms.Rotation.Lerp(new Vector3(0, ArmsAngle, 0), (float)delta * 15);
		Head.GetChild<MeshInstance3D>(0).Rotation = Body.GetChild<MeshInstance3D>(0).Rotation;
		Body.GetChild<MeshInstance3D>(0).Position = Body.GetChild<MeshInstance3D>(0).Position.Lerp(Body.Position, (float)delta * 15); 
		Head.GetChild<MeshInstance3D>(0).Position = Head.GetChild<MeshInstance3D>(0).Position.Lerp(Head.Position, (float)delta * 12.5f); 
		RLeg.GetChild<MeshInstance3D>(0).Position = RLeg.GetChild<MeshInstance3D>(0).Position.Lerp(RLeg.Position + Vector3.Up * RLegImpact, (float)delta * 15); 
		LLeg.GetChild<MeshInstance3D>(0).Position = LLeg.GetChild<MeshInstance3D>(0).Position.Lerp(LLeg.Position + Vector3.Up * LLegImpact, (float)delta * 15); 
		

		BodyLerpAngle.X = Mathf.LerpAngle(BodyLerpAngle.X, BodyRotation.X, (float)delta * 10);
		BodyLerpAngle.Y = Mathf.LerpAngle(BodyLerpAngle.Y, BodyRotation.Y, (float)delta * 10);
		BodyLerpAngle.Z = Mathf.LerpAngle(BodyLerpAngle.Z, BodyRotation.Z, (float)delta * 15);
		Body.GetChild<MeshInstance3D>(0).Rotation = BodyLerpAngle; 

		FloorRay.Position = Body.GetChild<MeshInstance3D>(0).Position;
		if(FloorRay.IsColliding())
		{
			Shadow.Position = FloorRay.GetCollisionPoint() + Vector3.Up * 0.001f;
			float Dencity = (5/FloorRay.Position.DistanceTo(FloorRay.GetCollisionPoint()) - 1)/4;
			Shadow.Scale = Dencity * Vector3.One;
			ShadowMaterial.AlbedoColor = new Color(1,1,1,Dencity);
		}

		ScreenSpaceInteraction();
	}

	public void ScreenSpaceInteraction()
	{
		Vector2 WindowCenter = GetViewport().GetVisibleRect().Size/2;
		Crosshair.Position = WindowCenter;
		Outline.Visible = false;
		PhysicsDirectSpaceState3D SpaceState = GetWorld3D().DirectSpaceState;
		Vector2 TargetPos = WindowCenter;
		Vector3 From = ThirdPerson.Position;
		Vector3 To = From + Camera.ProjectRayNormal(TargetPos) * 100;
		PhysicsRayQueryParameters3D Query = PhysicsRayQueryParameters3D.Create(From, To, 4); 
		Dictionary Intersection = SpaceState.IntersectRay(Query);
		if (Intersection.Count > 0)
		{
			CrosshairImpact = 1.5f;
			Vector3 IntersectPos = (Vector3)Intersection["position"];
			if (IntersectPos.DistanceTo(Position + Vector3.Up * Height) <= 10)
			{
				Aimed = true;
				Vector3 Normal = (Vector3)Intersection["normal"];
				Vector3 GlobalBlockPos = (IntersectPos - Normal/2).Floor();
				Vector3 PlaceBlockPos = (GlobalBlockPos + Normal).Floor();
				AimedPos = GlobalBlockPos + Vector3.One * .5f;

				OutlineShow(GlobalBlockPos);
				
				if (Input.IsActionJustPressed("Break"))
				{
					CrosshairImpact = 0.0f;
					PlaceBlock(GlobalBlockPos, new Block(0, 0));
				}
				else if (Input.IsActionJustPressed("Place") && Normal.AngleTo(Vector3.Zero)%Mathf.Pi/4==0)
				{
					CrosshairImpact = 3f;
					ShapeCast3D Checker = new ShapeCast3D();
					GetParent().AddChild(Checker);
					BoxShape3D Shape = new BoxShape3D();
					Checker.SetCollisionMaskValue(1, true);
					Checker.Shape = Shape;
					Checker.TargetPosition = Vector3.Zero;
					Checker.GlobalPosition = PlaceBlockPos + Vector3.One/2;
					Checker.ForceShapecastUpdate();
					if (!Checker.IsColliding()) { PlaceBlock(PlaceBlockPos, new Block(_BlockSelector.CurBlockId, 0)); }
					Checker.QueueFree();
				}
			}
		}
	}

	public void OutlineShow(Vector3 GlobalBlockPos)
	{
		Outline.Visible = true;
		Outline.Position = GlobalBlockPos + Vector3.One * 0.5f;
	}

	public void PlaceBlock(Vector3 GlobalBlockPos, Block Block)
	{
		RArmImpact = 1.5f;
		if (InputDirection == Vector2.Zero)
		{
			MakeStep(2, false);
			ArmsAngle = 0;
			BodyRotation.Y = ThirdPerson.Rotation.Y;
		}

		Vector2 ChunkPos = new Vector2(Mathf.Floor(GlobalBlockPos.X/Global.ChunkSize.X), Mathf.Floor(GlobalBlockPos.Z/Global.ChunkSize.Z));
		Vector3 BlockPos = new Vector3(Mathf.PosMod(GlobalBlockPos.X, Global.ChunkSize.X), GlobalBlockPos.Y, Mathf.PosMod(GlobalBlockPos.Z, Global.ChunkSize.Z)); 
		
		_World.SetBlockLocal(ChunkPos, BlockPos, Block, true);
		_World.Remesh(ChunkPos, true, true);

		if (BlockPos.X == 0                   ) { _World.Remesh(ChunkPos + new Vector2(-1, 0), true, true); }
		if (BlockPos.X == Global.ChunkSize.X-1) { _World.Remesh(ChunkPos + new Vector2( 1, 0), true, true); }
		if (BlockPos.Z == 0                   ) { _World.Remesh(ChunkPos + new Vector2( 0,-1), true, true); }
		if (BlockPos.Z == Global.ChunkSize.Z-1) { _World.Remesh(ChunkPos + new Vector2( 0, 1), true, true); }
	}
}
