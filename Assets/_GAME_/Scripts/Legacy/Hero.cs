using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : Piece
{
    [SerializeField] float lerpSpeed = 1f;
    public void Move(Vector2Int coord)
    {
        this.coord = coord;

        StartCoroutine(_Move(coord));
    }
    IEnumerator _Move(Vector2Int coord)
    {
        Card c = GameBoard.I.Get(coord);
        GameMaster.I.PlaceObject(transform, c.transform, lerpSpeed);

        yield return null;
    }
}
