using UnityEngine;

public class CameraController : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetMouseButton(0))
		{
			transform.Rotate(Vector3.up, Input.GetAxis("Mouse X"));
		}
	}
}
