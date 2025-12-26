using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public static GameBoard I { get; private set; }

    [SerializeField] List<Card> listBoard;

    //Dictionary<int, Dictionary<int, GameObject>> ddicBoard = new Dictionary<int, Dictionary<int, GameObject>>();
    Dictionary<Vector2Int, Card> dicBoard = new Dictionary<Vector2Int, Card>();
    [SerializeField] List<Card> listActivatedBoard;

    private void Awake()
    {
        I = this;

        _Init();
    }
    void _Init()
    {
        for (int i = 0; i < listBoard.Count; i++)
        {
            string[] s = listBoard[i].name.Split("_");
            if (int.TryParse(s[1], out int num) == true)
            {
                listBoard[i].Init(OnBoardClicked, num);
                dicBoard.Add(new Vector2Int(num, 0), listBoard[i]);
            }
            else
                Debug.LogError($"GameBoard:: _Init: invalid name");
        }
    }
    void OnBoardClicked(int index)
    {
        GameMaster.I.MsgProc(new Msg_Move(index));
    }
    #region - get & set -
    public void Set(Vector2Int v, Card c)
    {
        _Set(v, c);
    }
    public void Set(int x, int y, Card c)
    {
        _Set(new Vector2Int(x, y), c);
    }
    public void _Set(Vector2Int v, Card c)
    {
        if (dicBoard.ContainsKey(v) == false)
        {
            dicBoard.Add(v, c);
        }
        else
        {
            Debug.LogError($"GameBoard:: Set: already set grid. v.x = {v.x}, v.y = {v.y}");
        }
    }
    public Card Get(Vector2Int v)
    {
        return _Get(v);
    }
    public Card Get(int x, int y = 0)
    {
        return _Get(new Vector2Int(x, y));
    }
    Card _Get(Vector2Int v)
    {
        if (dicBoard.ContainsKey(v) == true)
        {
            return dicBoard[v];
        }
        else
        {
            Debug.LogWarning($"GameBoard:: Get: not found. dicBoard.ContainsKey({v}) = false");
            return null;
        }
    }
    #endregion
    public void HighLight(int playerPosIndex, int cardNumber, int oppositePlayerPosIndex, bool reset = false)
    {
        if(reset == true)
        {
            foreach(Card node in listActivatedBoard)
            {
                node.HighLight(false);
            }

            listActivatedBoard.Clear();
        }

        //int realIndex = playerPosIndex - 1;
        //int realIndexOpposite = oppositePlayerPosIndex - 1;
        //int destL = realIndex - cardNumber;
        //int destR = realIndex + cardNumber;

        //bool playerIsInLeft = playerPosIndex < oppositePlayerPosIndex;
        //if (playerIsInLeft == true)
        //{
        //    bool movableToRight = destR <= realIndexOpposite;
        //    if (destL >= 0)
        //    {
        //        listBoard[destL].HighLight(true);
        //        listActivatedBoard.Add(listBoard[destL]);
        //    }
        //    if (destR < listBoard.Count && movableToRight == true)
        //    {
        //        listBoard[destR].HighLight(true);
        //        listActivatedBoard.Add(listBoard[destR]);
        //    }
        //}
        //else
        //{
        //    bool movableToLeft = destL >= realIndexOpposite;
        //    if (destL >= 0)
        //    {
        //        listBoard[destL].HighLight(true);
        //        listActivatedBoard.Add(listBoard[destL]);
        //    }
        //    if (destR < listBoard.Count && movableToLeft == true)
        //    {
        //        listBoard[destR].HighLight(true);
        //        listActivatedBoard.Add(listBoard[destR]);
        //    }
        //}

        List<int> list = GetEnableIndices(playerPosIndex, cardNumber, oppositePlayerPosIndex);
        foreach(int node in list)
        {
            listBoard[node].HighLight(true);
            listActivatedBoard.Add(listBoard[node]);
        }
    }
    List<int> GetEnableIndices(int playerPosIndex, int cardNumber, int oppositePlayerPosIndex)
    {
        List<int> list = new List<int>();

        int realIndex = playerPosIndex - 1;
        int realIndexOpposite = oppositePlayerPosIndex - 1;
        int destL = realIndex - cardNumber;
        int destR = realIndex + cardNumber;

        bool playerIsInLeft = playerPosIndex < oppositePlayerPosIndex;
        if (playerIsInLeft == true)
        {
            bool movableToRight = destR <= realIndexOpposite;
            if (0 <= destL)
            {
                list.Add(destL);
            }
            if (destR < listBoard.Count && movableToRight == true)
            {
                list.Add(destR);
            }
        }
        else
        {
            bool movableToLeft = realIndexOpposite <= destL;
            if (destR < listBoard.Count)
            {
                list.Add(destR);
            }
            if (0 < destL && movableToLeft == true)
            {
                list.Add(destL);
            }
        }

        return list;
    }
    public bool CheckMovable(int playerPosIndex, int cardNumber, int oppositePlayerPosIndex)
    {
        return GetEnableIndices(playerPosIndex, cardNumber, oppositePlayerPosIndex).Count != 0;
    }
	public void Clear()
	{
        foreach(Card node in listBoard)
        {
            node.HighLight(false);
        }
	}
}
