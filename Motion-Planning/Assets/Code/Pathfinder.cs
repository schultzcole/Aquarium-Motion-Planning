using System;
using UnityEngine;

namespace Code
{
    public static class Pathfinder
    {
        /// <summary>
        /// Finds a path from the start point to the end point.
        /// ASSUMES THAT THE FIRST POINT IN THE points ARRAY IS THE START
        /// ASSUMES THAT THE LAST POINT IN THE points ARRAY IS THE END
        /// </summary>
        /// <param name="points">An array of the points in the PRM</param>
        /// <param name="edges">An array of edges in the PRM.</param>
        /// <returns>An array of points that make up the optimal path through the PRM from start to end.</returns>
        public static Vector2[] FindPath(Vector2[] points, Boolean[,] edges)
        {
            Vector2 start = points[0];
            Vector2 end = points[points.Length];

            throw new NotImplementedException();
        }
    }
}