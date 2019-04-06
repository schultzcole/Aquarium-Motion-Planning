using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Code;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Pathfinder
{
	private const Single HWeight = 3.0f;

	/// <summary>
	/// Finds a path from the start point to the end point.
	/// ASSUMES THAT THE FIRST POINT IN THE points ARRAY IS THE START
	/// ASSUMES THAT THE LAST POINT IN THE points ARRAY IS THE END
	/// </summary>
	/// <param name="points">An array of the points in the PRM</param>
	/// <param name="edges">An array of edges in the PRM.</param>
	/// <returns>An array of points that make up the optimal path through the PRM from start to end.</returns>
	/*
	public static Vector2[] FindPath(Vector2[] points, float[,] edges)
	{
		Stopwatch sw = Stopwatch.StartNew();
		int len = points.Length;
		Vector2 startLoc = points[0];
		Vector2 endLoc = points[points.Length - 1];
		
		PriorityQueue queue = new PriorityQueue(len);
		
		queue.Add(new PathNode {Depth = 0, Dist = Vector2.Distance(startLoc, endLoc), Position = startLoc, ID = 0});

		List<PathNode> closedSet = new List<PathNode>();

		PathNode final = null;
		while (!queue.IsEmpty())
		{
			PathNode curr = queue.Pop();
			closedSet.Add(curr);

			if (curr.Position == endLoc)
			{
				final = curr;
				break;
			}

			for (int i = 0; i < len; i++)
			{
				Vector2 other = points[i];
				if (curr.ID == i) continue;
				if (closedSet.FindIndex(x => x.ID == i) >= 0) continue;
				float toOther = edges[curr.ID, i];
				if (Single.IsNegativeInfinity(toOther)) continue;

				var index = i;
				if (queue.Contains(x => x != null && x.ID == index))
				{
					queue.ChangeParentPathNode(x => x.ID == index, curr, toOther);
				}
				else
				{
					queue.Add(new PathNode
					{
						Position = other,
						Depth = curr.Depth + toOther,
						Dist = Vector2.Distance(other, endLoc) * HWeight,
						Parent = curr,
						ID = i
					});
				}
			}
		}

		sw.Stop();
		if (final == null)
		{
			Debug.Log("Did not find path in " + sw.ElapsedMilliseconds + "ms!");
			return null;
		}
			
		Debug.Log("Found Path in " + sw.ElapsedMilliseconds + "ms!");
		PathNode next = final;
		List<Vector2> path = new List<Vector2>();
		path.Add(next.Position);
		while (next.Parent != null)
		{
			next = next.Parent;
			path.Add(next.Position);
		}
		path.Reverse();
		float total = 0;
		for (int i = 0; i < path.Count - 1; i++)
		{
			total += Vector2.Distance(path[i], path[i + 1]);
		}
		
		Debug.Log("Path Length: " + total);
		return path.ToArray();
	}
	*/

	public static TreeNode FindPaths(Vector3[] points, float[,] edges)
	{
        Stopwatch sw = Stopwatch.StartNew();

		var len = points.Length;
		var openList = new PriorityQueue(len/2);
		openList.Add(new PathNode(points[0], 0, null, 0));

		var closedList = new List<PathNode>();

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
					openList.Add(new PathNode(points[nextIdx], totalCostToNext, current, nextIdx));
				}
			}
		}

        sw.Stop();

        Debug.Log("Found paths in " + sw.ElapsedMilliseconds + "ms");
        sw.Restart();

        TreeNode result = new TreeNode(points[0]);

		while (closedList.Count > 0)
		{
			for (int i = closedList.Count - 1; i >= 0; i--)
			{
                if (closedList[i].Parent != null)
                {
                    TreeNode parent = result.FindInDescendants(closedList[i].Parent.Position);
                    if (parent == null) continue;

                    parent.AddChild(new TreeNode(closedList[i].Position));
                }
				closedList.RemoveAt(i);
			}
		}

        sw.Stop();

        Debug.Log("Built tree in " + sw.ElapsedMilliseconds + "ms");

        return result;
	}
}