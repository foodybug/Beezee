using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tag
{
    public const string Adjacentblock = "Adjacentblock";
    public const string SabreCard = "SabreCard";
    public const string Board = "Board";
}

public enum eSide { Up = 0, Right, Down, Left = 3, NONE = 4 }
public static class Side
{
    public static eSide GetCounterSide(eSide side)
    {
        switch (side)
        {
            case eSide.Left:
                return eSide.Right;
            case eSide.Right:
                return eSide.Left;
            case eSide.Up:
                return eSide.Down;
            case eSide.Down:
                return eSide.Up;
            default:
                Debug.LogWarning($"Side:: GetCounterSide: INVALID side = {side}");
                return eSide.NONE;
        }
    }
    public static eSide CheckSide(Vector2Int org, Vector2Int dest)
    {
        if (org.x == dest.x)
        {
            if (org.y == dest.y)
            {
                return eSide.NONE;
            }
            else
            {
                if (org.y + 1 == dest.y)
                    return eSide.Up;
                else if (org.y - 1 == dest.y)
                    return eSide.Down;
                else
                    return eSide.NONE;
            }
        }
        else
        {
            if (org.x + 1 == dest.x)
                return eSide.Right;
            else if (org.x - 1 == dest.x)
                return eSide.Left;
            else
                return eSide.NONE;
        }
    }
}
public enum eDirection { Up = 0, Right, Down, Left = 3 }
public static class Direction
{
	public const int Count = 4;
	public static eDirection GetCounterDirection(eDirection dir)
	{
		switch (dir)
		{
			case eDirection.Left:
				return eDirection.Right;
			case eDirection.Right:
				return eDirection.Left;
			case eDirection.Up:
				return eDirection.Down;
			case eDirection.Down:
				return eDirection.Up;
			default:
				Debug.LogWarning($"Side:: GetCounterDirection: INVALID dir = {dir}");
				return eDirection.Up;
		}
	}
    public static eDirection GetWorldDirection(eDirection cardDir, eDirection destDir)//, out eDirection resultDir)
	{
        eDirection result = (eDirection)(((int)cardDir + (int)destDir + Count) % Count);
        //Debug.Log($"Direction:: GetWorldDirection: cardDir = {cardDir}, destDir = {destDir}, result = {result}");

        return result;
    }
    public static eDirection GetLocalDirection(eDirection cardDir, eDirection destDir)//, out eDirection resultDir)
    {
        eDirection result = (eDirection)(((int)cardDir - (int)destDir + Count) % Count);
        //Debug.Log($"Direction:: GetLocalDirection: cardDir = {cardDir}, destDir = {destDir}, result = {result}");

        return result;
    }
    public static eDirection Rotate_ClockWise(eDirection d)
    {
        switch(d)
        {
            case eDirection.Up:
                return eDirection.Right;
            case eDirection.Right:
                return eDirection.Down;
            case eDirection.Down:
                return eDirection.Left;
            case eDirection.Left:
                return eDirection.Up;
            default:
                Debug.LogError($"Direction:: Rotate_ClockWise: invalid direction = {d}");
                return eDirection.Up;
        }
    }
	public static eDirection Rotate_CounterClockWise(eDirection d)
	{
		switch (d)
		{
			case eDirection.Up:
				return eDirection.Left;
			case eDirection.Right:
				return eDirection.Up;
			case eDirection.Down:
				return eDirection.Right;
			case eDirection.Left:
				return eDirection.Down;
			default:
				Debug.LogError($"Direction:: Rotate_CounterClockWise: invalid direction = {d}");
				return eDirection.Up;
		}
	}
    public static float GetAngle(eDirection d)
    {
		switch (d)
		{
			case eDirection.Up:
				return 0f;
			case eDirection.Right:
				return -90f;
			case eDirection.Down:
				return -180f;
			case eDirection.Left:
				return -270f;
			default:
				Debug.LogError($"Direction:: GetAngle: invalid direction = {d}");
				return 0f;
		}
	}
}