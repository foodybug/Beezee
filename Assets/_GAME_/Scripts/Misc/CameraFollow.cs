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

    [Header("Quarter View (Isometric)")]
    public bool useQuarterView = true;
    public Vector3 quarterViewRotation = new Vector3(45f, 0f, 0f);

    private void Start()
    {
        cam = GetComponent<Camera>();

        // 씬에 이미 컴포넌트가 있어서 기본값(true)이 무시되고 false로 저장되어 있을 수 있으므로 강제로 켭니다.
        useQuarterView = true;

        if (useQuarterView)
        {
            if (cam != null) cam.orthographic = true;
            transform.rotation = Quaternion.Euler(quarterViewRotation);
            // 카메라가 바라보는 방향의 반대쪽으로 오프셋을 설정하여 타겟을 내려다보게 함
            offset = -transform.forward * 10f;
        }

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

            // transform.LookAt(target.position); // 회전하지 않도록 주석 처리
        }
    }
}