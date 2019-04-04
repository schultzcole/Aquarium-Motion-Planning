using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Code;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Object = System.Object;
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
	private Agent _agent;
	private float _agentRadius;

	// Simulation bounds. Set in Unity Editor.
	[Header("Bounds")]
	[SerializeField] private float _left;
	[SerializeField] private float _right;
	[SerializeField] private float _top;
	[SerializeField] private float _bottom;

	// Bounds adjusted for agent radius.
	private float _safeLeft;
	private float _safeRight;
	private float _safeTop;
	private float _safeBottom;

	// Number of points to add to the roadmap. Set in Unity Editor.
	[Header("PRM Config")]
	[SerializeField] private int _numPointAttempts = 50;
	
	// Max connection distance. Set in Unity Editor
	[SerializeField] private float _maxPRMConnectionDistance = 10;
	[SerializeField] private float _minPRMPointDistance = .05f;

	// Whether edges should be drawn.
	private Boolean _drawLines = true;
	
	private List<Vector3> _prmPoints = new List<Vector3>();
	private Single[,] _prmEdges;
	private int _numEdges;

	private Collider[] _obstacles;
	
	private Vector2[] _finalPath;

	private void Start ()
	{
		// Cache array of obstacles.
		_obstacles = (from obs in GameObject.FindGameObjectsWithTag("Obstacles") select obs.GetComponent<Collider>()).ToArray();

		_agentGO = Instantiate(_agentPrefab);
		_agent = _agentGO.GetComponent<Agent>();
		_agentGO.transform.position = _startLoc.position;
		_agentRadius = _agentGO.transform.localScale.x / 2;

		_safeLeft = _left + _agentRadius;
		_safeRight = _right - _agentRadius;
		_safeTop = _top - _agentRadius;
		_safeBottom = _bottom + _agentRadius;
		
//		Random.InitState(1);

		Stopwatch sw = new Stopwatch();
		sw.Start();
		SpawnPRMPoints();
		sw.Stop();
		Debug.Log("Spawning " + _prmPoints.Count + " PRM points took: " + sw.ElapsedMilliseconds +"ms");
		
		sw.Reset();
		sw.Start();
		ConnectPRMEdges();
		sw.Stop();
		
		Debug.Log("Connecting " + _numEdges + " PRM edges took: " + sw.ElapsedMilliseconds +"ms");
	}


	/// <summary>
	/// Populates the _prmPoints list with valid points.
	/// </summary>
	private void SpawnPRMPoints()
	{
		_prmPoints.Add(_startLoc.position);
		_prmPoints.Add(_endLoc.position);
		Collider agentCollider = _agentGO.GetComponent<Collider>();
		for (var i = 0; i < _numPointAttempts; i++)
		{
			float x = Random.Range(_safeLeft, _safeRight);
			float y = Random.Range(_safeBottom, _safeTop);

			Vector3 loc = new Vector3(x, y);
			
			if(_prmPoints.Any(pt => (pt - loc).sqrMagnitude < _minPRMPointDistance * _minPRMPointDistance)) continue;

			bool isValid = false;
			int relocAttempts = 0;
			while (!isValid && relocAttempts++ < 5)
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

			if (loc.x < _safeRight && loc.x > _safeLeft && loc.y > _safeBottom && loc.y < _safeTop && isValid)
			{
				_prmPoints.Insert(1, loc);
			}
		}
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
				_prmEdges[i, j] = _prmEdges[j, i] = Single.NegativeInfinity;

				float dist = Vector3.Distance(_prmPoints[i], _prmPoints[j]);
				
				// Ignore pairs that are too far away
				if (dist > _maxPRMConnectionDistance) continue;

				// Ignore pairs with obstacles between them.
				// A capsule check is the fastest way I've found to do this that is accurate.
				if (Physics.CheckCapsule(_prmPoints[i], _prmPoints[j], _agentRadius)) continue;
				
				_prmEdges[i, j] = _prmEdges[j, i] = dist;
				_numEdges++;
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

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_finalPath = Pathfinder.FindPath((from pt in _prmPoints select (Vector2)pt).ToArray(), _prmEdges);
			_agent.Init(_finalPath);
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
			Gizmos.color = new Color(0, 0, 0, .1f);
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
		
		// Draw Path
		if (_finalPath != null)
		{
			Gizmos.color = Color.green;
			for (int i = 0; i < _finalPath.Length - 1; i++)
			{
				Gizmos.DrawLine(_finalPath[i], _finalPath[i+1]);
			}
		}
	}
}
