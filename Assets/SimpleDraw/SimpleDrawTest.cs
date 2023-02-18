using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDrawTest : MonoBehaviour
{

	void Update()
	{
		var p = transform.position;
		var r = transform.rotation;
		var c = Color.red;

		SimpleDraw.Game.Line(p, p + Vector3.right, c);
		SimpleDraw.Game.Text(p, "Line", c);

		p += Vector3.right * 2;

		SimpleDraw.Game.Circle(p, r, c, 1);
		SimpleDraw.Game.Text(p, "Circle", c);
	}
}
