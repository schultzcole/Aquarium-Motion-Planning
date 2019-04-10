using UnityEngine;
using UnityEngine.UI;

public class FPSMonitor : MonoBehaviour
{
	[SerializeField] private Text txtField;

	private void Start()
	{
		Application.targetFrameRate = 120;
        Time.timeScale = 0;
	}
	
	private void Update ()
	{
		var fps = 1 / Time.unscaledDeltaTime;

		txtField.text = $"FPS: {fps:F2}\nFrametime: {Time.unscaledDeltaTime*1000:N0}ms";

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
	}
}
