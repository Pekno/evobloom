using Godot;

namespace Utils
{
    public static class CanvasItemExtensions
    {
        /// <summary>
        /// Draws a dotted line between two points by placing dots (circles) along the segment.
        /// Use just like CanvasItem.DrawLine inside _Draw().
        /// </summary>
        /// <param name="canvas">Any CanvasItem (e.g. Node2D) in its _Draw() method.</param>
        /// <param name="from">Start point of the line.</param>
        /// <param name="to">End point of the line.</param>
        /// <param name="color">Color of the dots.</param>
        /// <param name="dotRadius">Radius of each dot in pixels.</param>
        /// <param name="dotSpacing">Distance in pixels between consecutive dots.</param>
        public static void DrawDottedLine(
            this CanvasItem canvas,
            Vector2 from,
            Vector2 to,
            Color color,
            float dotRadius = 2f,
            float dotSpacing = 10f
        )
        {
            // Compute direction and total length
            Vector2 delta = to - from;
            float length = delta.Length();
            Vector2 direction = delta.Normalized();

            // Step along the line in increments of dotSpacing
            float traveled = 0f;
            while (traveled <= length)
            {
                Vector2 point = from + direction * traveled;
                canvas.DrawCircle(point, dotRadius, color);
                traveled += dotSpacing;
            }
        }
    }
}
