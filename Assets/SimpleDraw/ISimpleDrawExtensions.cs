
using UnityEngine;

public static class ISimpleDrawExtensions
{
	/// <summary>
	/// Draw a line from start to start + dir with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the ray is rendered 1 frame.
	/// </summary>
	/// <param name="start">Point in world space where the ray should start.</param>
	/// <param name="dir">Direction and length of the ray.</param>
	/// <param name="color">Color of the ray.</param>
	/// <param name="duration">How long the ray should be visible for.</param>
	/// <param name="depthTest">Should the ray be obscured by objects closer to the camera ?</param>
	public static void Ray(this ISimpleDraw self, Vector3 start, Vector3 dir, Color? color = null, float duration = 0, bool depthTest = false)
	{
		if (dir == Vector3.zero)
			return;
		self.Line(start, start + dir, color, duration, depthTest);
	}

	/// <summary>
	/// Draw an arrow from start to end with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the arrow is rendered 1 frame.
	/// </summary>
	/// <param name="start">Point in world space where the arrow should start.</param>
	/// <param name="end">Point in world space where the arrow should end.</param>
	/// <param name="arrowHeadLength">Length of the 2 lines of the head.</param>
	/// <param name="arrowHeadAngle">Angle between the main line and each of the 2 smaller lines of the head.</param>
	/// <param name="color">Color of the arrow.</param>
	/// <param name="duration">How long the arrow should be visible for.</param>
	/// <param name="depthTest">Should the arrow be obscured by objects closer to the camera ?</param>
	public static void LineArrow(this ISimpleDraw self, Vector3 start, Vector3 end, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20, Color? color = null, float duration = 0, bool depthTest = false)
	{
		self.Arrow(start, end - start, arrowHeadLength, arrowHeadAngle, color, duration, depthTest);
	}

	/// <summary>
	/// Draw an arrow from start to start + dir with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the arrow is rendered 1 frame.
	/// </summary>
	/// <param name="start">Point in world space where the arrow should start.</param>
	/// <param name="dir">Direction and length of the arrow.</param>
	/// <param name="arrowHeadLength">Length of the 2 lines of the head.</param>
	/// <param name="arrowHeadAngle">Angle between the main line and each of the 2 smaller lines of the head.</param>
	/// <param name="color">Color of the arrow.</param>
	/// <param name="duration">How long the arrow should be visible for.</param>
	/// <param name="depthTest">Should the arrow be obscured by objects closer to the camera ?</param>
	public static void Arrow(this ISimpleDraw self, Vector3 start, Vector3 dir, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20, Color? color = null, float duration = 0, bool depthTest = false)
	{
		if (dir == Vector3.zero)
			return;
		self.Ray(start, dir, color, duration, depthTest);
		var right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
		var left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
		self.Ray(start + dir, right * arrowHeadLength, color, duration, depthTest);
		self.Ray(start + dir, left * arrowHeadLength, color, duration, depthTest);
	}

	/// <summary>
	/// Draw a square with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the square is renderer 1 frame.
	/// </summary>
	/// <param name="pos">Center of the square in world space.</param>
	/// <param name="rot">Rotation of the square in world space.</param>
	/// <param name="scale">Size of the square.</param>
	/// <param name="color">Color of the square.</param>
	/// <param name="duration">How long the square should be visible for.</param>
	/// <param name="depthTest">Should the square be obscured by objects closer to the camera ?</param>
	public static void Square(this ISimpleDraw self, Vector3 pos, Quaternion? rot = null, Vector3? scale = null, Color? color = null, float duration = 0, bool depthTest = false)
	{
		self.Square(Matrix4x4.TRS(pos, rot ?? Quaternion.identity, scale ?? Vector3.one), color, duration, depthTest);
	}

	public static void Square(this ISimpleDraw self, Transform transform, Vector3? scale = null, Color? color = null, float duration = 0, bool depthTest = false)
	{
		scale = scale ?? transform.lossyScale;
		self.Square(transform.position, transform.rotation, scale, color, duration, depthTest);
	}

