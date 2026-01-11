using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	public Transform target;      // 따라갈 대상 (플레이어)
	public float smoothTime = 0.3f; // 도달하는 데 걸리는 대략적인 시간
	public Vector3 offset;        // 대상과의 거리 유지

	private Vector3 velocity = Vector3.zero;

	private void LateUpdate()
	{
		if (target != null)
		{
			// 목표 위치 계산
			Vector3 targetPosition = target.position + offset;

			// SmoothDamp를 이용해 위치 이동
			transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
		}
	}
}