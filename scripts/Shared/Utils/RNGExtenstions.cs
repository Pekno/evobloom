using System.Collections.Generic;
using Godot;

public static class RNGExtensions
{
    /// <summary>
    /// Returns up to `count` distinct items randomly selected from `source`.
    /// </summary>
    public static List<T> GetItems<T>(this RandomNumberGenerator rng, IList<T> source, int count)
    {
        var array = new List<T>(source);
        rng.Randomize();
        // Fisher–Yates shuffle
        for (int i = array.Count - 1; i > 0; i--)
        {
            int j = (int)rng.RandiRange(0, i);
            var tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }
        // then take the first `count` (or as many as you have)
        return array.GetRange(0, Mathf.Min(count, array.Count));
    }
}
