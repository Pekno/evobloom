using System;
using System.Collections.Generic;
using Godot;

public partial class Fruit : CanBeSeenNode2D
{
    [Export] public Sprite2D Sprite;
    FruitType _fruitType;

    [Export] public StaticBody2D body2D;

    private bool _isPickable = false;

    public Action EatFruitCallback;

    public bool IsPickable
    {
        get => _isPickable;
        set
        {
            _isPickable = value;
            body2D.Visible = value;
            body2D.GetChild<CollisionShape2D>(0).SetDeferred("disabled", !value);
        }
    }

    private Dictionary<FruitType, int> _fruitTypeToName = new()
    {
        { FruitType.Apple, 24 },
        { FruitType.Orange, 26 },
        { FruitType.Pear, 28 },
        { FruitType.Peach, 30 },
        { FruitType.Strawberry, 48 },
        { FruitType.Raspberry, 50 },
        { FruitType.Blueberry, 52 },
    };

    public FruitType FruitType
    {
        get => _fruitType;
        set
        {
            _fruitType = value;
            Sprite.Frame = (int)_fruitTypeToName[_fruitType];
        }
    }

    public float Eat()
    {
        if (EatFruitCallback != null) EatFruitCallback();
        return 0.1f;
    }

    public override bool IsConsidered()
    {
        return Visible && IsPickable;
    }
}
