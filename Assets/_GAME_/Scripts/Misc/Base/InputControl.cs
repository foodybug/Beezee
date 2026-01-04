using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControl : MonoBehaviour
{
    public static InputControl it { get; private set; }

    public Action<GameObject> aMouseEnter;
    public Action<GameObject> aMouseExit;
    public Action<GameObject> aMouseClick;

    GameObject currentHoveredObject;
    Ray ray;
    RaycastHit hit;

    private void Awake()
    {
        it = this;
    }
    private void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            // 새로운 오브젝트에 마우스가 올라간 경우
            if (hit.collider.gameObject != currentHoveredObject)
            {
                // 이전에 마우스가 있던 오브젝트에서 Exit 이벤트 처리
                if (currentHoveredObject != null)
                {
                    HandleMouseExit(currentHoveredObject);
                }
                // 새로 진입한 오브젝트에서 Enter 이벤트 처리
                currentHoveredObject = hit.collider.gameObject;
                HandleMouseEnter(currentHoveredObject);
            }
            // 현재 오브젝트에 계속 마우스가 올라가 있는 경우
            //HandleMouseOver(currentHoveredObject);

            if (Input.GetMouseButtonDown(0) == true)
            {
                HandleMouseClick(currentHoveredObject);
            }
        }
        else
        {
            // 마우스가 아무 오브젝트에도 올라가 있지 않은 경우
            if (currentHoveredObject != null)
            {
                HandleMouseExit(currentHoveredObject);
                currentHoveredObject = null;
            }
            if (Input.GetMouseButtonDown(0) == true)
            {
                HandleMouseClick(null);
            }
        }
    }
    void HandleMouseEnter(GameObject obj)
    {
        //Debug.Log(obj.name + ": Raycast Mouse Enter!");

        aMouseEnter?.Invoke(obj);

        //AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
        //if (ab != null) ab.MouseEnter();
    }
    void HandleMouseExit(GameObject obj)
    {
        //Debug.Log(obj.name + ": Raycast Mouse Exit!");

        aMouseExit?.Invoke(obj);

        //AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
        //if (ab != null) ab.MouseExit();
    }
    void HandleMouseClick(GameObject obj)
    {
        Debug.Log($"InputControl:: HandleMouseClick: aMouseClick = {aMouseClick}, obj = {obj}");

        aMouseClick?.Invoke(obj);

        //Debug.Log(obj.name + ": Raycast Mouse Exit!");
        //AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
        //if (ab != null) ab.MouseClick(GameMaster.I.currentSabreCard);
        //GameMaster.I.currentSabreCard = null;
    }
}
