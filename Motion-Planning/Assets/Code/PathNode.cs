using UnityEngine;

public struct PathNode
{
	public Vector3 Position;
	public Vector3 Direction;
	public float TotalPathDist;

	public PathNode(Vector3 position, Vector3 direction, float totalPathDist)
	{
		Position = position;
		Direction = direction;
		TotalPathDist = totalPathDist;
	}
}