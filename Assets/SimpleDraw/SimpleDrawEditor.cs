using System.Collections.Generic;
using UnityEngine;

public class SimpleDrawEditor : MonoBehaviour, ISimpleDraw
{
	private List<TextData> textData = new();
	private List<TextData> textData_workList = new();

	private struct TextData
	{
		public Vector3 worldPosition;
		public string text;
		public Color color;
		public float duration;

		public TextData(Vector3 start, string text, Color color, float duration)
		{
			this.worldPosition = start;
			this.text = text;
			this.color = color;
			this.duration = duration;
		}
	}


	public Color DefaultColor { get; set; }
	public void Line(Vector3 start, Vector3 end, Color? color = default(Color?), float duration = 0, bool depthTest = false)
	{
		UnityEngine.Debug.DrawLine(start, end, color ?? DefaultColor, duration, depthTest);
	}

	public void Text(Vector3 worldPosition, string text, Color? color = null, float duration = 0)
	{
		textData.Add(new TextData(worldPosition, text, color ?? DefaultColor, duration));
	}

	void Update()
	{
		var deltaTime = Time.deltaTime;
		{
			textData_workList.Clear();
			for (int i = 0; i < textData.Count; i++)
			{
				var item = textData[i];
				var newDuration = item.duration - deltaTime;
				if (item.duration == 0 || newDuration > 0)
				{
					item.duration = newDuration;
					textData_workList.Add(item);
				}
			}
			var temp = textData_workList;
			textData_workList = textData;
			textData = temp;
		}
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		var originalColor = UnityEditor.Handles.color;
		for (int i = 0; i < textData.Count; i++)
		{
			UnityEditor.Handles.color = textData[i].color;
			UnityEditor.Handles.Label(textData[i].worldPosition, textData[i].text);
		}
		UnityEditor.Handles.color = originalColor;
	}
#endif

}