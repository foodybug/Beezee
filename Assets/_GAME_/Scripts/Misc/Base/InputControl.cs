using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputControl : MonoBehaviour
{
    public static InputControl I { get; private set; }

    public Action<GameObject> aMouseEnter;
    public Action<GameObject> aMouseExit;
    public Action<GameObject> aMouseClickDown;
    public Action<GameObject> aMouseClickUp;
    public Action<GameObject, Vector3> aMouseClicking;

    GameObject currentHoveredObject;
    Ray ray;
    RaycastHit hit;

    private void Awake()
    {
        I = this;
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
                HandleMouseClickDown(currentHoveredObject);
            }
			if (Input.GetMouseButtonUp(0) == true)
			{
				HandleMouseClickUp(currentHoveredObject);
			}
			if (Input.GetMouseButton(0) == true)
			{
				HandleMouseClicking(hit);
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
                HandleMouseClickDown(null);
            }
			if (Input.GetMouseButtonUp(0) == true)
			{
				HandleMouseClickUp(null);
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
    void HandleMouseClickDown(GameObject obj)
    {
        //Debug.Log($"InputControl:: HandleMouseClickDown: aMouseClickDown = {aMouseClickDown}, obj = {obj}");

		aMouseClickDown?.Invoke(obj);

        //Debug.Log(obj.name + ": Raycast Mouse Exit!");
        //AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
        //if (ab != null) ab.MouseClick(GameMaster.I.currentSabreCard);
        //GameMaster.I.currentSabreCard = null;
    }
	void HandleMouseClickUp(GameObject obj)
	{
		//Debug.Log($"InputControl:: HandleMouseClickUp: aMouseClickUp = {aMouseClickUp}, obj = {obj}");

		aMouseClickUp?.Invoke(obj);

		//Debug.Log(obj.name + ": Raycast Mouse Exit!");
		//AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
		//if (ab != null) ab.MouseClick(GameMaster.I.currentSabreCard);
		//GameMaster.I.currentSabreCard = null;
	}
	void HandleMouseClicking(RaycastHit hit)
	{
		//Debug.Log($"InputControl:: HandleMouseClickUp: aMouseClicking = {aMouseClicking}, obj = {hit.transform.gameObject}");

		aMouseClicking?.Invoke(hit.transform.gameObject, hit.point);

		//Debug.Log(obj.name + ": Raycast Mouse Exit!");
		//AdjacentBlock ab = obj.GetComponent<AdjacentBlock>();
		//if (ab != null) ab.MouseClick(GameMaster.I.currentSabreCard);
		//GameMaster.I.currentSabreCard = null;
	}
}
