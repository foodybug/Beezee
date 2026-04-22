using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    [SerializeField] Transform center;
    public int maxFood = 10;
    public int food = 10; // Default food amount

    private UnityEngine.UI.Slider foodSlider;

    void Start()
    {
        food = maxFood;

        // 동적으로 캔버스와 슬라이더 생성
        GameObject canvasObj = new GameObject("FlowerCanvas");
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0); 
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(2, 0.4f);

        GameObject sliderObj = new GameObject("FoodSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = Vector2.zero;
        sliderRT.anchorMax = Vector2.one;
        sliderRT.sizeDelta = Vector2.zero;

        foodSlider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0,0,0,0.5f);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one; fillAreaRT.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        UnityEngine.UI.Image fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(1f, 0.5f, 0f); // 주황색(꿀 느낌)
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = Vector2.zero;

        foodSlider.targetGraphic = bgImg;
        foodSlider.fillRect = fillRT;
        foodSlider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        foodSlider.interactable = false;
        foodSlider.transition = UnityEngine.UI.Selectable.Transition.None;

        foodSlider.maxValue = maxFood;
        foodSlider.value = food;
    }

    void LateUpdate()
    {
        if (foodSlider != null)
        {
            foodSlider.value = food;
            if (Camera.main != null)
            {
                foodSlider.transform.parent.forward = Camera.main.transform.forward;
            }
        }
    }

    public int TakeFood(int amount)
    {
        if (food <= 0) return 0;

        int taken = Mathf.Min(food, amount);
        food -= taken;

        if (food <= 0)
        {
            // Turn color to yellow
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material.color = Color.yellow;
            }

            // Remove from 'Flower' layer so bees won't target it anymore
            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        return taken;
    }
}
