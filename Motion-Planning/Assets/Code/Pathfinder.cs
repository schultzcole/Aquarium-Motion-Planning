using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Pathfinder
{
	public PathNode[] Results;

	public Exception Error;

	/// <summary>
	/// Tries to find the shortest path from every point in the PRM to the goal.
	/// The goal is assumed to be the first node in the list of points.
	/// If an exception is encountered it will store it in the "error" field.
	/// </summary>
	/// <param name="points">A list of points in the PRM</param>
	/// <param name="edges">A matrix of edges in the PRM</param>
	/// <param name="ct">A cancellation token to signal the task to abort early.</param>
	public void TryFindPaths(Vector3[] points, float[,] edges, CancellationToken ct)
	{
		// This is an evil hack because Unity doesn't natively support exceptions on background threads.
		// If an exception is encountered when trying to find paths, this will catch it and store the exception,
		// rather than the default which is to ignore it entirely.
		// At the very least it allows us to view the exception details in a debugger.

		try
		{
			FindPaths(points, edges, ct);
		}
		catch (Exception e)
		{
			Error = e;
		}
	}

	/// <summary>
	/// Finds the shortest path from every point in the PRM to the goal.
	/// The goal is assumed to be the first node in the list of points.
	/// </summary>
	/// <param name="points">A list of points in the PRM</param>
	/// <param name="edges">A matrix of edges in the PRM</param>
	/// <param name="ct">A cancellation token to signal the task to abort early.</param>
	private void FindPaths(Vector3[] points, float[,] edges, CancellationToken ct)
	{
		Debug.Log("Pathfind Begun");
		Stopwatch sw = Stopwatch.StartNew();

		var len = points.Length;
		var openList = new PriorityQueue(len/2);
		openList.Add(new QueueNode(points[0], 0, null, 0));

		var closedList = new ClosedList(len);

		while (!openList.IsEmpty())
		{
			if (ct.IsCancellationRequested)
			{
				Debug.Log("Pathfind Cancelled");
				throw new TaskCanceledException();
			}
			
			var current = openList.Pop();
			closedList.Add(current);

			for (int nextIdx = 0; nextIdx < len; nextIdx++)
			{
				if (nextIdx == current.ID) continue;
				var costCurrentToNext = edges[current.ID, nextIdx];
				if (float.IsNegativeInfinity(costCurrentToNext)) continue;
				if (closedList.Contains(nextIdx)) continue;

				var totalCostToNext = current.Depth + costCurrentToNext;

				if (openList.Contains(nextIdx))
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

		Results = new PathNode[len];
		foreach (var pnode in closedList.List)
		{
			for (int i = 0; i < len; i++)
			{
				if (pnode.ID != i) continue;

				Vector3 direction = pnode.Parent != null ? pnode.Parent.Position - pnode.Position: Vector3.zero;
				Results[i] = new PathNode(pnode.Position, direction, pnode.Depth);
				break;
			}
		}
	}
}