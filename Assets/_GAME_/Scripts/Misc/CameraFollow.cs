using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 타겟
    public float smoothTime = 0.3f; // 카메라 이동 부드러움 정도
    public Vector3 offset;        // 카메라 기본 오프셋
    Vector3 curOffset;        // 카메라 기본 오프셋

    [Header("Zoom Settings")]
    public float zoomSensitivity = 5f; // 마우스 휠 줌 민감도
    public float minZoom = 2f;         // 최소 줌 거리(또는 직교 카메라 사이즈)
    public float maxZoom = 20f;        // 최대 줌 거리(또는 직교 카메라 사이즈)
    float curDistance = 10f;

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    private void Start()
    {
        // 동일한 게임 오브젝트에 있는 Camera 컴포넌트 가져오기
        cam = GetComponent<Camera>();

        // 처음 오프셋 거리를 기반으로 카메라 사이즈 초기화 (직교 카메라일 경우)

        if (cam != null && cam.orthographic && offset != Vector3.zero)
        {
            cam.orthographicSize = offset.magnitude;
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && offset != Vector3.zero)
        {
            Vector3 direction = offset.normalized;
            float distance = offset.magnitude;

            // 휠 방향에 따라 값 조절 (앞으로 굴리면 줌인, 뒤로 굴리면 줌아웃)
            distance -= scroll * zoomSensitivity;
            distance = Mathf.Clamp(distance, minZoom, maxZoom);

            // 새로운 거리로 오프셋 업데이트
            offset = direction * distance;
            curOffset = Vector3.SmoothDamp(curOffset, offset, ref velocity, smoothTime);

            // 직교(Orthographic) 카메라가 있는 경우 Size도 같이 조정
            if (cam != null && cam.orthographic)
            {
                float v = 0f;
                curDistance = Mathf.SmoothDamp(curDistance, distance, ref v, smoothTime, 10f);
                cam.orthographicSize = curDistance;
            }
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            // 목표 위치 계산
            Vector3 targetPosition = target.position + curOffset;

            // SmoothDamp를 이용한 위치 이동
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            transform.LookAt(target.position);
        }
    }
}