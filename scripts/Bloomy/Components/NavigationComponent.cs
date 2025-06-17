using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks Bloomy's exploration by dividing the map into fixed-size chunks.
/// Records visited chunks and provides a random target within the nearest unvisited chunk that contains tiles.
/// Caches which chunks have tiles once at startup for performant debug drawing.
/// </summary>
public partial class NavigationComponent : BloomyComponent
{
    [Export] public Vector2 ChunkSize = new Vector2(200, 200);
    [Export] public Vector2 WorldOrigin = Vector2.Zero;

    private TileMapLayer _tileMapLayer;
    [Export]
    public TileMapLayer TileMapLayer
    {
        get => _tileMapLayer; set
        {
            _tileMapLayer = value;
            BuildChunksWithTiles();
        }
    }

    private HashSet<Vector2I> _visitedChunks = new HashSet<Vector2I>();
    private HashSet<Vector2I> _chunksWithTiles = new HashSet<Vector2I>();
    private RandomNumberGenerator _rng = new RandomNumberGenerator();


    /// <summary>
    /// Scans all used cells once to determine the set of chunk indices that have tiles.
    /// </summary>
    private void BuildChunksWithTiles()
    {
        if (_tileMapLayer == null)
            return;

        Vector2 cellSize = _tileMapLayer.TileSet.TileSize;
        foreach (Vector2I cell in _tileMapLayer.GetUsedCells())
        {
            // Convert map cell to local position, then to global
            Vector2 localPos = _tileMapLayer.MapToLocal(cell) + cellSize * 0.5f;
            Vector2 worldPos = _tileMapLayer.ToGlobal(localPos);
            Vector2I chunkIdx = WorldToChunk(worldPos);
            _chunksWithTiles.Add(chunkIdx);
        }
    }

    /// <summary>
    /// Mark the given world position's chunk as visited.
    /// </summary>
    public void MarkVisited(Vector2 worldPos)
    {
        _visitedChunks.Add(WorldToChunk(worldPos));
    }

    /// <summary>
    /// Finds the nearest unvisited chunk (spiral search) that contains tiles,
    /// and returns a random position within that chunk.
    /// Returns Vector2.Zero if no suitable chunk is found within max radius.
    /// </summary>
    public Vector2 GetClosestUnvisitedChunkRandomPosition(Vector2 currentPos)
    {
        var originIdx = WorldToChunk(currentPos);
        const int maxRadius = 10;

        var possible = new List<(Vector2I, int)>();

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                foreach (int dy in new int[] { -radius, radius })
                {
                    var idx = new Vector2I(originIdx.X + dx, originIdx.Y + dy);
                    if (!_visitedChunks.Contains(idx) && _chunksWithTiles.Contains(idx))
                        possible.Add((idx, radius));
                }
            }
            for (int dy = -radius + 1; dy <= radius - 1; dy++)
            {
                foreach (int dx in new int[] { -radius, radius })
                {
                    var idx = new Vector2I(originIdx.X + dx, originIdx.Y + dy);
                    if (!_visitedChunks.Contains(idx) && _chunksWithTiles.Contains(idx))
                        possible.Add((idx, radius));
                }
            }
        }

        if (possible.Count > 0)
        {
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                var availableInRadius = possible.Where(x => x.Item2 == radius).ToList();
                if (!availableInRadius.Any()) continue;
                var randomAvailableChunck = RNGExtensions.GetItems(_rng, availableInRadius, 1).First();
                return ChunkToWorldRandomPosition(randomAvailableChunck.Item1);
            }
        }
        return Vector2.Zero;
    }

    private Vector2I WorldToChunk(Vector2 wp)
    {
        int xi = Mathf.FloorToInt((wp.X - WorldOrigin.X) / ChunkSize.X);
        int yi = Mathf.FloorToInt((wp.Y - WorldOrigin.Y) / ChunkSize.Y);
        return new Vector2I(xi, yi);
    }

    private Vector2 ChunkToWorldRandomPosition(Vector2I coord)
    {
        _rng.Randomize();
        float xMin = WorldOrigin.X + coord.X * ChunkSize.X;
        float yMin = WorldOrigin.Y + coord.Y * ChunkSize.Y;
        return new Vector2(
            xMin + _rng.RandfRange(0, ChunkSize.X),
            yMin + _rng.RandfRange(0, ChunkSize.Y)
        );
    }

    public override void ProcessComponent(float delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Debug) return;

        var originIdx = WorldToChunk(GlobalPosition);
        const int debugRadius = 5;

        // Draw chunk grid
        for (int dx = -debugRadius; dx <= debugRadius; dx++)
        {
            for (int dy = -debugRadius; dy <= debugRadius; dy++)
            {
                var idx = new Vector2I(originIdx.X + dx, originIdx.Y + dy);
                if (!_chunksWithTiles.Contains(idx))
                    continue;

                bool visited = _visitedChunks.Contains(idx);
                Vector2 worldTL = new Vector2(
                    WorldOrigin.X + idx.X * ChunkSize.X,
                    WorldOrigin.Y + idx.Y * ChunkSize.Y);
                Vector2 localTL = worldTL - GlobalPosition;
                Rect2 rect = new Rect2(localTL, ChunkSize);

                Color fillColor = visited ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
                DrawRect(rect, fillColor);
                DrawRect(rect, Colors.White, false, 2);
            }
        }
    }
}
