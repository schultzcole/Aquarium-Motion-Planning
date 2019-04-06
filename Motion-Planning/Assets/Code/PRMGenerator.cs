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
	[SerializeField] private int _numPoints = 50;
	[SerializeField] private int _numPointCandidates = 5;
	
	// Max connection distance. Set in Unity Editor
	[SerializeField] private float _maxPRMConnectionDistance = 10;

	// Whether edges should be drawn.
	private Boolean _drawLines = true;
	
	private List<Vector3> _prmPoints = new List<Vector3>();
	private Single[,] _prmEdges;
	private int _numEdges;
	
	private TreeNode _finalPathRoot;
	private float _finalPathMaxDepth;

	private void Start ()
	{
        Random.InitState(1);

		_agentGO = Instantiate(_agentPrefab);
		_agent = _agentGO.GetComponent<Agent>();
		_agentGO.transform.position = _startLoc.position;
		_agentRadius = _agentGO.transform.localScale.x / 2;

		_safeLeft = _left + _agentRadius;
		_safeRight = _right - _agentRadius;
		_safeTop = _top - _agentRadius;
		_safeBottom = _bottom + _agentRadius;

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
	///
	/// Valid points are spawned with approximately Poisson-disc sampling, and do not overlap with obstacles.
	/// </summary>
	private void SpawnPRMPoints()
	{
		_prmPoints.Add(_endLoc.position);
		for (var i = 0; i < _numPoints; i++)
		{
			Vector3 bestCandidate = Vector3.zero;
			float bestCandidateDist = 0;
			
			// Mitchell's Best Candidate approximation for Poisson-disc sampling
			for (int j = 0; j < _numPointCandidates; j++)
			{
				float x = Random.Range(_safeLeft, _safeRight);
				float y = Random.Range(_safeBottom, _safeTop);

				var locCandidate = new Vector3(x, y);
				var distToNearestExistingPoint = float.PositiveInfinity;
				foreach (Vector3 existingPoint in _prmPoints)
				{
					if ((locCandidate - existingPoint).sqrMagnitude < distToNearestExistingPoint)
					{
						distToNearestExistingPoint = (locCandidate - existingPoint).sqrMagnitude;
					}
				}

				if (distToNearestExistingPoint > bestCandidateDist)
				{
					bestCandidateDist = distToNearestExistingPoint;
					bestCandidate = locCandidate;
				}
			}

			if (bestCandidate.x < _safeRight  && bestCandidate.x > _safeLeft &&
			    bestCandidate.y > _safeBottom && bestCandidate.y < _safeTop  &&
			    !Physics.CheckSphere(bestCandidate, _agentRadius))
			{
				_prmPoints.Insert(1, bestCandidate);
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
			_finalPathRoot = Pathfinder.FindPaths(_prmPoints.ToArray(), _prmEdges);
			_finalPathMaxDepth = _finalPathRoot.Flatten()
											   .Aggregate(0.0f, (max, next) => next.TotalDist > max ? next.TotalDist : max);
//			_agent.Init(_finalPath);
		}
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;
		
		// Draw Points
		Gizmos.color = Color.black;
		foreach (var point in _prmPoints)
		{
			Gizmos.DrawCube(point + Vector3.forward * -2, Vector3.one * .1f);
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
						Gizmos.DrawLine(_prmPoints[i] + Vector3.forward * -2, _prmPoints[j] + Vector3.forward * -2);
					}
				}
			}
		}
		
		// Draw Path
		if (_finalPathRoot != null)
		{
			foreach (var node in _finalPathRoot.Flatten())
			{
				if (node.Parent != null)
				{
                    Gizmos.color = Color.Lerp(new Color(0, .5f, 0), new Color(.5f, 0, 0), (float)node.TotalDist / _finalPathMaxDepth);
					Gizmos.DrawLine(node.Parent.Value, node.Value);
				}
			}
		}
	}
}
