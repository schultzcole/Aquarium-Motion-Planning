using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class ClosedList
{
	private List<QueueNode> _list;
	private HashSet<int> _hashSet;
	
	public ReadOnlyCollection<QueueNode> List => _list.AsReadOnly();

	public ClosedList(int capacity)
	{
		_list = new List<QueueNode>(capacity);
		_hashSet = new HashSet<Int32>();
	}

	public void Add(QueueNode node)
	{
		_list.Add(node);
		_hashSet.Add(node.ID);
	}

	public bool Contains(int id)
	{
		return _hashSet.Contains(id);
	}
}