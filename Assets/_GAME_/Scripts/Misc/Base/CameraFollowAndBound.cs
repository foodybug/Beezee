using UnityEngine;

public class CameraFollowAndBound : MonoBehaviour
{
	public Transform target; // 카메라가 따라갈 대상 (예: 플레이어)
	public float smoothSpeed = 0.125f; // 카메라 움직임의 부드러움 정도

	[Header("카메라 이동 영역 제한")]
	public Vector2 minCameraBound; // 카메라가 이동할 수 있는 최소 X, Y 좌표
	public Vector2 maxCameraBound; // 카메라가 이동할 수 있는 최대 X, Y 좌표

	void FixedUpdate() // FixedUpdate는 물리 계산에 사용되므로 카메라 팔로우에 적합
	{
		if (target == null)
		{
			Debug.LogWarning("CameraFollowAndBound: Target is not assigned!");
			return;
		}

		// 1. 타겟 위치 가져오기
		Vector3 desiredPosition = target.position;

		// 2. Z축은 고정 (2D 게임의 경우)
		// 3D 게임이라면 Z축도 조절 가능
		desiredPosition.z = transform.position.z;

		// 3. 카메라 위치를 제한된 영역 내로 Clamp (고정)
		float clampedX = Mathf.Clamp(desiredPosition.x, minCameraBound.x, maxCameraBound.x);
		float clampedY = Mathf.Clamp(desiredPosition.y, minCameraBound.y, maxCameraBound.y);

		Vector3 clampedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);

		// 4. 부드러운 이동 (Lerp 사용)
		Vector3 smoothedPosition = Vector3.Lerp(transform.position, clampedPosition, smoothSpeed);
		transform.position = smoothedPosition;
	}

	// 개발 중 카메라 경계를 시각적으로 확인하기 위한 기즈모 (선택 사항)
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		// 최소/최대 좌표를 이용하여 경계 상자 그리기
		Vector3 topLeft = new Vector3(minCameraBound.x, maxCameraBound.y, transform.position.z);
		Vector3 topRight = new Vector3(maxCameraBound.x, maxCameraBound.y, transform.position.z);
		Vector3 bottomLeft = new Vector3(minCameraBound.x, minCameraBound.y, transform.position.z);
		Vector3 bottomRight = new Vector3(maxCameraBound.x, minCameraBound.y, transform.position.z);

		Gizmos.DrawLine(topLeft, topRight);
		Gizmos.DrawLine(topRight, bottomRight);
		Gizmos.DrawLine(bottomRight, bottomLeft);
		Gizmos.DrawLine(bottomLeft, topLeft);
	}
}