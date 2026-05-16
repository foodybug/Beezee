using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ColonyUI : MonoBehaviour
{
    [Header("Target")]
    public Colony colony;

    [Header("UI Elements")]
    public Slider foodSlider;
    public Image colonyIndicator;

    [Header("Screen Settings")]
    public Vector3 worldOffset = new Vector3(0, 3f, 0);

    private RectTransform rectTransform;
    private Canvas screenCanvas;
    private Camera mainCam;
    private CanvasGroup canvasGroup;
    private bool isInitialized = false;

    private void Start()
    {
        mainCam = Camera.main;

        // Hide unnecessary HP UI elements
        Transform hpText = transform.Find("UI_Stats/HP");
        if (hpText != null) hpText.gameObject.SetActive(false);
        Transform hpSliderObj = transform.Find("UI_Stats/HpSlider");
        if (hpSliderObj != null) hpSliderObj.gameObject.SetActive(false);

        // colony 참조가 없으면 부모에서 찾기
        if (colony == null)
            colony = GetComponentInParent<Colony>();

        // 메인 ScreenSpace 캔버스 찾기 (Overlay 우선, 없으면 Camera)
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (screenCanvas == null || c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCanvas = c;
                }
            }
        }

        if (screenCanvas != null)
        {
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null) Destroy(scaler);

            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null) Destroy(ownCanvas);

            transform.SetParent(screenCanvas.transform, false);

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                // 앵커를 중앙으로 강제 설정하여 캔버스 크기 변화에 따라 찌그러지지 않게 함
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            isInitialized = true;
        }
        else
        {
            Debug.LogError("[ColonyUI] ScreenSpace 캔버스를 찾을 수 없습니다!");
        }
    }

    private void LateUpdate()
    {
        if (!isInitialized) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || colony == null) return;

        // 월드 좌표를 스크린 좌표로 변환
        Vector3 worldPos = colony.transform.position + worldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        // 카메라 뒤에 있으면 표시하지 않기
        if (screenPos.z < 0)
        {
            if (canvasGroup != null && canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            return;
        }
        else
        {
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        // UI 위치 업데이트
        if (rectTransform != null && screenCanvas != null)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenCanvas.GetComponent<RectTransform>(), 
                screenPos, 
                screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCam, 
                out localPos);
            rectTransform.localPosition = localPos;
        }
        else if (rectTransform != null)
        {
            rectTransform.position = screenPos;
        }

        // 식량 비율 업데이트 (벌 1마리 생성에 필요한 100을 기준으로 비율 표시)
        if (foodSlider != null)
        {
            foodSlider.value = Mathf.Clamp01((float)colony.food / 100f);
        }

        // 소속 콜로니 색상 업데이트
        if (colonyIndicator != null)
        {
            if (colony.flag == eColony.Red)
                colonyIndicator.color = Color.red;
            else if (colony.flag == eColony.Blue)
                colonyIndicator.color = Color.blue;
            else
                colonyIndicator.color = Color.white; // 기본값
        }
    }
}
