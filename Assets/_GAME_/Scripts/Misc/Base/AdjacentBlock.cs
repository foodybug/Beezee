using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjacentBlock : MonoBehaviour
{
	Card _originCard;
    [SerializeField] protected eDirection _direction = eDirection.Up; public eDirection direction { get { return _direction; } }
    [SerializeField] bool _occupied = false; public bool occupied {  get { return _occupied; } }

	//[SerializeField] Renderer renderer;
	//[SerializeField] Card centerCard;
	[SerializeField] Color clEnter;
	[SerializeField] Color clExit;

	//StateMachine sm;
    Action<AdjacentBlock, Card> aSelected;

    private void Awake()
    {
        //sm = new StateMachine();
        //sm.RegisterState(typeof(Idle), new Idle(this));
        //sm.RegisterState(typeof(Selecting), new Selecting(this));

        //sm.ChangeState(typeof(Idle));

        gameObject.SetActive(false);
    }
    public void Init(Card card, Action<AdjacentBlock, Card> a)
    {
        _originCard = card;
        aSelected = a;
    }
	public void BeginSelect()
	{
        gameObject.SetActive(true);
		GetComponent<Renderer>().material.color = clEnter;
	}
    public void Selected(SabreCard m)
	{
		_occupied = true;

		//m.transform.position = transform.position;

        if(aSelected != null)
            aSelected(this, m);
    }
	public void EndSelect()
	{
        gameObject.SetActive(false);
    }
    public void Fill()
    {
        _occupied = true;
    }
}
