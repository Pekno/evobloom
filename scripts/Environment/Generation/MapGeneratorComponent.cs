using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Procedurally generates a 2D tile-based world with water, ground, and decorative layers using noise.
/// Holes for water bodies are carved based on noise thresholds; ground fills remaining cells, and optional decorations scatter above.
/// </summary>
[Tool]
public partial class MapGeneratorComponent : Node2D
{
    [ExportToolButton("Regenerate Map")]
    public Callable RegenerateMapButton => Callable.From(GenerateMap);
    [Export] public bool GenerateMapOnLoad = false;

    [ExportGroup("Map Settings")]
    [Export] public Vector2I MapSize = new Vector2I(100, 100);

    [ExportGroup("Noise Settings")]
    [Export] public int Seed = 0;
    [Export] public float NoiseScale = 20f;
    [Export] public int Octaves = 4;
    [Export] public float Persistence = 0.5f;
    [Export] public float Lacunarity = 2.0f;
    [Export] public float WaterThreshold = -0.2f;
    [Export] public float GrassThreshold = 0.6f;
    [Export] public float StonesThreshold = 0.8f;

    [ExportGroup("TileMap Layers")]
    [Export] public TileMapLayer WaterLayer;
    [Export] public TileMapLayer WaterBackgroundLayer;
    [Export] public TileMapLayer GroundLayer;
    [Export] public TileMapLayer ForegroundLayer;

    [ExportGroup("Tile IDs")]
    [Export] public int WaterTileId = 0;
    [Export] public int GroundTileId = 1;
    [Export] public int DecorationTileId = 3;

    private FastNoiseLite _noise;
    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _EnterTree()
    {
        if (GenerateMapOnLoad)
            GenerateMap();
    }

    private void InitializeNoise()
    {
        _rng.Randomize();
        Seed = _rng.RandiRange(0, int.MaxValue);
        _noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = Seed,
            Frequency = 1.0f / NoiseScale,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = Octaves,
            FractalLacunarity = Lacunarity,
            FractalGain = Persistence
        };
    }

    /// <summary>
    /// Single-pass map generation: clears layers, then applies water, ground, and decorations.
    /// </summary>
    private void GenerateMap()
    {
        if (WaterLayer == null || GroundLayer == null || ForegroundLayer == null || WaterBackgroundLayer == null)
        {
            GD.PrintErr("MapGeneratorComponent: Assign all TileMapLayers in the inspector.");
            return;
        }
        InitializeNoise();
        WaterLayer.Clear();
        GroundLayer.Clear();
        WaterBackgroundLayer.Clear();
        ForegroundLayer.Clear();

        for (int y = -MapSize.Y / 2; y < MapSize.Y / 2; y++)
        {
            for (int x = -MapSize.X / 2; x < MapSize.X / 2; x++)
            {
                var cellPos = new Vector2I(x, y);
                float value = _noise.GetNoise2D(x, y);

                WaterBackgroundLayer.SetCell(cellPos, WaterTileId, Vector2I.Zero);
                if (value < WaterThreshold)
                {
                    WaterLayer.SetCell(cellPos, WaterTileId, Vector2I.Zero);
                    continue;
                }
                GroundLayer.SetCellsTerrainConnect([cellPos], 0, 0);

                float grassSample = _noise.GetNoise2D(x * 2, y * 2);
                if (grassSample > GrassThreshold)
                {
                    // Grass is (0, 2) to (3, 2)
                    int choice = _rng.RandiRange(0, 3);
                    ForegroundLayer.SetCell(cellPos, DecorationTileId, new Vector2I(choice, 2));
                }

                float stoneSample = _noise.GetNoise2D(x * 2, y * 2);
                if (stoneSample > StonesThreshold)
                {
                    // Stones is (0, 1) to (5, 1)
                    int choice = _rng.RandiRange(0, 5);
                    ForegroundLayer.SetCell(cellPos, DecorationTileId, new Vector2I(choice, 1));
                }
            }
        }
    }
}
