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
            // ���ο� ������Ʈ�� ���콺�� �ö� ���
            if (hit.collider.gameObject != currentHoveredObject)
            {
                // ������ ���콺�� �ִ� ������Ʈ���� Exit �̺�Ʈ ó��
                if (currentHoveredObject != null)
                {
                    HandleMouseExit(currentHoveredObject);
                }
                // ���� ������ ������Ʈ���� Enter �̺�Ʈ ó��
                currentHoveredObject = hit.collider.gameObject;
                HandleMouseEnter(currentHoveredObject);
            }
            // ���� ������Ʈ�� ��� ���콺�� �ö� �ִ� ���
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
            // ���콺�� �ƹ� ������Ʈ���� �ö� ���� ���� ���
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


        //if (ab != null) ab.MouseEnter();
    }
    void HandleMouseExit(GameObject obj)
    {
        //Debug.Log(obj.name + ": Raycast Mouse Exit!");

        aMouseExit?.Invoke(obj);


        //if (ab != null) ab.MouseExit();
    }
    void HandleMouseClickDown(GameObject obj)
    {
        //Debug.Log($"InputControl:: HandleMouseClickDown: aMouseClickDown = {aMouseClickDown}, obj = {obj}");

		aMouseClickDown?.Invoke(obj);

        //Debug.Log(obj.name + ": Raycast Mouse Exit!");



    }
	void HandleMouseClickUp(GameObject obj)
	{
		//Debug.Log($"InputControl:: HandleMouseClickUp: aMouseClickUp = {aMouseClickUp}, obj = {obj}");

		aMouseClickUp?.Invoke(obj);

		//Debug.Log(obj.name + ": Raycast Mouse Exit!");



	}
	void HandleMouseClicking(RaycastHit hit)
	{
		//Debug.Log($"InputControl:: HandleMouseClickUp: aMouseClicking = {aMouseClicking}, obj = {hit.transform.gameObject}");

		aMouseClicking?.Invoke(hit.transform.gameObject, hit.point);

		//Debug.Log(obj.name + ": Raycast Mouse Exit!");



	}
}
