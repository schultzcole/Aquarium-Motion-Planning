using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PRMGenerator : MonoBehaviour
{
	// End Location. Set in the Unity Editor.
	[SerializeField] private Transform goalLoc;
	
	
	[Header("Agents")]
	// Agent Prefab. Set in the Unity Editor.
	[SerializeField] private GameObject agentPrefab;
	private float _agentRadius;
	[SerializeField] private int agentCount;

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
	private Boolean _drawPaths = true;
	
	private List<Vector3> _prmPoints = new List<Vector3>();
	private OctTreeNode _octTree;
	private Single[,] _prmEdges;
	private int _numEdges;

	private Pathfinder _pathfinder;
	private Task _pathfindTask;
	private CancellationTokenSource _cts = new CancellationTokenSource();
	private Boolean _firstPath = true;
	private int _goalIndex = 0;
	
	// Array of path nodes, indicating the direction to the next node closer to the goal. In the same order as _prmPoints.
	private PathNode[] _finalPaths;
	private float _finalPathMaxDepth;

	private void Start ()
	{
		_agentRadius = agentPrefab.transform.localScale.x;

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
		
		sw.Restart();
		ConnectPRMEdges();
		sw.Stop();
		Debug.Log("Connecting " + _numEdges + " PRM edges took: " + sw.ElapsedMilliseconds +"ms");

		SpawnAgents();
	}

	private void SpawnAgents()
	{
		for (int i = 0; i < agentCount; i++)
		{
			Vector3 agentLoc;
			do
			{
				float x = Random.Range(_safeLeft, _safeRight);
				float y = Random.Range(_safeBottom, _safeTop);
				float z = Random.Range(_safeBack, _safeFront);

				agentLoc = new Vector3(x, y, z);
			} while (Physics.CheckSphere(agentLoc, _agentRadius, LayerMask.GetMask("Obstacles")));

			var agent = Instantiate(agentPrefab);

			agent.transform.position = agentLoc;
			agent.GetComponent<Agent>().Init(this);
		}
	}

	/// <summary>
	/// Populates the _prmPoints list with valid points.
	///
	/// Valid points are spawned with approximately Poisson-disc sampling, and do not overlap with obstacles.
	/// </summary>
	private void SpawnPRMPoints()
	{
		_prmPoints.Add(goalLoc.position);
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

	/// <summary>
	/// Returns a direction which indicates the direction an agent should travel from the given point to get to the goal.
	/// </summary>
	/// <param name="point">The point at which to query</param>
	/// <returns>A vector indicating the direction to travel to reach the goal from the given point, or null if no path exists.</returns>
	public Vector3? QueryGradientField(Vector3 point)
	{
		if (_finalPaths == null) return null;

		if (!Physics.Linecast(point, _prmPoints[_goalIndex], LayerMask.GetMask("Obstacles")))
		{
			return Vector3.ClampMagnitude(_prmPoints[_goalIndex] - point, 1);
		}

		int numToSample = 3;
		float sampleRadius = 5;

		List<int> inRange = new List<int>();
		for (int i = 0; i < _prmPoints.Count; i++)
		{
			var sqrDist = (_prmPoints[i] - point).sqrMagnitude;
			if (sqrDist < sampleRadius)
			{
				inRange.Add(i);
			}
		}

		int[] nearestIndex = (from _ in Enumerable.Range(0, numToSample) select -1).ToArray();
		float[] nearestDist = (from _ in Enumerable.Range(0, numToSample) select float.PositiveInfinity).ToArray();
		foreach (var i in inRange)
		{
			for (int j = 0; j < numToSample; j++)
			{
				var sqrDist = (point - _prmPoints[i]).sqrMagnitude;
				if (sqrDist < nearestDist[j])
				{
					for (int k = numToSample - 1; k > j; k--)
					{
						nearestDist[k] = nearestDist[k - 1];
						nearestIndex[k] = nearestIndex[k - 1];
					}

					nearestDist[j] = sqrDist;
					nearestIndex[j] = i;
				}
			}
		}

		var totalDir = nearestIndex
			.Where(cand => cand != -1 && !Physics.Linecast(point, _prmPoints[cand], LayerMask.GetMask("Obstacles")))
			.Aggregate(new Tuple<Vector3, int>(Vector3.zero, 0),
				(acc, next) =>
				{
					var dir = acc.Item1 + _finalPaths[next].Direction;
					var count = acc.Item2 + 1;
					return new Tuple<Vector3, int>(dir, count);
				});

		var avgDir = totalDir.Item1/totalDir.Item2;

		return avgDir.normalized;
	}
	
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (_drawPRM == _drawPaths) _drawPRM = !_drawPRM;
			else _drawPaths = !_drawPaths;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			_finalPaths = null;
			if (_pathfindTask != null && !_pathfindTask.IsCompleted)
			{
				_cts.Cancel();
				_pathfindTask.Wait();
				_cts.Dispose();
				_cts = new CancellationTokenSource();
			}

			if (!_firstPath)
			{
				int rnd;
				do
				{
					rnd = Random.Range(0, _prmPoints.Count - 1);
				} while (rnd == _goalIndex);

				_goalIndex = rnd;
			}
			
			_pathfinder = new Pathfinder();
			_pathfindTask = new Task(() => _pathfinder.TryFindPaths(_prmPoints.ToArray(), _prmEdges, _goalIndex, _cts.Token));
			_pathfindTask.Start();

			_firstPath = false;
		}

		if (_pathfindTask != null && _pathfindTask.IsCompleted && _pathfinder.Error == null)
		{
			goalLoc.position = _prmPoints[_goalIndex];
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
			foreach (Vector3 point in _prmPoints)
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
		if (_finalPaths != null && _drawPaths)
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
