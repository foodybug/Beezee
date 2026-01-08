using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

public class Hand : MonoBehaviour
{
	public static Hand I {  get; private set; }

	//[SerializeField] List<CardInfo> listCard;
	[SerializeField] List<Transform> _listTrnHands;
	[SerializeField] List<SabreCard> listCard;
	//[SerializeField] List<CardInfo> listSelectedCard;
	[SerializeField] List<SabreCard> listSelectedCard;

	private void Awake()
	{
		I = this;

		//listCard[0].btn.onClick.AddListener(() => OnCardClicked(0));
		//listCard[1].btn.onClick.AddListener(() => OnCardClicked(1));
		//listCard[2].btn.onClick.AddListener(() => OnCardClicked(2));
		//listCard[3].btn.onClick.AddListener(() => OnCardClicked(3));
		//listCard[4].btn.onClick.AddListener(() => OnCardClicked(4));

		listSelectedCard.Clear();
	}
	
	public void Show(bool b)
	{
		gameObject.SetActive(b);
	}
	public void Set( List<SabreCard> list)
	{
		if (list == null)
			return;

		listSelectedCard.Clear();

        for (int i=0; i< list.Count; i++)
		{
			list[i].transform.SetParent(_listTrnHands[i]);
			list[i].transform.localPosition = Vector3.zero;
			list[i].transform.localRotation = Quaternion.identity;
		}

		listCard = list;
	}
	public void CardClicked(SabreCard sc, bool reset = false)
	{
        if (reset == true)
        {
            foreach (Card node in listSelectedCard)
            {
                node.HighLight(false);
            }

            listSelectedCard.Clear();
        }

        foreach (SabreCard node in listCard)
		{
			if (sc == node)
			{
                sc.HighLight(true);
                listSelectedCard.Add(node);
			}
		}
	}
	public void Deselect()
	{
        foreach (SabreCard node in listCard)
        {
            node.HighLight(false);
        }

		listSelectedCard.Clear();
    }
	public void DiscardUsedCard(List<SabreCard> list)
	{
        List<SabreCard> listTemp = new List<SabreCard>();

        foreach (SabreCard card in listSelectedCard)
		{
            foreach (SabreCard node in list)
            {
				if (card == node)
				{
					listTemp.Add(node);
                }
            }
        }

        listSelectedCard.Clear();

		foreach(SabreCard node in listTemp)
		{

        }
    }
}
