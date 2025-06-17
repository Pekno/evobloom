using Godot;
using System;
using System.Diagnostics;

public partial class BloomySpawnerComponent : Node2D
{
	// Enable debug mode
	[Export]
	public bool Debug = false;

	// Expose the Bloomy scene for instantiation
	[Export]
	public PackedScene BloomyScene;

	// How big the world is
	[Export] public Vector2 SpawnAreaSize = new Vector2(800, 600);
	// Prevent spawn near the sides
	[Export] public Vector2 Margin = new Vector2(50, 50);

	[Export]
	public int BloomyCount = 5; // Number of Bloomies to create

	[Export] public TileMapLayer TileMapLayer;

	// For consistent randomness
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		// Optional: Print a message when the scene is ready
		GD.Print("Welcome to EvoBloom!");

		// Check if the Bloomy scene is assigned in the inspector
		if (BloomyScene == null)
		{
			GD.PrintErr("BloomyScene is not assigned!");
			return;
		}

		// Seed once for this session
		_rng.Randomize();

		var cursorController = GetNode<CursorController>("%CursorController");

		for (int i = 0; i < BloomyCount; i++)
		{
			// Instantiate a Bloomy
			var bloomieInstance = (Node2D)BloomyScene.Instantiate();

			// Assign debug flag (and unique names if you like)
			var bloomy = bloomieInstance.GetNodeOrNull<Bloomy>("%Bloomy");


			// Compute a random position within [Margin, SpawnAreaSize - Margin]
			float minX = -SpawnAreaSize[0] / 2 + Margin[0];
			float maxX = SpawnAreaSize[0] / 2 - Margin[0];
			float minY = -SpawnAreaSize[1] / 2 + Margin[1];
			float maxY = SpawnAreaSize[1] / 2 - Margin[1];

			float x = _rng.RandfRange(minX, maxX);
			float y = _rng.RandfRange(minY, maxY);
			bloomieInstance.Position = new Vector2(x, y);

			AddChild(bloomieInstance);

			if (bloomy != null)
			{
				bloomy.Debug = Debug;
				cursorController.RegisterForClickable(bloomy);
				bloomy.GetBodyPart<NavigationComponent>().TileMapLayer = TileMapLayer;
			}
		}
	}
}