	/// <summary>
	/// Draw a square with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the square is renderer 1 frame.
	/// </summary>
	/// <param name="matrix">Transformation matrix which represent the square transform.</param>
	/// <param name="color">Color of the square.</param>
	/// <param name="duration">How long the square should be visible for.</param>
	/// <param name="depthTest">Should the square be obscured by objects closer to the camera ?</param>
	public static void Square(this ISimpleDraw self, Matrix4x4 matrix, Color? color = null, float duration = 0, bool depthTest = false)
	{
		Vector3
				p_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, 0, .5f)),
				p_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, 0, -.5f)),
				p_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, 0, -.5f)),
				p_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, 0, .5f));

		self.Line(p_1, p_2, color, duration, depthTest);
		self.Line(p_2, p_3, color, duration, depthTest);
		self.Line(p_3, p_4, color, duration, depthTest);
		self.Line(p_4, p_1, color, duration, depthTest);
	}

	/// <summary>
	/// Draw a cube with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the square is renderer 1 frame.
	/// </summary>
	/// <param name="pos">Center of the cube in world space.</param>
	/// <param name="rot">Rotation of the cube in world space.</param>
	/// <param name="scale">Size of the cube.</param>
	/// <param name="color">Color of the cube.</param>
	/// <param name="duration">How long the cube should be visible for.</param>
	/// <param name="depthTest">Should the cube be obscured by objects closer to the camera ?</param>
	public static void Cube(this ISimpleDraw self, Vector3 pos, Quaternion? rot = null, Vector3? scale = null, Color? color = null, float duration = 0, bool depthTest = false)
	{
		self.Cube(Matrix4x4.TRS(pos, rot ?? Quaternion.identity, scale ?? Vector3.one), color, duration, depthTest);
	}
	/// <summary>
	/// Draw a cube with color for a duration of time and with or without depth testing.
	/// If duration is 0 then the square is renderer 1 frame.
	/// </summary>
	/// <param name="matrix">Transformation matrix which represent the cube transform.</param>
	/// <param name="color">Color of the cube.</param>
	/// <param name="duration">How long the cube should be visible for.</param>
	/// <param name="depthTest">Should the cube be obscured by objects closer to the camera ?</param>
	public static void Cube(this ISimpleDraw self, Matrix4x4 matrix, Color? color = null, float duration = 0, bool depthTest = false)
	{
		Vector3
				down_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, -.5f, .5f)),
				down_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, -.5f, -.5f)),
				down_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, -.5f, -.5f)),
				down_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, -.5f, .5f)),
				up_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, .5f, .5f)),
				up_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, .5f, -.5f)),
				up_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, .5f, -.5f)),
				up_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, .5f, .5f));

		self.Line(down_1, down_2, color, duration, depthTest);
		self.Line(down_2, down_3, color, duration, depthTest);
		self.Line(down_3, down_4, color, duration, depthTest);
		self.Line(down_4, down_1, color, duration, depthTest);

		self.Line(down_1, up_1, color, duration, depthTest);
		self.Line(down_2, up_2, color, duration, depthTest);
		self.Line(down_3, up_3, color, duration, depthTest);
		self.Line(down_4, up_4, color, duration, depthTest);

		self.Line(up_1, up_2, color, duration, depthTest);
		self.Line(up_2, up_3, color, duration, depthTest);
		self.Line(up_3, up_4, color, duration, depthTest);
		self.Line(up_4, up_1, color, duration, depthTest);
	}

	public static void Circle(this ISimpleDraw self, Vector3 position, Quaternion? rotation = null, Color? color = null, float radius = 1.0f, float duration = 0, bool depthTest = false, uint? detailPointsCount = null)
	{
		var rot = rotation ?? Quaternion.identity;
		var steps = detailPointsCount ?? 90;
		for (var i = 0; i < steps; i++)
		{
			var r1 = i / (float)steps;
			var previous = position + rot * (new Vector3(Mathf.Cos(r1 * Mathf.PI * 2), Mathf.Sin(r1 * Mathf.PI * 2), 0) * radius);
			var r2 = (i + 1) / (float)steps;
			var next = position + rot * (new Vector3(Mathf.Cos(r2 * Mathf.PI * 2), Mathf.Sin(r2 * Mathf.PI * 2), 0) * radius);
			self.Line(previous, next, color, duration, depthTest);
		}
	}

	public static void Point(this ISimpleDraw self, Vector3 position, float scale = 1.0f, Color? color = null, float duration = 0, bool depthTest = false)
	{
		self.Ray(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale, color, duration, depthTest);
		self.Ray(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale, color, duration, depthTest);
		self.Ray(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale, color, duration, depthTest);
	}

	public static void Bounds(this ISimpleDraw self, Bounds bounds, Color? color = null, float duration = 0, bool depthTest = false)
	{
		var center = bounds.center;

		float x = bounds.extents.x;
		float y = bounds.extents.y;
		float z = bounds.extents.z;

		var ruf = center + new Vector3(x, y, z);
		var rub = center + new Vector3(x, y, -z);
		var luf = center + new Vector3(-x, y, z);
		var lub = center + new Vector3(-x, y, -z);

		var rdf = center + new Vector3(x, -y, z);
		var rdb = center + new Vector3(x, -y, -z);
		var lfd = center + new Vector3(-x, -y, z);
		var lbd = center + new Vector3(-x, -y, -z);

		self.Line(ruf, luf, color, duration, depthTest);
		self.Line(ruf, rub, color, duration, depthTest);
		self.Line(luf, lub, color, duration, depthTest);
		self.Line(rub, lub, color, duration, depthTest);

		self.Line(ruf, rdf, color, duration, depthTest);
		self.Line(rub, rdb, color, duration, depthTest);
		self.Line(luf, lfd, color, duration, depthTest);
		self.Line(lub, lbd, color, duration, depthTest);

		self.Line(rdf, lfd, color, duration, depthTest);
		self.Line(rdf, rdb, color, duration, depthTest);
		self.Line(lfd, lbd, color, duration, depthTest);
		self.Line(lbd, rdb, color, duration, depthTest);
	}



	public static void LocalCube(this ISimpleDraw self, Transform transform, Vector3 size, Color? color = null, Vector3 center = default(Vector3), float duration = 0, bool depthTest = false)
	{
		var lbb = transform.TransformPoint(center + ((-size) * 0.5f));
		var rbb = transform.TransformPoint(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

		var lbf = transform.TransformPoint(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
		var rbf = transform.TransformPoint(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

		var lub = transform.TransformPoint(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
		var rub = transform.TransformPoint(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

		var luf = transform.TransformPoint(center + ((size) * 0.5f));
		var ruf = transform.TransformPoint(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

		self.Line(lbb, rbb, color, duration, depthTest);
		self.Line(rbb, lbf, color, duration, depthTest);
		self.Line(lbf, rbf, color, duration, depthTest);
		self.Line(rbf, lbb, color, duration, depthTest);

		self.Line(lub, rub, color, duration, depthTest);
		self.Line(rub, luf, color, duration, depthTest);
		self.Line(luf, ruf, color, duration, depthTest);
		self.Line(ruf, lub, color, duration, depthTest);

		self.Line(lbb, lub, color, duration, depthTest);
		self.Line(rbb, rub, color, duration, depthTest);
		self.Line(lbf, luf, color, duration, depthTest);
		self.Line(rbf, ruf, color, duration, depthTest);
	}


	public static void LocalCube(this ISimpleDraw self, Matrix4x4 space, Vector3 size, Color? color = null, Vector3 center = default(Vector3), float duration = 0, bool depthTest = false)
	{
		var lbb = space.MultiplyPoint3x4(center + ((-size) * 0.5f));
		var rbb = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

		var lbf = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
		var rbf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

		var lub = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
		var rub = space.MultiplyPoint3x4(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

		var luf = space.MultiplyPoint3x4(center + ((size) * 0.5f));
		var ruf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

		self.Line(lbb, rbb, color, duration, depthTest);
		self.Line(rbb, lbf, color, duration, depthTest);
		self.Line(lbf, rbf, color, duration, depthTest);
		self.Line(rbf, lbb, color, duration, depthTest);

		self.Line(lub, rub, color, duration, depthTest);
		self.Line(rub, luf, color, duration, depthTest);
		self.Line(luf, ruf, color, duration, depthTest);
		self.Line(ruf, lub, color, duration, depthTest);

		self.Line(lbb, lub, color, duration, depthTest);
		self.Line(rbb, rub, color, duration, depthTest);
		self.Line(lbf, luf, color, duration, depthTest);
		self.Line(rbf, ruf, color, duration, depthTest);
	}


	public static void Sphere(this ISimpleDraw self, Vector3 position, Color? color = null, float radius = 1.0f, float duration = 0, bool depthTest = false)
	{
		float angle = 10.0f;

		var x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
		var y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
		var z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

		Vector3 new_x;
		Vector3 new_y;
		Vector3 new_z;

		for (int i = 1; i < 37; i++)
		{
			new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
			new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y, position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
			new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

			self.Line(x, new_x, color, duration, depthTest);
			self.Line(y, new_y, color, duration, depthTest);
			self.Line(z, new_z, color, duration, depthTest);

			x = new_x;
			y = new_y;
			z = new_z;
		}
	}

	public static void Cylinder(this ISimpleDraw self, Vector3 start, Vector3 end, Color? color = null, float radius = 1, float duration = 0, bool depthTest = false)
	{
		var up = (end - start).normalized * radius;
		var forward = Vector3.Slerp(up, -up, 0.5f);
		var right = Vector3.Cross(up, forward).normalized * radius;
		var rot = Quaternion.LookRotation(start - end);

		//Radial circles
		self.Circle(start, rot, color, radius, duration, depthTest);
		self.Circle(end, rot, color, radius, duration, depthTest);
		self.Circle((start + end) * 0.5f, rot, color, radius, duration, depthTest);

		//Side lines
		self.Line(start + right, end + right, color, duration, depthTest);
		self.Line(start - right, end - right, color, duration, depthTest);

		self.Line(start + forward, end + forward, color, duration, depthTest);
		self.Line(start - forward, end - forward, color, duration, depthTest);

		//Start endcap
		self.Line(start - right, start + right, color, duration, depthTest);
		self.Line(start - forward, start + forward, color, duration, depthTest);

		//End endcap
		self.Line(end - right, end + right, color, duration, depthTest);
		self.Line(end - forward, end + forward, color, duration, depthTest);
	}

	public static void Cone(this ISimpleDraw self, Vector3 position, Vector3 direction, Color? color = null, float angle = 45, float duration = 0, bool depthTest = false)
	{
		float length = direction.magnitude;

		var forward = direction;
		var up = Vector3.Slerp(forward, -forward, 0.5f);
		var right = Vector3.Cross(forward, up).normalized * length;
		var rot = Quaternion.LookRotation(direction);

		direction = direction.normalized;

		var slerpedVector = Vector3.Slerp(forward, up, angle / 90.0f);

		float dist;
		var farPlane = new Plane(-direction, position + forward);
		var distRay = new Ray(position, slerpedVector);

		farPlane.Raycast(distRay, out dist);

		self.Ray(position, slerpedVector.normalized * dist, color);
		self.Ray(position, Vector3.Slerp(forward, -up, angle / 90.0f).normalized * dist, color, duration, depthTest);
		self.Ray(position, Vector3.Slerp(forward, right, angle / 90.0f).normalized * dist, color, duration, depthTest);
		self.Ray(position, Vector3.Slerp(forward, -right, angle / 90.0f).normalized * dist, color, duration, depthTest);

		self.Circle(position + forward, rot, color, (forward - (slerpedVector.normalized * dist)).magnitude, duration, depthTest);
		self.Circle(position + (forward * 0.5f), rot, color, ((forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude, duration, depthTest);
	}

	public static void Capsule(this ISimpleDraw self, Vector3 start, Vector3 end, Color? color = null, float radius = 1, float duration = 0, bool depthTest = false)
	{
		var up = (end - start).normalized * radius;
		var forward = Vector3.Slerp(up, -up, 0.5f);
		var right = Vector3.Cross(up, forward).normalized * radius;
		var rot = Quaternion.LookRotation(end - start);

		float height = (start - end).magnitude;
		float sideLength = Mathf.Max(0, (height * 0.5f) - radius);
		var middle = (end + start) * 0.5f;

		start = middle + ((start - middle).normalized * sideLength);
		end = middle + ((end - middle).normalized * sideLength);

		//Radial circles
		self.Circle(start, rot, color, radius, duration, depthTest);
		self.Circle(end, rot, color, radius, duration, depthTest);

		//Side lines
		self.Line(start + right, end + right, color, duration, depthTest);
		self.Line(start - right, end - right, color, duration, depthTest);

		self.Line(start + forward, end + forward, color, duration, depthTest);
		self.Line(start - forward, end - forward, color, duration, depthTest);

		for (int i = 1; i < 26; i++)
		{

			//Start endcap
			self.Line(Vector3.Slerp(right, -up, i / 25.0f) + start, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
			self.Line(Vector3.Slerp(-right, -up, i / 25.0f) + start, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
			self.Line(Vector3.Slerp(forward, -up, i / 25.0f) + start, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
			self.Line(Vector3.Slerp(-forward, -up, i / 25.0f) + start, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);

			//End endcap
			self.Line(Vector3.Slerp(right, up, i / 25.0f) + end, Vector3.Slerp(right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
			self.Line(Vector3.Slerp(-right, up, i / 25.0f) + end, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
			self.Line(Vector3.Slerp(forward, up, i / 25.0f) + end, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
			self.Line(Vector3.Slerp(-forward, up, i / 25.0f) + end, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
		}
	}

	readonly static Vector3[] diamondCorner =
	{
		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3(+1.0f, 0.0f, 0.0f),
		new Vector3(0.0f, -1.0f, 0.0f),
		new Vector3(0.0f, +1.0f, 0.0f),
		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, 0.0f, +1.0f)
	};

	public static void Diamond(this ISimpleDraw self, Vector3 center, Color? color = null, float size = 1, float duration = 0.0f, bool depthTest = false)
	{
		var v = new Vector3[6];

		for (int i = 0; i < 6; ++i)
		{
			v[i] = center + size * diamondCorner[i];
		}

		self.Line(v[0], v[2], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[1], v[3], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[2], v[1], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[3], v[0], color: color, duration: duration, depthTest: depthTest);

		self.Line(v[4], v[0], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[4], v[1], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[4], v[2], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[4], v[3], color: color, duration: duration, depthTest: depthTest);

		self.Line(v[5], v[0], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[5], v[1], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[5], v[2], color: color, duration: duration, depthTest: depthTest);
		self.Line(v[5], v[3], color: color, duration: duration, depthTest: depthTest);
	}
}