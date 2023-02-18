using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drawing of lines using Camera.main and GL immediate API.
/// help from: https://forum.unity3d.com/threads/drawline-with-gl.235793/
/// Version: aeroson 2017-07-30 (author yyyy-MM-dd)
/// License: ODC Public Domain Dedication & License 1.0 (PDDL-1.0) https://tldrlegal.com/license/odc-public-domain-dedication-&-license-1.0-(pddl-1.0)
/// </summary>
public class SimpleDrawGL : MonoBehaviour, ISimpleDraw
{
	private struct LineData
	{
		public Vector3 start;
		public Vector3 end;
		public Color color;
		public float duration;

		public LineData(Vector3 start, Vector3 end, Color color, float duration)
		{
			this.start = start;
			this.end = end;
			this.color = color;
			this.duration = duration;
		}
	}

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
	private Material matZOn;
	private Material matZOff;

	// simple optimized list


	private List<LineData> linesZOn = new();
	private List<LineData> linesZOff = new();
	private List<TextData> textData = new();
	private List<LineData> linesZOn_workList = new();
	private List<LineData> linesZOff_workList = new();
	private List<TextData> textData_workList = new();


	public void Awake()
	{
		DefaultColor = Color.white;
		SetMaterial();
	}

	void SetMaterial()
	{
		Shader shaderZOn = Shader.Find("Hidden/SimpleDrawGL/GLlineZOn");
		shaderZOn.hideFlags = HideFlags.HideAndDontSave;
		matZOn = new Material(shaderZOn);
		matZOn.hideFlags = HideFlags.HideAndDontSave;

		Shader shaderZOff = Shader.Find("Hidden/SimpleDrawGL/GLlineZOff");
		shaderZOff.hideFlags = HideFlags.HideAndDontSave;
		matZOff = new Material(shaderZOff);
		matZOff.hideFlags = HideFlags.HideAndDontSave;
	}

	void Update()
	{
		var deltaTime = Time.deltaTime;

		// these update loops could be written with one line using System.Linq
		// e.g.: linesZOn = linesZOn.Where(l=>l.item.duration - deltaTime < 0).ToArray();
		// but Linq ToArray would allocate new array every frame
		// we instead use two arrays (front, back) that we switch over = no unnecessary allocations
		{
			linesZOn_workList.Clear();
			for (int i = 0; i < linesZOn.Count; i++)
			{
				var item = linesZOn[i];
				var newDuration = item.duration - deltaTime;
				if (item.duration == 0 || newDuration > 0)
				{
					item.duration = newDuration;
					linesZOn_workList.Add(item);
				}
			}
			var temp = linesZOn_workList;
			linesZOn_workList = linesZOn;
			linesZOn = temp;
		}

		{
			linesZOff_workList.Clear();
			for (int i = 0; i < linesZOff.Count; i++)
			{
				var item = linesZOff[i];
				var newDuration = item.duration - deltaTime;
				if (item.duration == 0 || newDuration > 0)
				{
					item.duration = newDuration;
					linesZOff_workList.Add(item);
				}
			}
			var temp = linesZOff_workList;
			linesZOff_workList = linesZOff;
			linesZOff = temp;
		}

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

	void OnGUI()
	{
		var originalColor = GUI.color;
		for (int i = 0; i < textData.Count; i++)
		{
			GUI.color = textData[i].color;
			var position = Camera.main.WorldToScreenPoint(textData[i].worldPosition);
			var textSize = GUI.skin.label.CalcSize(new GUIContent(textData[i].text));
			GUI.Label(new Rect(position.x, Screen.height - position.y, textSize.x, textSize.y), textData[i].text);
		}
		GUI.color = originalColor;
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

		if (!Application.isPlaying)
			return;

		originalColor = Gizmos.color;
		for (int i = 0; i < linesZOn.Count; i++)
		{
			Gizmos.color = linesZOn[i].color;
			Gizmos.DrawLine(linesZOn[i].start, linesZOn[i].end);
		}
		for (int i = 0; i < linesZOff.Count; i++)
		{
			Gizmos.color = linesZOff[i].color;
			Gizmos.DrawLine(linesZOff[i].start, linesZOff[i].end);
		}
		Gizmos.color = originalColor;
	}
#endif

	void OnPostRender()
	{
		if (linesZOn.Count > 0)
		{
			matZOn.SetPass(0);
			GL.Begin(GL.LINES);
			for (int i = 0; i < linesZOn.Count; i++)
			{
				GL.Color(linesZOn[i].color);
				GL.Vertex(linesZOn[i].start);
				GL.Vertex(linesZOn[i].end);
			}
			GL.End();
		}

		if (linesZOff.Count > 0)
		{
			matZOff.SetPass(0);
			GL.Begin(GL.LINES);
			for (int i = 0; i < linesZOff.Count; i++)
			{
				GL.Color(linesZOff[i].color);
				GL.Vertex(linesZOff[i].start);
				GL.Vertex(linesZOff[i].end);
			}
			GL.End();
		}
	}

	public void Line(Vector3 start, Vector3 end, Color? color = null, float duration = 0, bool depthTest = false)
	{
		if (start == end)
			return;
		if (depthTest)
			linesZOn.Add(new LineData(start, end, color ?? DefaultColor, duration));
		else
			linesZOff.Add(new LineData(start, end, color ?? DefaultColor, duration));
	}

	public void Text(Vector3 worldPosition, string text, Color? color = null, float duration = 0)
	{
		textData.Add(new TextData(worldPosition, text, color ?? DefaultColor, duration));
	}
}
