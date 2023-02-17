using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MouseLook : MonoBehaviour
{
	public Vector2 mouseAbsolute;
	public Vector2 mouseClampInDegrees = new Vector2(360, 180);
	public Vector2 mouseSensitivity = new Vector2(500, 500);
	public float moveSpeed = 20.0f;

	void Update()
	{
		if (!Input.GetKey(KeyCode.Mouse1))
			return;

		var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
		var moveDelta = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		if (Input.GetKey(KeyCode.Q))
			moveDelta.y += 1;
		if (Input.GetKey(KeyCode.E))
			moveDelta.y -= 1;
		var speedBoost = Input.GetKey(KeyCode.LeftShift);
		var moveSpeedAdjust = Input.GetAxis("Mouse ScrollWheel");

		mouseDelta = Vector2.Scale(mouseDelta, mouseSensitivity);
		mouseDelta *= Time.deltaTime;
		moveSpeed = Mathf.Pow(moveSpeed, 1 + moveSpeedAdjust * Time.deltaTime * 30);
		moveDelta *= moveSpeed;
		if (speedBoost)
			moveDelta *= 4;
		moveDelta *= Time.deltaTime;
		mouseAbsolute += mouseDelta;
		if (mouseClampInDegrees.x <= 360)
			mouseAbsolute.x = Mathf.Clamp(mouseAbsolute.x, -mouseClampInDegrees.x * 0.5f, mouseClampInDegrees.x * 0.5f);
		if (mouseClampInDegrees.y <= 360)
			mouseAbsolute.y = Mathf.Clamp(mouseAbsolute.y, -mouseClampInDegrees.y * 0.5f, mouseClampInDegrees.y * 0.5f);

		transform.rotation = Quaternion.AngleAxis(mouseAbsolute.x, Vector3.up) * Quaternion.AngleAxis(-mouseAbsolute.y, Vector3.right);
		transform.position = transform.position + transform.rotation * moveDelta;
	}
}