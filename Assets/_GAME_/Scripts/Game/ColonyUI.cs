using UnityEngine;
using UnityEngine.UI;

public class ColonyUI : MonoBehaviour
{
    [Header("Target")]
    public Colony colony;

    [Header("UI Elements")]
    public Image hpFill;
    public Image foodFill;
    public Image colonyIndicator;

    private void LateUpdate()
    {
        // 최적화를 위해 메인 카메라 참조
        if (Camera.main != null)
        {
            // UI 캔버스가 카메라를 정면으로 바라보게 함 (빌보드 효과)
            transform.forward = Camera.main.transform.forward;
        }

        if (colony != null)
        {
            // 체력 비율 업데이트
            if (hpFill != null && colony.maxHp > 0)
            {
                hpFill.fillAmount = (float)colony.hp / colony.maxHp;
            }

            // 식량 비율 업데이트
            if (foodFill != null && colony.maxFood > 0)
            {
                foodFill.fillAmount = (float)colony.food / colony.maxFood;
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
