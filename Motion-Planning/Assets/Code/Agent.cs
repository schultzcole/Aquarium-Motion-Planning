using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Agent : MonoBehaviour
{
	private static List<Agent> _agents = new List<Agent>();
	// Position of this agent in the static _agents array. Used to provide unique results for the random impulse
	private int _agentId;
	
	// Random time offsets for generating random impulse.
	// Without this, every agent changes random direction at the same time.
	// We have separate offsets for each axis so that random changes in
	// each axis don't necessarily change at the same time.
	private float _randomOffsetX;
	private float _randomOffsetY;
	private float _randomOffsetZ;
	
	private static Collider[] _obstacles;
	
	[SerializeField] private float maxSpeed;
	[SerializeField] private float angularAcceleration;
	[SerializeField] private float maxAcceleration;
	[SerializeField] private float velocityDamping;

	[Header("Boids Config")]
	// Distances are measured in boid radii
	// All boids configuration variables are set in the Unity editor for quick iteration.
	[SerializeField] private float separationDist = 1;
	[SerializeField] private float alignmentDist = 1;
	[SerializeField] private float cohesionDist = 1;
	[SerializeField] private float obstacleDist = 1;
	[SerializeField] private float separationStrength = 1;
	[SerializeField] private float alignmentStrength = 1;
	[SerializeField] private float cohesionStrength = 1;
	[SerializeField] private float obstacleAvoidStrength = 1;
	[SerializeField] private float obstacleSlideStrength = 1;
	[SerializeField] private float centeringStrength = 1;
	[SerializeField] private float oscillationStrength = 1;
	[SerializeField] private float oscillationTimeScale = 1;
	[SerializeField] private float randomStrength = 1;
	[SerializeField] private float randomTimeScale = 1;
	
	private Rigidbody _rb;

	private PRMGenerator _prmGen;

	private float _radius;

	private void Start()
	{
		if (_obstacles == null)
		{
			_obstacles = GameObject.FindGameObjectsWithTag("Obstacles").Select(x => x.GetComponent<Collider>())
				.ToArray();
		}
		
		_agents.Add(this);
		_agentId = _agents.Count;
		
		_randomOffsetX = Random.Range(1.0f, 50.0f);
		_randomOffsetY = Random.Range(1.0f, 50.0f);
		_randomOffsetZ = Random.Range(1.0f, 50.0f);
		
		_rb = GetComponent<Rigidbody>();
		_radius = transform.localScale.x / 2;
	}

	/// <summary>
	/// Initializes the agent with the PRM generator, so the agent can query the PRM for target direction
	/// </summary>
	/// <param name="prmGen">The PRM this agent should query for paths.</param>
	public void Init(PRMGenerator prmGen)
	{
		_prmGen = prmGen;
	}

	private void Update()
	{
		if (Math.Abs(Time.timeScale) < .01) return;
		
		var dir = _prmGen.QueryGradientField(transform.position);
		
		Vector3 target = _rb.velocity * (1-velocityDamping);
		if (dir.HasValue)
		{
			 target = Vector3.RotateTowards(_rb.velocity, dir.Value.normalized * maxSpeed, angularAcceleration,
				maxAcceleration);
		}

		Vector3 newVel = CalcBoidsImpulses(target) + target;
		
		_rb.velocity = Vector3.ClampMagnitude(newVel, maxSpeed);
		
		transform.LookAt(transform.position + _rb.velocity.normalized, Vector3.up);
	}

	/// <summary>
	/// Calculates the boid impulses on this agent, given a target velocity
	/// </summary>
	/// <param name="target">Target velocity</param>
	/// <returns>accumulated </returns>
	private Vector3 CalcBoidsImpulses(Vector3 target)
	{
		Vector3 separation = Vector3.zero;
		Vector3 alignment = Vector3.zero;
		int alignNeighbors = 0;
		Vector3 cohesion = Vector3.zero;
		int cohesionNeighbors = 0;
		Vector3 obstacleAvoid = Vector3.zero;
		Vector3 obstacleSlide = Vector3.zero;
		Vector3 oscillation = transform.right * Mathf.Sin(Time.time * oscillationTimeScale + _randomOffsetX);

		// The intent of using GetHashCode on the agent ID is to make it so that agents with similar IDs do not
		// necessarily have similar random vectors.
		Vector3 random = Vector3.ClampMagnitude(
			new Vector3(
				Mathf.PerlinNoise(Time.time * randomTimeScale + _randomOffsetX, _agentId.GetHashCode()) * 2 - 1,
				Mathf.PerlinNoise(Time.time * randomTimeScale + _randomOffsetY, (_agentId + 7).GetHashCode()) * 2 - 1,
				Mathf.PerlinNoise(Time.time * randomTimeScale + _randomOffsetZ, (_agentId - 11).GetHashCode()) * 2 - 1)
			* maxSpeed, maxSpeed
		);

		var myPos = transform.position;

		foreach (var other in _agents)
		{
			if (other == this) continue;

			var toOther = other.transform.position - myPos;
			var dist = toOther.magnitude;

			if (dist < separationDist * _radius && dist > 0)
			{
				separation += toOther * -1 / dist;
			}

			if (dist < alignmentDist * _radius)
			{
				alignment += other._rb.velocity;
				alignNeighbors++;
			}

			if (dist < cohesionDist * _radius)
			{
				cohesion += other.transform.position;
				cohesionNeighbors++;
			}
		}

		if (alignNeighbors > 0)
		{
			alignment /= alignNeighbors;
		}

		if (cohesionNeighbors > 0)
		{
			cohesion /= cohesionNeighbors;
		}

		if (_obstacles != null)
		{
			foreach (var obs in _obstacles)
			{
				if (obs is MeshCollider && !(obs as MeshCollider).convex) continue;

				var obsTransform = obs.transform;
				var closestPoint = Physics.ClosestPoint(myPos, obs, obsTransform.position,
					obsTransform.rotation);

				var toPoint = closestPoint - myPos;
				var toPointDist = toPoint.magnitude;
				if (toPointDist < obstacleDist * _radius)
				{
					if (toPointDist > 0)
					{
						obstacleAvoid += toPoint * -1 / toPointDist;
					}

					obstacleSlide += (target - toPoint * Vector3.Dot(target, toPoint.normalized)) *
					                 obstacleSlideStrength;
				}
			}
		}

		var toCenter = new Vector3(0, 10, 0) - myPos;

		var newVel = separation * separationStrength +
		             alignment * alignmentStrength +
		             cohesion * cohesionStrength +
		             obstacleAvoid * obstacleAvoidStrength +
		             obstacleSlide +
		             toCenter * centeringStrength +
		             oscillation * oscillationStrength +
		             random * randomStrength;
		
		return newVel;
	}
}
