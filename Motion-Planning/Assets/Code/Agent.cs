using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace Code
{
	public class Agent : MonoBehaviour
	{
		private static List<Agent> _agents = new List<Agent>();
		private static Collider[] _obstacles;
		
		[SerializeField] private float maxSpeed;
		[SerializeField] private float angularAcceleration;
		[SerializeField] private float maxAcceleration;
		[SerializeField] private float velocityDamping;

		[Header("Boids Config")]
		// Distances are measured in boid radii
		[SerializeField] private float separationDist = 1;
		[SerializeField] private float alignmentDist = 1;
		[SerializeField] private float cohesionDist = 1;
		[SerializeField] private float obstacleDist;
		[SerializeField] private float separationStrength = 1;
		[SerializeField] private float alignmentStrength = 1;
		[SerializeField] private float cohesionStrength = 1;
		[SerializeField] private float obstacleAvoidStrength = 1;
		[SerializeField] private float obstacleSlideStrength = 1;
		[SerializeField] private float centeringStrength = 1;
		
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
			_rb = GetComponent<Rigidbody>();
			_radius = transform.localScale.x / 2;
		}

		public void Init(PRMGenerator prmGen)
		{
			_prmGen = prmGen;
		}

		private void Update()
		{
			var dir = _prmGen.QueryGradientField(transform.position);
			
			Vector3 target = _rb.velocity * (1-velocityDamping);
			if (dir.HasValue)
			{
				 target = Vector3.RotateTowards(_rb.velocity, dir.Value.normalized * maxSpeed, angularAcceleration,
					maxAcceleration);
			}

			Vector3 separation = Vector3.zero;
			Vector3 alignment = Vector3.zero;
			int alignNeighbors = 0;
			Vector3 cohesion = Vector3.zero;
			int cohesionNeighbors = 0;
			Vector3 obstacleAvoid = Vector3.zero;
			Vector3 obstacleSlide = Vector3.zero;

			foreach (var other in _agents)
			{
				if (other == this) continue;

				var toOther = other.transform.position - transform.position;
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
					var obsTransform = obs.transform;
					var myPos = transform.position;
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
						obstacleSlide += (target - toPoint * Vector3.Dot(target, toPoint.normalized)) * obstacleSlideStrength;
					}
				}
			}

			var toCenter = new Vector3(0, 10, 0) - transform.position;

			var newVel = target + separation * separationStrength + alignment * alignmentStrength +
			             cohesion * cohesionStrength + obstacleAvoid * obstacleAvoidStrength +
			             obstacleSlide + toCenter * centeringStrength;
			
			_rb.velocity = Vector3.ClampMagnitude(newVel, maxSpeed);
		}
	}
}