using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code
{
	public class PathNode
	{
		public Vector3 Position;
		public float Depth = Single.PositiveInfinity;
//		public float Dist = 0;
//		public float TotalCost => Depth + Dist;
		public PathNode Parent = null;
		public int ID = 0;

		public PathNode(Vector3 position, float depth, PathNode parent, int id)
		{
			Position = position;
			Depth = depth;
			Parent = parent;
			ID = id;
		}
	}

	public class PathNodeComparer : Comparer<PathNode>
	{
		public override Int32 Compare(PathNode x, PathNode y)
		{
			return y.Depth.CompareTo(x.Depth);
		}
	}
}