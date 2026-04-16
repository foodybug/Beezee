using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    public RawImage minimapImage;
    public Transform target; // 플레이어 벌을 따라감

    [Header("Minimap Settings")]
    public float cameraHeight = 50f;       // 미니맵 카메라 높이
    public float orthoSize = 30f;          // 직교 카메라 사이즈 (보여지는 범위)
    public int textureSize = 256;          // RenderTexture 해상도

    private Camera minimapCam;
    private RenderTexture renderTex;

    private void Start()
    {
        SetupMinimapCamera();
    }

    private void SetupMinimapCamera()
    {
        // RenderTexture 생성
        renderTex = new RenderTexture(textureSize, textureSize, 16);
        renderTex.name = "MinimapRT";

        // 미니맵 카메라 생성
        GameObject camObj = new GameObject("MinimapCamera");
        camObj.transform.SetParent(transform); // MinimapUI 하위에 배치
        minimapCam = camObj.AddComponent<Camera>();
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = orthoSize;
        minimapCam.targetTexture = renderTex;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.15f, 0.2f, 0.15f, 1f); // 어두운 녹색 배경
        minimapCam.cullingMask = ~0; // 모든 레이어 렌더링
        minimapCam.depth = -10; // 메인 카메라보다 낮은 depth

        // 카메라를 위에서 아래로 바라보게 설정
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // RawImage에 RenderTexture 연결
        if (minimapImage != null)
        {
            minimapImage.texture = renderTex;
        }
    }

    private void LateUpdate()
    {
        if (minimapCam == null) return;

        // 타겟이 없으면 플레이어 벌 찾기
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }

        // 카메라 위치 업데이트 (타겟 위에서 내려다 봄)
        if (target != null)
        {
            minimapCam.transform.position = new Vector3(
                target.position.x,
                cameraHeight,
                target.position.z
            );
        }
    }

    private void OnDestroy()
    {
        if (renderTex != null)
        {
            renderTex.Release();
            Destroy(renderTex);
        }
    }
}
