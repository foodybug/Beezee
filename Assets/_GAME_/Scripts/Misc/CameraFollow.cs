using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // 타겟
    public float smoothTime = 0.15f; // 카메라 이동 부드러움 정도 (반응성 향상)
    public Vector3 offset;        // 카메라 기본 오프셋
    private Vector3 curOffset;

    [Header("Zoom Settings")]
    public float minZoom = 0.5f;        // 줌 인 범위를 늘려 오브젝트를 크게 볼 수 있도록 설정
    public float maxZoom = 12.5f;
    
    private float targetDistance = 10f;
    private float curDistance = 10f;
    private int currentZoomStep = 1; // 0: minZoom, 1: midZoom, 2: maxZoom

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
            float initialMag = offset.magnitude;
            float midZoom = (minZoom + maxZoom) / 2f;
            
            // 초기 거리에 가장 가까운 줌 단계 설정
            if (Mathf.Abs(initialMag - minZoom) < Mathf.Abs(initialMag - midZoom))
                currentZoomStep = 0;
            else if (Mathf.Abs(initialMag - maxZoom) < Mathf.Abs(initialMag - midZoom))
                currentZoomStep = 2;
            else
                currentZoomStep = 1;

            targetDistance = GetZoomLevel(currentZoomStep);
            curDistance = targetDistance;
            curOffset = offset.normalized * curDistance;
        }

        if (cam != null && cam.orthographic && offset != Vector3.zero)
        {
            cam.orthographicSize = curDistance;
        }
    }

    private float GetZoomLevel(int step)
    {
        if (step == 0) return minZoom;
        if (step == 1) return (minZoom + maxZoom) / 2f;
        return maxZoom;
    }

    // 마우스 휠 입력을 프레임당 한 번씩만 처리하기 위한 쿨다운 (빠른 스크롤 방지)
    private float scrollCooldown = 0f;

    private void Update()
    {
        if (scrollCooldown > 0f)
            scrollCooldown -= Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        // 스크롤 입력이 있을 때 3단계 줌 적용
        if (scroll != 0f && offset != Vector3.zero && scrollCooldown <= 0f)
        {
            if (scroll > 0f)
            {
                // Zoom in
                currentZoomStep--;
                if (currentZoomStep < 0) currentZoomStep = 0;
            }
            else if (scroll < 0f)
            {
                // Zoom out
                currentZoomStep++;
                if (currentZoomStep > 2) currentZoomStep = 2;
            }
            
            targetDistance = GetZoomLevel(currentZoomStep);
            scrollCooldown = 0.15f; // 약간의 쿨다운을 주어 한 번의 휠 굴림에 여러 단계 건너뛰는 것을 방지
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
        }
    }
}