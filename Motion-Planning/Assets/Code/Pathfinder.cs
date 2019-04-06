using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Code;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Pathfinder
{
	/// <summary>
	/// Finds the shortest path from every point in the PRM to the goal.
	/// The goal is assumed to be the first node in the list of points.
	/// </summary>
	/// <param name="points">A list of points in the PRM</param>
	/// <param name="edges">A matrix of edges in the PRM</param>
	/// <returns>An array of Rays, which indicate a PRM point and
	/// the direction to move from that point on the optimal path to the goal.</returns>
	public static PathNode[] FindPaths(Vector3[] points, float[,] edges)
	{
		Stopwatch sw = Stopwatch.StartNew();

		var len = points.Length;
		var openList = new PriorityQueue(len/2);
		openList.Add(new QueueNode(points[0], 0, null, 0));

		var closedList = new List<QueueNode>();

		while (!openList.IsEmpty())
		{
			var current = openList.Pop();
			closedList.Add(current);

			for (int nextIdx = 0; nextIdx < len; nextIdx++)
			{
				if (nextIdx == current.ID) continue;
				if (closedList.Exists(x => nextIdx == x.ID)) continue;
				var costCurrentToNext = edges[current.ID, nextIdx];
				if (float.IsNegativeInfinity(costCurrentToNext)) continue;

				var totalCostToNext = current.Depth + costCurrentToNext;

				if (openList.Contains(x => x.ID == nextIdx))
				{
					openList.ReparentPathNode(nextIdx, current, costCurrentToNext);
				}
				else
				{
					openList.Add(new QueueNode(points[nextIdx], totalCostToNext, current, nextIdx));
				}
			}
		}

		sw.Stop();

		Debug.Log("Found paths in " + sw.ElapsedMilliseconds + "ms");
		sw.Restart();

		var result = new PathNode[len];
		foreach (var pnode in closedList)
		{
			for (int i = 0; i < len; i++)
			{
				if (pnode.Position != points[i]) continue;

				Vector3 direction = pnode.Parent != null ? pnode.Parent.Position - pnode.Position: Vector3.zero;
				result[i] = new PathNode(pnode.Position, direction, pnode.Depth);
				break;
			}
		}

		sw.Stop();

		Debug.Log("Built tree in " + sw.ElapsedMilliseconds + "ms");

		return result;
	}
}