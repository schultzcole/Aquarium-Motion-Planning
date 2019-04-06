using System;
using UnityEngine;
using UnityEngine.UI;

public class FPSMonitor : MonoBehaviour
{
	[SerializeField] private Text txtField;

	private void Start()
	{
		Application.targetFrameRate = 120;
	}
	
	private void Update ()
	{
		var fps = 1 / Time.deltaTime;

		txtField.text = $"FPS: {fps:F2}\nFrametime: {Time.deltaTime*1000:N0}ms";
	}
}
