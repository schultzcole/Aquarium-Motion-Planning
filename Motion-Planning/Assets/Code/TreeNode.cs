using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class TreeNode
{
	public Vector3 Value;
	public List<TreeNode> Children = new List<TreeNode>();
	public TreeNode Parent;

	public TreeNode(Vector3 value)
	{
		Value = value;
	}

	public void AddChild(TreeNode child)
	{
		Children.Add(child);
	}

	public bool ContainedInDescendents(Vector3 value)
	{
		// Inorder traversal of the tree.
		// Postorder may provide better results, I will test that later.
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
}