using UnityEngine;

public static class SimpleDraw
{
	private static SimpleDrawGL simpleDrawGL;
	/// <summary>
	/// Draw in game view.
	/// Also visible in editor view.
	/// </summary>
	public static ISimpleDraw Game
	{
		get
		{
			if (simpleDrawGL == null)
			{
				simpleDrawGL = Camera.main.gameObject.GetComponent<SimpleDrawGL>();
				if (!simpleDrawGL)
					simpleDrawGL = Camera.main.gameObject.AddComponent<SimpleDrawGL>();
				simpleDrawGL.hideFlags = HideFlags.DontSave;
			}
			return simpleDrawGL;
		}
	}

	private static SimpleDrawEditor simpleDrawHandles;
	/// <summary>
	/// Draw only in editor scene view.
	/// </summary>
	public static ISimpleDraw Editor
	{
		get
		{
			if (simpleDrawHandles == null)
			{
				simpleDrawHandles = Camera.main.gameObject.GetComponent<SimpleDrawEditor>();
				if (!simpleDrawHandles)
					simpleDrawHandles = Camera.main.gameObject.AddComponent<SimpleDrawEditor>();
				simpleDrawHandles.hideFlags = HideFlags.DontSave;
			}
			return simpleDrawHandles;
		}
	}
}
