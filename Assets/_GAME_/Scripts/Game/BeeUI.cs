using UnityEngine;
using UnityEngine.UI;

public class BeeUI : MonoBehaviour
{
    [Header("Target")]
    public Bee bee;

    [Header("UI Elements")]
    public Image hpFill;
    public Image foodFill;
    public Image colonyIndicator;

    private void LateUpdate()
    {
        // 최적화를 위해 메인 카메라 참조 (매 프레임 Camera.main보다 변수 할당이 나을 수 있으나 2020.2 이후로는 빠릅니다)
        if (Camera.main != null)
        {
            // UI 캔버스가 카메라를 정면으로 바라보게 함 (빌보드 효과)
            transform.forward = Camera.main.transform.forward;
        }

        if (bee != null)
        {
            // 체력 비율 업데이트
            if (hpFill != null && bee.maxHp > 0)
            {
                hpFill.fillAmount = (float)bee.hp / bee.maxHp;
            }

            // 식량 비율 업데이트
            if (foodFill != null && bee.maxFood > 0)
            {
                foodFill.fillAmount = (float)bee.food / bee.maxFood;
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
        }
    }
}
