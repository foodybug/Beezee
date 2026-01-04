using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPile : MonoBehaviour
{
	[SerializeField] List<Card> listPile = new List<Card>();
    public Transform trnRoot;

	public void Shuffle()
	{
		listPile.Shuffle();
	}
	public bool Draw(out Card c)
	{
		c = null;

		if(listPile.Count > 0)
		{
			c = listPile[listPile.Count - 1];
            listPile.RemoveAt(listPile.Count - 1);
            return true;
        }
		else
		{
			Debug.Log($"CardPile:: Draw: no card");
            return false;
        }
	}
    public void Put(Card c)
    {
        listPile.Add(c);
    }
    public int RemainCount()
    {
        return listPile.Count;
    }
    //public bool DrawFoyer(out Card c)
    //{
    //    c = null;

    //    if (listPile.Count > 0)
    //    {
    //        c = listPile[listPile.Count - 1];
    //        listPile.RemoveAt(listPile.Count - 1);
    //        return true;
    //    }
    //    else
    //    {
    //        Debug.Log($"CardPile:: Draw: no card");
    //        return false;
    //    }
    //}
}
