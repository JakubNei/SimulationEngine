using UnityEngine;

public interface ISimpleDraw
{
	Color DefaultColor { get; set; }

	/// <summary>
	/// Draw a line from start to end with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the line is rendered 1 frame.
	/// </summary>
	/// <param name="worldPositionStart">Point in world space where the line should start.</param>
	/// <param name="worldPositionEnd">Point in world space where the line should end.</param>
	/// <param name="color">Color of the line.</param>
	/// <param name="duration">How long the line should be visible for.</param>
	/// <param name="depthTest">Should the line be obscured by objects closer to the camera ?</param>
	void Line(Vector3 worldPositionStart, Vector3 worldPositionEnd, Color? color = null, float duration = 0, bool depthTest = false);
	void Text(Vector3 worldPosition, string text, Color? color = null, float duration = 0);
}
