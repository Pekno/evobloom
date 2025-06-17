using Godot;
using System;

/// <summary>
/// Handles a custom hand cursor for picking up and dragging Bloomies (left‐click)
/// and locking the camera onto them with debug enabled (right‐click).
/// Left‐click+hold drags the Bloomy; release drops it.
/// Right‐click toggles camera tracking + debug.  Clicking anywhere else clears the lock.
/// </summary>
public partial class CursorController : CanvasLayer
{
	[Export] public Camera2D Camera2D;               // Assigned in the inspector
	[Export] public Texture2D OpenHandTexture;
	[Export] public Texture2D ClosedHandTexture;
	[Export] public float HangDistance = 0f;          // Vertical offset when dragging
	[Export] public SideBarMenu BloomyFeelingsMenu { get; private set; } // Assign your SideBar_Menu instance

	private Sprite2D _cursorSprite;
	private Bloomy _draggedBloomy;
	private Bloomy _lockedBloomy;
	private Vector2 _localGrabPivot;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;

		_cursorSprite = new Sprite2D
		{
			Texture = OpenHandTexture,
			ZIndex = 1000
		};
		AddChild(_cursorSprite);

		SetProcessInput(true);
		SetProcess(true);
	}

	/// <summary>
	/// Call this on each Bloomy after spawning: wires its CharacterBody2D.input_event.
	/// </summary>
	public void RegisterForClickable(Bloomy bloomy)
	{
		var body = bloomy.GetParent<CharacterBody2D>();
		var cb = Callable.From(
			(Node viewport, InputEvent ev, long shapeIdx) =>
			{
				if (ev is InputEventMouseButton mb && mb.Pressed)
				{
					// If the sidebar menu is NOT for feelings, but for general Bloomy interaction,
					// you might want to call a more general "OnBloomyClicked" method on the sidebar.
					if (BloomyFeelingsMenu != null)
					{
						// For a right-click to show feelings menu, or perhaps a dedicated UI button.
						// If you want a different click to show this menu (e.g. middle mouse), adjust here.
						// For this example, let's say right-click (which also locks camera) also shows the menu.
						if (mb.ButtonIndex == MouseButton.Right)
						{
							BloomyFeelingsMenu.OnBloomySelected(bloomy);
						}
					}

					// Any click on a new Bloomy clears the old lock
					ClearLock();

					if (mb.ButtonIndex == MouseButton.Left)
						BeginDrag(bloomy);
					else if (mb.ButtonIndex == MouseButton.Right)
						BeginLock(bloomy);
				}
			}
		);
		body.Connect(CharacterBody2D.SignalName.InputEvent, cb);
	}

	private void BeginDrag(Bloomy bloom)
	{
		// If we were locked on someone, clear it now
		ClearLock();

		_draggedBloomy = bloom;

		// Compute world position at mouse via CanvasItem method
		var worldPos = bloom.GetGlobalMousePosition();

		// Compute pivot relative to the Bloomy's transform
		_localGrabPivot = bloom.ToLocal(worldPos);

		_cursorSprite.Texture = ClosedHandTexture;
		bloom.GetBodyPart<BehaviourComponent>()?.GrabbedAction(true);
	}

	private void EndDrag()
	{
		if (_draggedBloomy != null)
		{
			_draggedBloomy.GetBodyPart<BehaviourComponent>()?.GrabbedAction(false);
			_draggedBloomy = null;
			_cursorSprite.Texture = OpenHandTexture;
		}
	}

	private void BeginLock(Bloomy bloom)
	{
		_lockedBloomy = bloom;
		// don't interfere with drag sprite
	}

	private void ClearLock()
	{
		if (_lockedBloomy != null)
		{
			_lockedBloomy.Debug = false;
			_lockedBloomy = null;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			// RELEASE left => drop drag
			if (mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
				EndDrag();

			// PRESS right on empty space => clear lock
			if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
			{
				// If the purpose of right click on empty space is to clear ANY selection/UI:
				if (BloomyFeelingsMenu != null && BloomyFeelingsMenu.Visible)
				{
					// Check if mouse is over the feelings menu itself
					if (!BloomyFeelingsMenu.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
					{
						BloomyFeelingsMenu.HideMenu();
					}
				}
				ClearLock();
			}

			if (mb.ButtonIndex == MouseButton.Middle && mb.Pressed)
			{
				if (_lockedBloomy != null)
				{
					_lockedBloomy.Debug = !_lockedBloomy.Debug;
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		// Move custom cursor sprite to follow mouse
		_cursorSprite.GlobalPosition = GetViewport().GetMousePosition();

		// Dragging
		if (_draggedBloomy != null)
		{
			// Compute world pivot under cursor with hang distance
			var pivotWorld = _draggedBloomy.GetGlobalMousePosition() + new Vector2(0, HangDistance);

			// Position the body so its local pivot aligns under desired world pivot
			_draggedBloomy.GetParent<CharacterBody2D>().GlobalPosition = pivotWorld - _localGrabPivot;
		}

		// Camera follow when locked
		if (_lockedBloomy != null && Camera2D != null)
		{
			Camera2D.GlobalPosition = _lockedBloomy.GlobalPosition;
		}
	}
}
