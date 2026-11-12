using UnityEngine;

public class Tabs : MonoBehaviour
{
	[Header("UI Settings")]
	[Tooltip("Drag the Canvas GameObject you want to toggle here.")]
	public GameObject pauseMenuCanvas;

	private void Start()
	{
		if (pauseMenuCanvas != null)
		{
			pauseMenuCanvas.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			ToggleMenu();
		}
	}

	private void ToggleMenu()
	{
		if (pauseMenuCanvas != null)
		{
			bool activeSelf = pauseMenuCanvas.activeSelf;
			pauseMenuCanvas.SetActive(!activeSelf);
		}
	}
}

