using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeNode
{
	public Vector3 Value;
	public List<TreeNode> Children = new List<TreeNode>();
	public TreeNode Parent;
    public Vector3 ToParent;
	public float TotalDist;

	public TreeNode(Vector3 value)
	{
		Value = value;
	}

	public void AddChild(TreeNode child)
	{
		Children.Add(child);
        child.Parent = this;
        child.ToParent = child.Value - Value;
		child.TotalDist = TotalDist + child.ToParent.magnitude;
	}

	public bool ContainedInDescendants(Vector3 value)
	{
		bool result = false;
		foreach (var child in Children)
		{
			result |= child.ContainedInDescendants(value);
			if (result)
			{
				return true;
			}
		}

		return value == Value;
	}

	public TreeNode FindInDescendants(Vector3 value)
	{
		if (value == Value) return this;

		TreeNode inChildren;
		foreach (var child in Children)
		{
			inChildren = child.FindInDescendants(value);
			if (inChildren != null) return inChildren;
		}

		return null;
	}
	
	public IEnumerable<TreeNode> Flatten()
	{
		return new[] {this}.Concat(Children.SelectMany(x => x.Flatten()));
	}
}