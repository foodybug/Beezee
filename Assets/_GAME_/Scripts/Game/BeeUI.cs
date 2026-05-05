using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class BeeUI : MonoBehaviour
{
    [Header("Target")]
    public Bee bee;

    [Header("UI Elements")]
    public Slider hpSlider;
    public Slider foodSlider;
    public Image colonyIndicator;
    public Text stateText;

    [Header("Screen Settings")]
    public Vector3 worldOffset = new Vector3(0, -0.5f, 0);

    private RectTransform rectTransform;
    private Canvas screenCanvas;
    private Camera mainCam;
    private CanvasGroup canvasGroup;
    private bool isInitialized = false;

    private void Start()
    {
        mainCam = Camera.main;

        // bee 참조가 없으면 부모에서 찾기 (reparent 전에)
        if (bee == null)
            bee = GetComponentInParent<Bee>();

        // 메인 ScreenSpaceOverlay 캔버스 찾기
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenCanvas = c;
                break;
            }
        }

        if (screenCanvas != null)
        {
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
                Destroy(scaler);

            // 기존 WorldSpace Canvas 컴포넌트 제거 (종속성 때문에 가장 나중에 제거해야 함)
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null)
                Destroy(ownCanvas);

            // 메인 캔버스로 이동
            transform.SetParent(screenCanvas.transform, false);

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (stateText == null)
            {
                GameObject txtObj = new GameObject("StateText");
                txtObj.transform.SetParent(this.transform, false);
                stateText = txtObj.AddComponent<Text>();
                Font f = null;
                try
                {
                    f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch { }

                if (f == null)
                {
                    Text[] allTexts = FindObjectsOfType<Text>(true);
                    foreach (var t in allTexts)
                    {
                        if (t != null && t.font != null) { f = t.font; break; }
                    }
                }
                if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 14);
                stateText.font = f;
                stateText.fontSize = 14;
                stateText.alignment = TextAnchor.MiddleCenter;
                stateText.horizontalOverflow = HorizontalWrapMode.Overflow;
                stateText.verticalOverflow = VerticalWrapMode.Overflow;
                
                RectTransform txtRT = stateText.GetComponent<RectTransform>();
                // 슬라이더들(4, -4)보다 더 아래 위치
                txtRT.anchoredPosition = new Vector2(0, -20);
                txtRT.sizeDelta = new Vector2(100, 20);
                
                // 가독성을 위한 검은색 외곽선(Outline) 효과 추가
                Outline outline = txtObj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);
            }
            colonyIndicator.gameObject.SetActive(false);

            isInitialized = true;
        }
    }

    private void OnDestroy()
    {
        // bee가 파괴되면 UI도 같이 정리
    }

    private void LateUpdate()
    {
        if (!isInitialized) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || bee == null) return;

        if (bee.strCurState == "Death")
        {
            gameObject.SetActive(false);
            return;
        }

        // bee의 월드 좌표를 스크린 좌표로 변환
        Vector3 worldPos = bee.transform.position + worldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        // 카메라 뒤에 있으면 표시하지 않기 (gameObject 자체를 끄면 LateUpdate가 멈추므로 CanvasGroup 사용)
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

        // 체력 비율 업데이트
        if (hpSlider != null && bee.maxHp > 0)
        {
            hpSlider.value = (float)bee.hp / bee.maxHp;
        }

        // 식량 비율 업데이트
        if (foodSlider != null && bee.maxFood > 0)
        {
            foodSlider.value = (float)bee.food / bee.maxFood;
        }

        // 소속 콜로니 색상 업데이트
        if (colonyIndicator != null && bee.colony != null)
        {
            if (bee.colony.flag == eColony.Red)
                colonyIndicator.color = Color.red;
            else if (bee.colony.flag == eColony.Blue)
                colonyIndicator.color = Color.blue;
            else
                colonyIndicator.color = Color.white; // 기본값
        }

        // 현재 상태 텍스트 업데이트
        if (stateText != null && bee != null)
        {
            stateText.text = bee.strCurState;
        }
    }
}
