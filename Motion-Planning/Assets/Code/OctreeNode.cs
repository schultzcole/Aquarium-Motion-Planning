using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class OctreeNode
{
	private Vector3 _boundsCenter;
	private float _boundsSideLength;

	private OctreeNode _parent;

	/// <summary>
	/// Creates a new node in the octtree.
	/// </summary>
	/// <param name="boundsCenter">The center of this node's bounds</param>
	/// <param name="boundsSideLength">The side length of this nodes bounds</param>
	/// <param name="parent">This node's parent node. Can be null for the root node</param>
	protected OctreeNode(Vector3 boundsCenter, float boundsSideLength, OctreeNode parent)
	{
		_boundsCenter = boundsCenter;
		_boundsSideLength = boundsSideLength;
		_parent = parent;
	}

	public static OctreeNode CreateOctTree(Vector3[] points, Vector3 center, float sideLength)
	{
		var hashset = new HashSet<int>();
		for (int i = 0; i < points.Length; i++)
		{
			hashset.Add(i);
		}
		
		OctreeNode root = new BranchNode(points, hashset, center, sideLength, null);

		return root;
	}

	/// <summary>
	/// Takes in parameters for a sphere and returns a set of the indices of points which are in octtree bounds
	/// colliding with that sphere.
	/// WARNING: Does not return the indices of points which overlap the sphere.
	/// You still have to check the returned points against the sphere manually.
	/// This just narrows down the quantity of points that need to be checked.
	/// </summary>
	/// <param name="center">Center of the sphere to check</param>
	/// <param name="r">Radius of the sphere to check</param>
	/// <returns>A set of the indices of points which are in octtree bounds colliding with the given sphere.</returns>
	public abstract HashSet<int> CheckSphere(Vector3 center, float r);

	private static bool PointInBounds(Vector3 point, Vector3 min, Vector3 max)
	{
		return point.x < max.x && point.x > min.x && point.y < max.y && point.y > min.y && point.z < max.z &&
		       point.z > min.z;
	}

	private static bool SphereOverlapBox(Vector3 center, float r, Vector3 boundsMin, Vector3 boundsMax)
	{
		Vector3 closestPoint = Vector3.Min(Vector3.Max(center, boundsMin), boundsMax);

		return (closestPoint - center).sqrMagnitude < r * r;
	}

	private bool SphereOverlapBounds(Vector3 center, float r)
	{
		var min = _boundsCenter - Vector3.one * _boundsSideLength / 2;
		var max = _boundsCenter + Vector3.one * _boundsSideLength / 2;
		return SphereOverlapBox(center, r, min, max);
	}
	
	/// <summary>
	/// An Octree node that holds 8 child octree nodes.
	/// </summary>
	private class BranchNode : OctreeNode
	{
		private OctreeNode[] children = new OctreeNode[8];

		/// <summary>
		/// Creates a new branch node in the octtree.
		/// </summary>
		/// <param name="points">The full list of points in this octtree</param>
		/// <param name="indices">A set of indices into the points array
		/// indicating which points fall under this branch.</param>
		/// <param name="boundsCenter">The center of this node's bounds</param>
		/// <param name="boundsSideLength">The side length of this nodes bounds</param>
		/// <param name="parent">This node's parent node. Can be null for the root node</param>
		public BranchNode(Vector3[] points, HashSet<int> indices, Vector3 boundsCenter, float boundsSideLength,
			OctreeNode parent)
			: base(boundsCenter, boundsSideLength, parent)
		{
			var childHalfLength = boundsSideLength / 4;

			// min, max
			Vector3[] childCenters =
			{
				new Vector3(boundsCenter.x + childHalfLength, boundsCenter.y + childHalfLength,
					boundsCenter.z + childHalfLength),
				new Vector3(boundsCenter.x - childHalfLength, boundsCenter.y + childHalfLength,
					boundsCenter.z + childHalfLength),
				new Vector3(boundsCenter.x + childHalfLength, boundsCenter.y + childHalfLength,
					boundsCenter.z - childHalfLength),
				new Vector3(boundsCenter.x - childHalfLength, boundsCenter.y + childHalfLength,
					boundsCenter.z - childHalfLength),
				new Vector3(boundsCenter.x + childHalfLength, boundsCenter.y - childHalfLength,
					boundsCenter.z + childHalfLength),
				new Vector3(boundsCenter.x - childHalfLength, boundsCenter.y - childHalfLength,
					boundsCenter.z + childHalfLength),
				new Vector3(boundsCenter.x + childHalfLength, boundsCenter.y - childHalfLength,
					boundsCenter.z - childHalfLength),
				new Vector3(boundsCenter.x - childHalfLength, boundsCenter.y - childHalfLength,
					boundsCenter.z - childHalfLength)
			};

			HashSet<int>[] childContents = new HashSet<int>[8];

			for (int child = 0; child < 8; child++)
			{
				childContents[child] = new HashSet<int>();
			}

			for (int i = 0; i < points.Length; i++)
			{
				for (int child = 0; child < 8; child++)
				{
					if (PointInBounds(points[i],
						childCenters[child] - Vector3.one * childHalfLength,
						childCenters[child] + Vector3.one * childHalfLength))
					{
						childContents[child].Add(i);
					}
				}
			}

			if (indices.Count >= 64)
			{
				for (int child = 0; child < 8; child++)
				{
					children[child] = new BranchNode(points, childContents[child], childCenters[child],
						boundsSideLength / 2, this);
				}
			}
			else
			{
				for (int child = 0; child < 8; child++)
				{
					children[child] = new LeafNode(childContents[child], childCenters[child], boundsSideLength / 2,
						this);
				}
			}
		}

		public override HashSet<int> CheckSphere(Vector3 center, Single r)
		{
			if (SphereOverlapBounds(center, r))
			{
				HashSet<int> result = new HashSet<int>();

				foreach (var child in children)
				{
					result.UnionWith(child.CheckSphere(center, r));
				}

				return result;
			}
			
			return new HashSet<int>();
		}
	}

	/// <summary>
	/// An Octree node that holds a collection of indices of PRM points.
	/// </summary>
	private class LeafNode : OctreeNode
	{
		private HashSet<int> children;

		/// <summary>
		/// Creates a new branch node in the octtree.
		/// </summary>
		/// <param name="boundsCenter">The center of this node's bounds</param>
		/// <param name="boundsSideLength">The side length of this nodes bounds</param>
		/// <param name="parent">This node's parent node. Can be null for the root node</param>
		public LeafNode(HashSet<int> indices, Vector3 boundsCenter, float boundsSideLength, OctreeNode parent)
			: base(boundsCenter, boundsSideLength, parent)
		{
			children = indices;
		}

		public override HashSet<Int32> CheckSphere(Vector3 center, Single r)
		{
			if (SphereOverlapBounds(center, r))
			{
				return children;
			}
			
			return new HashSet<Int32>();
		}
	}
}
