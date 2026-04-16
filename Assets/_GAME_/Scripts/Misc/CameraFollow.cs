using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 타겟
    public float smoothTime = 0.15f; // 카메라 이동 부드러움 정도 (반응성 향상)
    public Vector3 offset;        // 카메라 기본 오프셋
    private Vector3 curOffset;

    [Header("Zoom Settings")]
    public float zoomSensitivity = 15f; // 줌 속도 대폭 상향
    public float minZoom = 0.5f;        // 줌 인 범위를 늘려 오브젝트를 크게 볼 수 있도록 설정
    public float maxZoom = 25f;
    
    private float targetDistance = 10f;
    private float curDistance = 10f;

    private Vector3 moveVelocity = Vector3.zero;
    private float zoomVelocity = 0f;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (offset != Vector3.zero)
        {
            targetDistance = offset.magnitude;
            curDistance = targetDistance;
            curOffset = offset;
        }

        if (cam != null && cam.orthographic && offset != Vector3.zero)
        {
            cam.orthographicSize = curDistance;
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        // 스크롤 입력이 있을 때 목표 거리 갱신
        if (scroll != 0f && offset != Vector3.zero)
        {
            targetDistance -= scroll * zoomSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minZoom, maxZoom);
        }

        // 스크롤 유무와 상관없이 매 프레임 SmoothDamp를 수행하여 뚝뚝 끊기지 않게 함
        if (offset != Vector3.zero)
        {
            curDistance = Mathf.SmoothDamp(curDistance, targetDistance, ref zoomVelocity, smoothTime);
            curOffset = offset.normalized * curDistance;

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = curDistance;
            }
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + curOffset;

            // 이동과 줌의 ref velocity를 분리하여 위치 갱신
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref moveVelocity, smoothTime);

            transform.LookAt(target.position);
        }
    }
}