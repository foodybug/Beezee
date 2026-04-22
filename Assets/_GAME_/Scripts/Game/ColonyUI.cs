using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ColonyUI : MonoBehaviour
{
    [Header("Target")]
    public Colony colony;

    [Header("UI Elements")]
    public Slider hpSlider;
    public Slider foodSlider;
    public Image colonyIndicator;

    [Header("Screen Settings")]
    public Vector3 worldOffset = new Vector3(0, 3f, 0);

    private void LateUpdate()
    {
        // 최적화를 위해 메인 카메라 참조
        if (Camera.main != null)
        {
            // UI 캔버스가 카메라를 정면으로 바라보게 함 (빌보드 효과)
            transform.forward = Camera.main.transform.forward;
        }

        // 지정된 오프셋으로 위치 적용 (부모인 Colony 기준)
        transform.localPosition = worldOffset;

        if (colony != null)
        {
            // 체력 비율 업데이트
            if (hpSlider != null && colony.maxHp > 0)
            {
                hpSlider.value = (float)colony.hp / colony.maxHp;
            }

            // 식량 비율 업데이트
            if (foodSlider != null && colony.maxFood > 0)
            {
                foodSlider.value = (float)colony.food / colony.maxFood;
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
}
