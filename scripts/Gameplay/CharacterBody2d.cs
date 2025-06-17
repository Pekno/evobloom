using Godot;
using System;

public partial class CharacterBody2d : CharacterBody2D
{
	public const float Speed = 300.0f;
	[Export] public NodePath CameraPath;
	[Export] public float ZoomStep = 0.1f;
	[Export] public float MinZoom = 0.5f;
	[Export] public float MaxZoom = 2.0f;

	private Camera2D _camera;

	public override void _Ready()
	{
		_camera = GetNode<Camera2D>(CameraPath);
		if (_camera == null)
			GD.PrintErr("Camera2D not found at path: " + CameraPath);
	}

	public override void _Input(InputEvent @event)
	{
		if (_camera == null)
			return;

		if (@event is InputEventMouseButton mb && mb.IsPressed())
		{
			// WheelUp to zoom in, WheelDown to zoom out
			if (mb.ButtonIndex == MouseButton.WheelUp)
				AdjustZoom(ZoomStep);
			else if (mb.ButtonIndex == MouseButton.WheelDown)
				AdjustZoom(-ZoomStep);
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Y = direction.Y * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void AdjustZoom(float delta)
	{
		Vector2 newZoom = _camera.Zoom + new Vector2(delta, delta);
		newZoom.X = Mathf.Clamp(newZoom.X, MinZoom, MaxZoom);
		newZoom.Y = Mathf.Clamp(newZoom.Y, MinZoom, MaxZoom);
		_camera.Zoom = newZoom;
	}

}
