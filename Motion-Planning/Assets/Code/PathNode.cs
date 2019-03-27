using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code
{
	public class PathNode
	{
		public Vector2 Position;
		public float Depth = Single.PositiveInfinity;
		public float Dist = 0;
		public float TotalCost {
			get { return Depth + Dist; }
		}
		public PathNode Parent = null;
		public int Index = 0;
	}

	public class PathNodeComparer : Comparer<PathNode>
	{
		public override Int32 Compare(PathNode x, PathNode y)
		{
			return y.TotalCost.CompareTo(x.TotalCost);
		}
	}
}