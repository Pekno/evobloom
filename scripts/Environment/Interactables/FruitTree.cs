using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
/// Represents a berry bush that can be harvested by Bloomies.
/// When BerryCount reaches zero, the bush is removed.
/// </summary>
public partial class FruitTree : CanBeSeenNode2D
{
    /// <summary>Current number of berries available.</summary>
    [Export] public Marker2D[] SpawnLocation = [];

    [Export] public AnimationTree tree;

    [Export] public PackedScene Fruits;

    [Export] public FruitType FruitType;

    [Export] public float RespawnDelay = 20f;

    [Export] public int[] SpawnnableFruitTypes;

    private List<Fruit> FruitsList = new List<Fruit>();

    public int FruitCount { get => FruitsList.Where(f => f.Visible).Count(); }

    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    private bool fruitOnFloor = false;

    public override void _Ready()
    {
        tree.AnimationFinished += OnAnimationFinished;
        // Get a random type of fruit
        var FruitType = (FruitType)RNGExtensions.GetItems(_rng, SpawnnableFruitTypes, 1).First();

        // Spawn all Fruit scenes
        for (int i = 0; i < SpawnLocation.Length; i++)
        {
            var fruit = Fruits.Instantiate<Fruit>();
            fruit.FruitType = FruitType;
            fruit.Visible = false;
            fruit.IsPickable = false;
            fruit.EatFruitCallback = () => OnEatFruit(fruit);
            SpawnLocation[i].AddChild(fruit);
            FruitsList.Add(fruit);
        }

        Spawn();
    }

    public void Spawn()
    {
        tree.Set("parameters/conditions/is_dropped", false);
        _rng.Randomize();
        // Get a random number of fruits to spawn
        var nbrFruit = _rng.RandiRange(1, SpawnLocation.Length);
        var selectedFruits = RNGExtensions.GetItems(_rng, FruitsList, nbrFruit);

        foreach (var fruit in selectedFruits)
            fruit.Visible = true;

        fruitOnFloor = false;

        tree.Set("parameters/conditions/is_spawn", true);
    }

    public void Shake()
    {
        tree.Set("parameters/conditions/is_spawn", false);

        tree.Set("parameters/conditions/is_dropped", true);
    }

    private void OnAnimationFinished(StringName animName)
    {
        // Godot will pass the name of the finished animation;
        // only do your fruit‐enabling logic if it’s the “shake” one:
        if (animName == "shake")
        {
            foreach (var fruit in FruitsList)
                fruit.IsPickable = true;
        }
        fruitOnFloor = true;
    }

    private void OnEatFruit(Fruit fruit)
    {
        //fruit.Visible = false;
        fruit.SetDeferred("visible", false);
        // if that was the last one, start the respawn timer
        if (FruitCount == 0)
        {
            GetTree()
                .CreateTimer(RespawnDelay)
                .Connect("timeout",
                         new Callable(this, nameof(OnRespawnTimer)));
        }
    }

    private void OnRespawnTimer()
    {
        // simply call Spawn again
        Spawn();
    }

    public override bool IsConsidered()
    {
        return FruitCount > 0 && !fruitOnFloor;
    }
}
