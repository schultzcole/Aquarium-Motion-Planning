using System;
using System.Collections.Generic;
using System.Linq;
using Code;
using UnityEngine;

/// <summary>
/// A priority queue, in which elements are added and can be removed in order of priority, as defined by TComparer.
/// </summary>
public class PriorityQueue
{
	/// <summary>
	/// The underlying array representing the queue. Internally the queue is implemented as a heap structure,
	/// stored in array form here.
	/// </summary>
	private PathNode[] _array;

	/// <summary>
	/// The current capacity of the queue. Represents how much memory is allocated for _array.
	/// As more elements are added to the queue, the capacity will increase to match.
	///
	/// Different than the Count, which is the actual number of elements. This is the number of slots currently
	/// allocated for elements.
	/// </summary>
	private Int32 _capacity;

	/// <summary>
	/// The comparer object to use to compare elements in the array.
	/// </summary>
	private readonly PathNodeComparer _comparer;

	/// <summary>
	/// How many elements are currently in the queue.
	/// </summary>
	public Int32 Count { get; private set; }

	/// <inheritdoc />
	/// <summary>
	/// Create a new Priority Queue with default capacity (8) and comparer.
	/// </summary>
	public PriorityQueue() : this(8, new PathNodeComparer()) { }

	/// <inheritdoc />
	/// <summary>
	/// Create a new Priority Queue with given initial capacity and a default comparer.
	/// </summary>
	/// <param name="initialCapacity">Initial slots to allocate.</param>
	public PriorityQueue(Int32 initialCapacity) : this(initialCapacity, new PathNodeComparer()) { }

	/// <summary>
	/// Create a new Priority Queue with given initial capacity and comparer.
	/// </summary>
	/// <param name="initialCapacity">Initial slots to allocate.</param>
	/// <param name="comparer">Comparer object to use to compare elements in the queue.</param>
	public PriorityQueue(Int32 initialCapacity, PathNodeComparer comparer)
	{
		_capacity = initialCapacity;
		_array = new PathNode[initialCapacity + 1];
		Count = 0;

		_comparer = comparer;
	}

	/// <summary>
	/// Adds an element to the PriorityQueue.
	/// </summary>
	/// <param name="value">The element to add.</param>
	public void Add(PathNode value)
	{
		if (Count + 1 >= _capacity)
		{
			_capacity *= 2;
			Array.Resize(ref _array, _capacity);
		}

		_array[++Count] = value;
		BubbleUp(Count);
	}

	/// <summary>
	/// Returns the first item in the queue. Does not modify the queue.
	/// </summary>
	/// <returns>First item in the queue.</returns>
	/// <exception cref="QueueEmptyException">Thrown if the queue is empty.</exception>
	public PathNode Peek()
	{
		if (IsEmpty())
		{
			throw new QueueEmptyException();
		}

		return _array[1];
	}

	/// <summary>
	/// Removes and returns the first item in the queue.
	/// </summary>
	/// <returns>First item in the queue.</returns>
	/// <exception cref="QueueEmptyException">Thrown if the queue is empty.</exception>
	public PathNode Pop()
	{
		if (IsEmpty())
		{
			throw new QueueEmptyException();
		}

		PathNode result = _array[1];
		_array[1] = _array[Count--];
		BubbleDown(1);
		return result;
	}

	/// <summary>
	/// Returns whether the queue contains any elements.
	/// </summary>
	/// <returns>Whether the queue contains any elements.</returns>
	public Boolean IsEmpty()
	{
		return Count == 0;
	}

	/// <summary>
	/// Changes a PathNode's parent and depth, thereby altering it's priority in the queue.
	/// </summary>
	/// <param name="thisId">The id of the node to change</param>
	/// <param name="newParent">The new parent of the node to change</param>
	/// <param name="distParentToThis">The distance from the node to change to its parent</param>
	/// <returns>True if successful, false if a node with that ID does not exist or
	/// if the existing depth is lower than the new depth.</returns>
	public bool ReparentPathNode(int thisId, PathNode newParent, float distParentToThis)
	{
		int arrIndex;
		try
		{
			arrIndex = Array.FindIndex(_array, 1, Count, x => x != null && x.ID == thisId);
		}
		catch
		{
			return false;
		}

		if (_array[arrIndex].Depth < newParent.Depth + distParentToThis)
		{
			return false;
		}

		_array[arrIndex].Parent = newParent;
		_array[arrIndex].Depth = newParent.Depth + distParentToThis;
		var parent = ParentIndex(arrIndex);
		while (arrIndex > 1 && _comparer.Compare(_array[arrIndex], _array[parent]) > 0)
		{
			SwapElements(parent, arrIndex);
			arrIndex = parent;
			parent = ParentIndex(arrIndex);
		}

		return true;
	}

	public bool Contains(Func<PathNode, bool> pred)
	{
        for (int i = 1; i <= Count; ++i)
        {
            if (_array[i] == null) continue;
            if (pred(_array[i])) return true;
        }

		return false;
	}

	/// <summary>
	/// Pushes a higher value down to its correct location in the tree.
	/// </summary>
	/// <param name="index">The index of the value to push down.</param>
	private void BubbleDown(Int32 index)
	{
		var maxIndex = index;
		PathNode maxValue = _array[index];
		var left = LeftChildIndex(index);
		var right = RightChildIndex(index);
		if (left <= Count && _comparer.Compare(_array[index], _array[left]) < 0)
		{
			maxValue = _array[left];
			maxIndex = left;
		}

		if (right <= Count && _comparer.Compare(maxValue, _array[right]) < 0)
		{
			maxIndex = right;
		}

		if (maxIndex == index)
		{
			return;
		}

		SwapElements(index, maxIndex);

		BubbleDown(maxIndex);
	}

	/// <summary>
	/// Pulls a lower value up to the correct position in the tree.
	/// </summary>
	/// <param name="index">The index of the element to pull up.</param>
	private void BubbleUp(Int32 index)
	{
		if (index < 0)
		{
			return;
		}

		var parent = ParentIndex(index);
		if (parent > 0 && _comparer.Compare(_array[index], _array[parent]) > 0)
		{
			SwapElements(index, parent);
			BubbleUp(parent);
		}
	}
	
	/// <summary>
	/// Swaps two elements in the array, given their indices.
	/// </summary>
	/// <param name="aIndex">Index of the first element to swap.</param>
	/// <param name="bIndex">Index of the second element to swap.</param>
	private void SwapElements(Int32 aIndex, Int32 bIndex)
	{
		PathNode tmp = _array[aIndex];
		_array[aIndex] = _array[bIndex];
		_array[bIndex] = tmp;
	}

	/// <summary>
	/// Gets the index of a given index's left child.
	/// </summary>
	/// <param name="index">The parent index.</param>
	/// <returns>The left child index.</returns>
	private static Int32 LeftChildIndex(Int32 index)
	{
		return index << 1;
	}

	/// <summary>
	/// Gets the index of a given index's right child.
	/// </summary>
	/// <param name="index">The parent index.</param>
	/// <returns>The right child index.</returns>
	private static Int32 RightChildIndex(Int32 index)
	{
		return (index << 1) + 1;
	}

	/// <summary>
	/// Gets the index of a given index's parent.
	/// </summary>
	/// <param name="index">The child index.</param>
	/// <returns>The parent index.</returns>
	private static Int32 ParentIndex(Int32 index)
	{
		return index >> 1;
	}
}

/// <inheritdoc />
/// <summary>
/// Thrown when an operation expects elements to be in the queue and the queue is empty.
/// </summary>
public class QueueEmptyException : Exception { }