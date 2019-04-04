using UnityEngine;

namespace Code
{
	public class Agent : MonoBehaviour
	{
		[SerializeField] private float _speed = 3;

		private Rigidbody rb;

		private Vector2[] _path;
		private bool _pathValid;
		private int _nextNode;

		private float _radius;

		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			_radius = transform.localScale.x / 2;
		}

		public void Init(Vector2[] path)
		{
			_path = path;
			_pathValid = true;
			transform.position = _path[0];
			_nextNode = 1;

		}

		private void Update()
		{
			if (!_pathValid) return;

			for (int nextSeenNode = _nextNode; nextSeenNode < _path.Length; nextSeenNode++)
			{
				if (Physics.CheckCapsule(transform.position, _path[nextSeenNode], _radius,
					LayerMask.GetMask("Obstacles")))
				{
					break;
				}

				_nextNode = nextSeenNode;
			}

			Vector2 toNext = _path[_nextNode] - (Vector2)transform.position;
			float distToNext = toNext.magnitude;

			if (_nextNode == _path.Length - 1)
			{
				if (distToNext < _speed * Time.deltaTime)
				{
					_pathValid = false;
					rb.velocity = Vector3.zero;
					return;
				}
			}
			else if (distToNext < _radius)
			{
				++_nextNode;
			}
			
			rb.velocity = Vector3.RotateTowards(rb.velocity, toNext.normalized * _speed, _speed * Time.deltaTime / _radius, _speed);
		}
	}
}