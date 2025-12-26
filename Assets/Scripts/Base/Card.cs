using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditorInternal.ReorderableList;

public class Card : MonoBehaviour, IMsgProc
{
	SM<Card> sm;
    [SerializeField] protected string strCurState = "";

    [SerializeField] Texture texture;
	[SerializeField] Material material;

	[SerializeField] Vector2Int _coord; public Vector2Int coord { get { return _coord; } }

    [SerializeField] Color cNormal = Color.white;
    [SerializeField] Color cEnable = new Color(0.5f, 1f, 0.5f, 0.5f);

    Action<int> aClicked;

	bool selected = false;

	public void Init(Action<int> a = null, int x = -1)
    {
		material = GetComponent<MeshRenderer>().material;

		sm = new SM<Card>(this, (a) => {
			Debug.Log($"[Player] SM<Player>:: ChangeState: type = {a}");
			strCurState = a.ToString();
		});
		sm.RegisterState(typeof(Default), new Default(sm));

		selected = false;
		material.color = cNormal;

		GetComponent<MeshRenderer>().material.mainTexture = texture;

		aClicked = a;
		_coord.x = x;
    }
    public void Selected()
    {
        if(selected == true)
            aClicked?.Invoke(_coord.x);
    }
	public void MsgProc(MsgBase m) { }
    #region - get & set -
	//public Vector2Int GetWorldCoord(eDirection dir) {

 //       eDirection worldDirection = Direction.GetWorldDirection(direction, dir);
	//	switch(worldDirection)
	//	{
	//		case eDirection.Up:
	//			return coord + Vector2Int.up;
	//		case eDirection.Right:
	//			return coord + Vector2Int.right;
	//		case eDirection.Down:
	//			return coord + Vector2Int.down;
	//		case eDirection.Left:
	//			return coord + Vector2Int.left;
	//		default:
	//			Debug.LogError($"Card:: GetCoord: invalid direction index. dirIndex = {dir}");
	//			return coord;
	//	}
	//}
	protected void SetCoord(Vector2Int v)
	{
		_coord = v;
	}
    #endregion
	public void HighLight(bool b)
	{
        if (b == true)
        {
            selected = true;
            material.color = cEnable;
        }
		else
		{
            selected = false;
            material.color = cNormal;
        }
    }
    #region - state -
	class Default : SM<Card>.BaseState, IState
	{
        public Default(SM<Card> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            //ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
            ddic[GetType()].Add(typeof(Msg_HighLight), OnHighLight);
        }
        public void Enter(MsgBase m)
        {
            Hand.I.Show(false);
        }
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
        void OnHighLight(MsgBase m)
        {
            Msg_HighLight hl = m as Msg_HighLight;
            if (hl.active == true)
            {
                owner.selected = true;
                owner.material.color = owner.cEnable;
            }
            else
            {
                owner.selected = false;
                owner.material.color = owner.cNormal;
            }
        }
    }
    #endregion
}
