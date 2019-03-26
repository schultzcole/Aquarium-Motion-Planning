using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PRMGenerator : MonoBehaviour {

	// Start Location. Set in the Unity Editor.
	[Header("Refs")]
	[SerializeField] private Transform _startLoc;
	
	// End Location. Set in the Unity Editor.
	[SerializeField] private Transform _endLoc;
	
	// Agent Prefab. Set in the Unity Editor.
	[SerializeField] private GameObject _agentPrefab;
	private GameObject _agentGO;
	private float _agentRadius;

	// Simulation bounds. Set in Unity Editor.
	[Header("Bounds")]
	[SerializeField] private float _left;
	[SerializeField] private float _right;
	[SerializeField] private float _top;
	[SerializeField] private float _bottom;

	// Number of points to add to the roadmap. Set in Unity Editor.
	[Header("PRM Config")]
	[SerializeField] private int _numPointAttempts = 50;
	
	// Max connection distance. Set in Unity Editor
	[SerializeField] private float _maxPRMConnectionDistance = 10;
	[SerializeField] private float _minPRMPointDistance = .05f;

	private Boolean _drawLines = true;
	
	private List<Vector3> _prmPoints = new List<Vector3>();
	private Single[,] _prmEdges;

	private Collider[] _obstacles;
	
	private void Start ()
	{
		// Cache array of obstacles.
		_obstacles = (from obs in GameObject.FindGameObjectsWithTag("Obstacles") select obs.GetComponent<Collider>()).ToArray();

		_agentGO = Instantiate(_agentPrefab);
		_agentGO.transform.position = _startLoc.position;
		_agentRadius = _agentGO.transform.localScale.x / 2;
		
		SpawnPRMPoints();
		Debug.Log(_prmPoints.Count);
		ConnectPRMEdges();
	}


	/// <summary>
	/// Populates the _prmPoints list with valid points.
	/// </summary>
	private void SpawnPRMPoints()
	{
		_prmPoints.Add(_startLoc.position);
		Collider agentCollider = _agentGO.GetComponent<Collider>();
		for (var i = 0; i < _numPointAttempts; i++)
		{
			float x = Random.Range(_left, _right);
			float y = Random.Range(_bottom, _top);

			Vector3 loc = new Vector3(x, y);

			if (_prmPoints.Any(pt => (pt - loc).sqrMagnitude < _minPRMPointDistance * _minPRMPointDistance))
			{
				continue;
			}

			bool isValid = false;
			int attempts = 0;
			while (!isValid && attempts++ < 5)
			{
				isValid = true;
				foreach (var obs in _obstacles)
				{
					Vector3 dir;
					float dist;
					
					if (Physics.ComputePenetration(obs, obs.transform.position, obs.transform.rotation,
						agentCollider, loc, Quaternion.identity,
						out dir, out dist))
					{
						isValid = false;
						loc += dir * dist;
					}
				}
			}

			if (loc.x < _right && loc.x > _left && loc.y > _bottom && loc.y < _top && isValid)
			{
				_prmPoints.Add(loc);
			}
		}
		_prmPoints.Add(_endLoc.position);
	}

	/// <summary>
	/// Populates the _prmEdges array with edges between points in the PRM.
	/// </summary>
	private void ConnectPRMEdges()
	{
		_agentGO.GetComponent<Collider>().enabled = false;
		int len = _prmPoints.Count;
		_prmEdges = new Single[len, len];
		for (var i = 0; i < len; i++)
		{
			for (var j = i + 1; j < len; j++)
			{
				_prmEdges[i, j] = Single.NegativeInfinity;

				Vector3 dir = _prmPoints[j] - _prmPoints[i];
				float dist = dir.magnitude;
				// Ignore pairs that are too far away
				if (dist > _maxPRMConnectionDistance) continue;

				// Ignore pairs with obstacles between them.
				bool isValid = true;
				for (float k = 0; k < dist; k += _agentRadius / 2)
				{
					isValid &= !Physics.CheckSphere(_prmPoints[i] + dir.normalized * k, _agentRadius);
					if (!isValid) break;
				}

				if (isValid)
				{
					_prmEdges[i, j] = _prmEdges[j, i] = dist;
				}
			}
		}
		
		_agentGO.GetComponent<Collider>().enabled = true;
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
		if (_drawLines && _prmEdges != null)
		{
			Gizmos.color = new Color(0, 0, 0, .2f);
			for (var i = 0; i < _prmEdges.GetLength(0); i++)
			{
				for (var j = i + 1; j < _prmEdges.GetLength(1); j++)
				{
					if (!Single.IsNegativeInfinity(_prmEdges[i, j]))
					{
						Gizmos.DrawLine(_prmPoints[i], _prmPoints[j]);
					}
				}
			}
		}
	}
}
