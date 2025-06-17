using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Spawns berry bushes based on noise-driven placement, ensuring they're on ground tiles
/// with a one-tile margin from water edges, and centered within tiles.
/// </summary>
[Tool]
public partial class BerrySpawnerComponent : Node2D
{
    [ExportToolButton("Regenerate Bushes")]
    public Callable RegenerateBushesButton => Callable.From(SpawnBushes);
    [Export] public bool GenerateBushesOnLoad = false;
    [Export] public PackedScene[] SpawnableBushes; // Variants of berry bush scenes
    [Export] public float BushesProportion = .01f;
    [Export] public TileMapLayer GroundLayer;
    [Export] public int MarginTiles = 1;
    [Export] public int Seed = 0;
    [Export] public float NoiseScale = 0.1f; // frequency of noise sampling

    private FastNoiseLite _noise;
    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        if (SpawnableBushes == null || SpawnableBushes.Length == 0)
        {
            GD.PrintErr("BerrySpawnerComponent: No bush scenes assigned.");
            return;
        }
        if (GroundLayer == null)
        {
            GD.PrintErr("BerrySpawnerComponent: GroundLayer not assigned.");
            return;
        }

        if (GenerateBushesOnLoad)
            SpawnBushes();
    }

    private void InitializeNoise()
    {
        _rng.Randomize();
        Seed = _rng.RandiRange(0, int.MaxValue);
        _noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = Seed,
            Frequency = NoiseScale
        };
    }

    private void SpawnBushes()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        InitializeNoise();
        var _cells = GroundLayer.GetUsedCells();
        // Score each ground cell by noise value
        var scored = new List<(Vector2I cell, float score)>();
        foreach (var cell in _cells)
        {
            // sample noise at cell center
            float n = _noise.GetNoise2D(cell.X + 0.5f, cell.Y + 0.5f);
            scored.Add((cell, n));
        }

        // define the four cardinal neighbor offsets
        var neighborOffsets = new Vector2I[] {
            new Vector2I( 1,  0),
            new Vector2I(-1,  0),
            new Vector2I( 0,  1),
            new Vector2I( 0, -1),
        };


        var numberOfBushes = (int)Math.Truncate(_cells.Count * BushesProportion);

        // sort descending
        scored.Sort((a, b) => b.score.CompareTo(a.score));
        scored = scored.Take(numberOfBushes).ToList();
        // only keep positions where every neighbor is ground
        var valid = scored
            .Where(entry =>
                neighborOffsets.All(off =>
                    GroundLayer.GetCellTileData(entry.cell + off) != null
                )
            )
            .ToList();

        int toSpawn = Math.Min(numberOfBushes, scored.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            var cell = scored[i].cell;
            var scene = SpawnableBushes[GD.Randi() % SpawnableBushes.Length];
            var bush = scene.Instantiate<Node2D>();
            AddChild(bush);
            bush.Owner = GetTree().EditedSceneRoot;

            Vector2 cellSize = GroundLayer.TileSet.TileSize;
            // Convert map cell to local position, then to global
            Vector2 localPos = GroundLayer.MapToLocal(cell) + cellSize * 0.5f;
            Vector2 worldPos = GroundLayer.ToGlobal(localPos);

            bush.Position = worldPos;
        }
    }
}
