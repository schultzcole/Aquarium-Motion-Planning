using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class PRMGenerator : MonoBehaviour {

	// Start Location. Set in the Unity Editor.
	public Transform StartLoc;
	
	// End Location. Set in the Unity Editor.
	public Transform EndLoc;
	
	// Agent Prefab. Set in the Unity Editor.
	public GameObject AgentPrefab;
	private float _agentRadius;

	// Simulation bounds. Set in Unity Editor.
	public float Left;
	public float Right;
	public float Top;
	public float Bottom;

	// Number of points to add to the roadmap. Set in Unity Editor.
	public int NumPoints;

	public Boolean _drawLines = true;
	
	private List<Vector2> _prmPoints = new List<Vector2>();
	private Boolean[,] _prmEdges;

	private Collider[] _obstacles;
	
	private void Start ()
	{
		_obstacles = (from obs in GameObject.FindGameObjectsWithTag("Obstacles") select obs.GetComponent<Collider>()).ToArray();
		_agentRadius = AgentPrefab.transform.localScale.x;
		
		// Spawn PRM points
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
		
		_prmPoints.Add(StartLoc.position);
		_prmPoints.Add(EndLoc.position);

		var len = _prmPoints.Count;
		_prmEdges = new Boolean[len,len];
		for (var i = 0; i < len; i++)
		{
			for (var j = i + 1; j < len; j++)
			{
				Vector2 dir = _prmPoints[j] - _prmPoints[i];
				RaycastHit hit;
				if (!Physics.SphereCast(_prmPoints[i], _agentRadius, dir, out hit, dir.magnitude,
					LayerMask.GetMask("Obstacles")))
				{
					_prmEdges[i, j] = _prmEdges[j, i] = true;
				}
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
		
		Gizmos.color = Color.black;
		foreach (Vector2 point in _prmPoints)
		{
			Gizmos.DrawCube(point, Vector3.one * .1f);
		}

		if (_drawLines)
		{
			Gizmos.color = new Color(0, 0, 0, .2f);
			for (var i = 0; i < _prmEdges.GetLength(0); i++)
			{
				for (var j = i + 1; j < _prmEdges.GetLength(1); j++)
				{
					if (_prmEdges[i, j])
					{
						Gizmos.DrawLine(_prmPoints[i], _prmPoints[j]);
					}
				}
			}
		}
	}
}
