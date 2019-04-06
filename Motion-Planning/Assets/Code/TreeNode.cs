using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeNode
{
	public Vector3 Value;
	public List<TreeNode> Children = new List<TreeNode>();
	public TreeNode Parent;
	public int Depth = 0;

	public TreeNode(Vector3 value)
	{
		Value = value;
	}

	public void AddChild(TreeNode child)
	{
		Children.Add(child);
		child.Depth = Depth + 1;
	}

	public bool ContainedInDescendents(Vector3 value)
	{
		bool result = false;
		foreach (var child in Children)
		{
			result |= child.ContainedInDescendents(value);
			if (result)
			{
				return true;
			}
		}

		return value == Value;
	}
	
	public IEnumerable<TreeNode> Flatten()
	{
		return new[] {this}.Concat(Children.SelectMany(x => x.Flatten()));
	}
}