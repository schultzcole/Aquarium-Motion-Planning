using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An element of the PriorityQueue for pathfinding
/// </summary>
public class QueueNode
{
	public Vector3 Position;
	public float Depth;
	public QueueNode Parent;
	public int ID;

	public QueueNode(Vector3 position, float depth, QueueNode parent, int id)
	{
		Position = position;
		Depth = depth;
		Parent = parent;
		ID = id;
	}
}

public class QueueNodeComparer : Comparer<QueueNode>
{
	public override Int32 Compare(QueueNode x, QueueNode y)
	{
		return y.Depth.CompareTo(x.Depth);
	}
}