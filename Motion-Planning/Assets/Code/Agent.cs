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

		private void Start()
		{
			rb = GetComponent<Rigidbody>();
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

			Vector2 toNext = _path[_nextNode] - (Vector2)transform.position;
			float distToNext = toNext.magnitude;

			if (distToNext < _speed * Time.deltaTime)
			{
				transform.position = _path[_nextNode];
				
				if (++_nextNode > _path.Length - 1)
				{
					_pathValid = false;
					rb.velocity = Vector3.zero;
					return;
				}
			}
			
			rb.velocity = toNext.normalized * _speed;
		}
	}
}