// File: scripts/Utils/BloomyNameGenerator.cs
using Godot;
using System;

public static class BloomyNameGenerator
{
    // Editor‐tweakable lists of name parts
    private static readonly string[] Prefixes = new[]
    {
        "Fluff", "Blink", "Spark", "Misty", "Wobble",
        "Pip", "Nim", "Glim", "Fuzz", "Blinky"
    };

    private static readonly string[] Suffixes = new[]
    {
        "er", "let", "o", "bee", "kin",
        "sy", "ling", "puff", "nub", "boo"
    };

    private static Random _rng = new Random();

    /// <summary>
    /// Generates a random Bloomy name, e.g. “Fluffer”, “Blinklet”, “Pipoo”, etc.
    /// </summary>
    public static string GenerateName()
    {
        // Pick one prefix and one suffix at random
        string pre = Prefixes[_rng.Next(Prefixes.Length)];
        string suf = Suffixes[_rng.Next(Suffixes.Length)];
        return pre + suf;
    }
}
