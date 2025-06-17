using Godot;
using System;
using System.Collections.Generic;

public partial class WaterMapper : Node2D
{
    [Export] public NodePath TileMapLayerPath;
    [Export] public PackedScene WaterSourceScene;            // Your water_source.tscn
    [Export] public Vector2 CellSize = new Vector2(64, 64);  // Match your TileMapLayer cell size

    [Export] public bool Debug = false; // Enable debug outlines

    private TileMapLayer _layer;

    // Store each lake's world‐space Rect2 for debug drawing
    private readonly List<Rect2> _waterRects = new List<Rect2>();

    public override void _Ready()
    {
        _layer = GetNode<TileMapLayer>(TileMapLayerPath);
        if (_layer == null || WaterSourceScene == null)
        {
            GD.PrintErr("WaterMapper: TileMapLayer or WaterSourceScene not assigned!");
            return;
        }

        // 1) Gather all used cells (every one is water on this layer)
        var waterCells = new HashSet<Vector2I>(_layer.GetUsedCells());

        // 2) Flood-fill clusters of adjacent water cells (4-way)
        var clusters = new List<List<Vector2I>>();
        var visited = new HashSet<Vector2I>();
        var dirs = new[] {
            new Vector2I( 1,  0),
            new Vector2I(-1,  0),
            new Vector2I( 0,  1),
            new Vector2I( 0, -1),
        };

        foreach (var start in waterCells)
        {
            if (visited.Contains(start)) continue;

            var stack = new Stack<Vector2I>();
            var cluster = new List<Vector2I>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var cell = stack.Pop();
                if (visited.Contains(cell) || !waterCells.Contains(cell))
                    continue;

                visited.Add(cell);
                cluster.Add(cell);

                foreach (var d in dirs)
                {
                    var nbr = cell + d;
                    if (!visited.Contains(nbr) && waterCells.Contains(nbr))
                        stack.Push(nbr);
                }
            }

            clusters.Add(cluster);
        }

        // 3) For each cluster, compute world‐space Rect2, spawn WaterSource, and record for debug
        foreach (var cluster in clusters)
        {
            // Compute grid‐space bounds
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var c in cluster)
            {
                if (c.X < minX) minX = c.X;
                if (c.X > maxX) maxX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.Y > maxY) maxY = c.Y;
            }

            // Build world‐space rect using CellSize and layer position
            Vector2 worldTL = _layer.GlobalPosition
                            + new Vector2(minX * CellSize.X, minY * CellSize.Y);
            Vector2 worldBR = _layer.GlobalPosition
                            + new Vector2((maxX + 1) * CellSize.X, (maxY + 1) * CellSize.Y);
            Rect2 worldRect = new Rect2(worldTL, worldBR - worldTL);

            // Instantiate WaterSource at the center
            var instance = (Node2D)WaterSourceScene.Instantiate();
            AddChild(instance);
            instance.Position = worldRect.Position + worldRect.Size * 0.5f;

            // Rebuild its CollisionPolygon2D to match the rectangle
            var poly = instance.GetNode<CollisionPolygon2D>("StaticBody2D/CollisionPolygon2D");
            if (poly != null)
            {
                var half = worldRect.Size * 0.5f;
                poly.Polygon = new Vector2[] {
                    new Vector2(-half.X, -half.Y),
                    new Vector2( half.X, -half.Y),
                    new Vector2( half.X,  half.Y),
                    new Vector2(-half.X,  half.Y),
                };
            }
            else
            {
                GD.PrintErr("WaterMapper: CollisionPolygon2D not found in WaterSourceScene!");
            }

            // Save for debug outline
            _waterRects.Add(worldRect);
        }
    }

    public override void _Draw()
    {
        if (!Debug)
            return;

        // Draw each lake's outline in cyan, 2px wide
        foreach (var rect in _waterRects)
        {
            // Build the four corners, then close the loop
            var tl = rect.Position;
            var tr = rect.Position + new Vector2(rect.Size.X, 0);
            var br = rect.Position + rect.Size;
            var bl = rect.Position + new Vector2(0, rect.Size.Y);

            DrawPolyline(
                new Vector2[] { tl, tr, br, bl, tl },
                Colors.Cyan,
                2.0f
            );
        }
    }
}
