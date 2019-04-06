using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Code;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PRMGenerator : MonoBehaviour {

	// Start Location. Set in the Unity Editor.
	[Header("Refs")]
	[SerializeField] private Transform startLoc;
	
	// End Location. Set in the Unity Editor.
	[SerializeField] private Transform endLoc;
	
	// Agent Prefab. Set in the Unity Editor.
	[SerializeField] private GameObject agentPrefab;
	private GameObject _agentGO;
	private Agent _agent;
	private float _agentRadius;

	// Simulation bounds. Set in Unity Editor.
	[Header("Bounds")]
	[SerializeField] private float left;
	[SerializeField] private float right;
	[SerializeField] private float front;
	[SerializeField] private float back;
	[SerializeField] private float top;
	[SerializeField] private float bottom;

	// Bounds adjusted for agent radius.
	private float _safeLeft;
	private float _safeRight;
	private float _safeFront;
	private float _safeBack;
	private float _safeTop;
	private float _safeBottom;

	// Number of points to add to the roadmap. Set in Unity Editor.
	[Header("PRM Config")]
	[SerializeField] private int numPoints = 50;
	[SerializeField] private int numPointCandidates = 5;
	
	// Max connection distance. Set in Unity Editor
	[SerializeField] private float maxPRMConnectionDistance = 10;

	// Whether edges should be drawn.
	private Boolean _drawPRM = true;
	
	private List<Vector3> _prmPoints = new List<Vector3>();
	private Single[,] _prmEdges;
	private int _numEdges;

	private Pathfinder _pathfinder;
	private Task _pathfindTask;
	private PathNode[] _finalPaths;
	private float _finalPathMaxDepth;

	private void Start ()
	{
		_agentGO = Instantiate(agentPrefab);
		_agent = _agentGO.GetComponent<Agent>();
		_agentGO.transform.position = startLoc.position;
		_agentRadius = _agentGO.transform.localScale.x / 2;

		_safeLeft = left + _agentRadius;
		_safeRight = right - _agentRadius;
		_safeFront = front + _agentRadius;
		_safeBack = back - _agentRadius;
		_safeTop = top - _agentRadius;
		_safeBottom = bottom + _agentRadius;

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
		_prmPoints.Add(endLoc.position);
		for (var i = 0; i < numPoints; i++)
		{
			Vector3 bestCandidate = Vector3.zero;
			float bestCandidateDist = 0;
			
			// Mitchell's Best Candidate approximation for Poisson-disc sampling
			for (int j = 0; j < numPointCandidates; j++)
			{
				float x = Random.Range(_safeLeft, _safeRight);
				float y = Random.Range(_safeBottom, _safeTop);
				float z = Random.Range(_safeBack, _safeFront);

				var locCandidate = new Vector3(x, y, z);
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
			    !Physics.CheckSphere(bestCandidate, _agentRadius, LayerMask.GetMask("Obstacles")))
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
		int len = _prmPoints.Count;
		_prmEdges = new Single[len, len];
		for (var i = 0; i < len; i++)
		{
			for (var j = i + 1; j < len; j++)
			{
				_prmEdges[i, j] = _prmEdges[j, i] = Single.NegativeInfinity;

				float dist = Vector3.Distance(_prmPoints[i], _prmPoints[j]);
				
				// Ignore pairs that are too far away
				if (dist > maxPRMConnectionDistance) continue;

				// Ignore pairs with obstacles between them.
				// A capsule check is the fastest way I've found to do this that is accurate.
				if (Physics.CheckCapsule(_prmPoints[i], _prmPoints[j], _agentRadius,
					LayerMask.GetMask("Obstacles"))) continue;
				
				_prmEdges[i, j] = _prmEdges[j, i] = dist;
				_numEdges++;
			}
		}
	}
	
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			_drawPRM = !_drawPRM;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_pathfinder = new Pathfinder();
			_pathfindTask = new Task(() => _pathfinder.TryFindPaths(_prmPoints.ToArray(), _prmEdges));
			_pathfindTask.Start();
		}

		if (_pathfindTask != null && _pathfindTask.IsCompleted && _pathfinder.Error == null)
		{
			_finalPaths = _pathfinder.Results.ToArray();
			_finalPathMaxDepth =
				_finalPaths.Aggregate(0.0f, (max, next) => next.TotalPathDist > max ? next.TotalPathDist : max);


            _pathfinder = null;
            _pathfindTask.Dispose();
            _pathfindTask = null;
        }
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		if (_drawPRM)
		{
			// Draw Points
			Gizmos.color = new Color(0, 0, 0, .5f);
			foreach (var point in _prmPoints)
			{
				Gizmos.DrawSphere(point, .05f);
			}

			// Draw Lines
			Gizmos.color = new Color(0, 0, 0, .05f);
			if (_prmEdges != null)
			{
				var len = _prmPoints.Count;
				for (var i = 0; i < len; i++)
				{
					for (var j = i + 1; j < len; j++)
					{
						if (!Single.IsNegativeInfinity(_prmEdges[i, j]))
						{
							Gizmos.DrawLine(_prmPoints[i], _prmPoints[j]);
						}
					}
				}
			}
		}

		// Draw Path
		if (_finalPaths != null)
		{
			foreach (var node in _finalPaths)
			{
				Gizmos.color = Color.Lerp(new Color(0, .5f, 0, .5f), new Color(.5f, 0, 0, .5f),
					node.TotalPathDist / _finalPathMaxDepth);
				Gizmos.DrawLine(node.Position + node.Direction, node.Position);
			}
		}
	}
}
