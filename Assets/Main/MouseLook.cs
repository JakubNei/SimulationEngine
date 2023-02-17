using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MouseLook : MonoBehaviour
{
	[SerializeField]
	Vector2 configMouseClampInDegrees = new Vector2(360, 180);
	[SerializeField]
	Vector2 configMouseSensitivity = new Vector2(500, 500);
	float moveSpeed = 20.0f;
	Vector2 mouseAbsolute;

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

		mouseDelta = Vector2.Scale(mouseDelta, configMouseSensitivity);
		mouseDelta *= Time.deltaTime;
		moveSpeed = Mathf.Pow(moveSpeed, 1 + moveSpeedAdjust * Time.deltaTime * 30);
		moveDelta *= moveSpeed;
		if (speedBoost)
			moveDelta *= 4;
		moveDelta *= Time.deltaTime;
		mouseAbsolute += mouseDelta;
		if (configMouseClampInDegrees.x <= 360)
			mouseAbsolute.x = Mathf.Clamp(mouseAbsolute.x, -configMouseClampInDegrees.x * 0.5f, configMouseClampInDegrees.x * 0.5f);
		if (configMouseClampInDegrees.y <= 360)
			mouseAbsolute.y = Mathf.Clamp(mouseAbsolute.y, -configMouseClampInDegrees.y * 0.5f, configMouseClampInDegrees.y * 0.5f);

		transform.rotation = Quaternion.AngleAxis(mouseAbsolute.x, Vector3.up) * Quaternion.AngleAxis(-mouseAbsolute.y, Vector3.right);
		transform.position = transform.position + transform.rotation * moveDelta;
	}
}