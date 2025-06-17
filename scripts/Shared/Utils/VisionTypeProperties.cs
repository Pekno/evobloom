using Godot;

public static class VisionTypeProperties
{
    public static VisionType GetVisionTypeFromGroups(Node obj)
    {
        if (obj.IsInGroup(nameof(Fruit)))
            return VisionType.Fruit;
        if (obj.IsInGroup(nameof(FruitTree)))
            return VisionType.FruitTree;
        if (obj.IsInGroup("Threat"))
            return VisionType.Threat;
        if (obj.IsInGroup("Mate"))
            return VisionType.Mate;
        if (obj.IsInGroup(nameof(Water)))
            return VisionType.Water;
        if (obj.IsInGroup("Monster"))
            return VisionType.Predator;
        if (obj.IsInGroup("Shelter"))
            return VisionType.Shelter;

        return VisionType.Unknown;
    }

    public static float GetBaseWeight(VisionType type)
    {
        switch (type)
        {
            case VisionType.Fruit: return 100f;
            case VisionType.FruitTree: return 75f;
            case VisionType.Threat: return -500f;
            case VisionType.Mate: return 50f;
            case VisionType.Water: return 120f;
            case VisionType.Predator: return -800f;
            case VisionType.Shelter: return 30f;
            default: return 0f;
        }
    }
}
