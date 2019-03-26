using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PRMGenerator : MonoBehaviour {

	// Start Location. Set in the Unity Editor.
	[Header("Refs")]
	[SerializeField] private Transform StartLoc;
	
	// End Location. Set in the Unity Editor.
	[SerializeField] private Transform EndLoc;
	
	// Agent Prefab. Set in the Unity Editor.
	[SerializeField] private GameObject AgentPrefab;
	private float _agentRadius;

	// Simulation bounds. Set in Unity Editor.
	[Header("Bounds")]
	[SerializeField] private float Left;
	[SerializeField] private float Right;
	[SerializeField] private float Top;
	[SerializeField] private float Bottom;

	// Number of points to add to the roadmap. Set in Unity Editor.
	[Header("PRM Config")]
	[SerializeField] private int NumPoints = 50;
	
	// Max connection distance. Set in Unity Editor
	[SerializeField] private int MaxPRMConnectionDistance = 10;

	private Boolean _drawLines = true;
	
	private List<Vector2> _prmPoints = new List<Vector2>();
	private Single[,] _prmEdges;

	private Collider[] _obstacles;
	
	private void Start ()
	{
		// Cache array of obstacles.
		_obstacles = (from obs in GameObject.FindGameObjectsWithTag("Obstacles") select obs.GetComponent<Collider>()).ToArray();
		_agentRadius = AgentPrefab.transform.localScale.x;
		
		// Spawn PRM points
		_prmPoints.Add(StartLoc.position);
		for (var i = 0; i < NumPoints; i++)
		{
			var x = Random.Range(Left, Right);
			var y = Random.Range(Bottom, Top);

			Vector3 loc = new Vector2(x, y);

			if (_obstacles.All(obs =>
				(obs.transform.position - loc).magnitude > obs.transform.localScale.x + _agentRadius))
			{
				_prmPoints.Add(loc);
			}
		}
		_prmPoints.Add(EndLoc.position);

		var len = _prmPoints.Count;
		_prmEdges = new Single[len,len];
		for (var i = 0; i < len; i++)
		{
			for (var j = i + 1; j < len; j++)
			{
				_prmEdges[i, j] = Single.NegativeInfinity;
				
				Vector2 dir = _prmPoints[j] - _prmPoints[i];
				var dist = dir.magnitude;
				// Ignore pairs that are too far away
				if (dist > MaxPRMConnectionDistance) continue;
				
				// Ignore pairs with obstacles between them.
				RaycastHit hit;
				if (Physics.SphereCast(_prmPoints[i], _agentRadius, dir, out hit, dist,
					LayerMask.GetMask("Obstacles")))
				{
					continue;
				}
				
				_prmEdges[i, j] = _prmEdges[j, i] = dist;
			}
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			_drawLines = !_drawLines;
		}
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;
		
		// Draw Points
		Gizmos.color = Color.black;
		foreach (Vector2 point in _prmPoints)
		{
			Gizmos.DrawCube(point, Vector3.one * .1f);
		}

		// Draw Lines
		if (_drawLines)
		{
			Gizmos.color = new Color(0, 0, 0, .2f);
			for (var i = 0; i < _prmEdges.GetLength(0); i++)
			{
				for (var j = i + 1; j < _prmEdges.GetLength(1); j++)
				{
					if (_prmEdges[i, j] > 0)
					{
						Gizmos.DrawLine(_prmPoints[i], _prmPoints[j]);
					}
				}
			}
		}
	}
}
