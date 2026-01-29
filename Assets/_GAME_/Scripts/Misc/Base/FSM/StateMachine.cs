using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

#region - sm -
public interface IMsgProc
{
	public void MsgProc(MsgBase m) { }
}
public interface IState
{
	public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> dic) { }
    public void Enter(MsgBase m = null) { }
    public void Update() { }
    public void Exit() { }
}

public class StateMachine
{
	Dictionary<Type, IState> dicState = new Dictionary<Type, IState>();
	Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddicEvent = new Dictionary<Type, Dictionary<Type, Action<MsgBase>>>();
	private IState _currentState;
	protected Action<Type> cbStateChanged;

	public virtual bool RegisterState(Type t, IState s)
	{
		if(dicState.ContainsKey(t) == true)
		{
			Debug.LogError($"StateMachine:: RegisterState: already added state. t = {t}");
			return false;
		}
		else
		{
			dicState.Add(t, s);
            ddicEvent.Add(s.GetType(), new Dictionary<Type, Action<MsgBase>>());
            s.RegisterEvent(ddicEvent);
			return true;
		}
	}
    public bool RegisterState<T>(T t) where T : IState
    {
        if (dicState.ContainsKey(t.GetType()) == true)
        {
            Debug.LogError($"StateMachine:: RegisterState: already added state. t = {t}");
            return false;
        }
        else
        {
            dicState.Add(t.GetType(), t);
            ddicEvent.Add(t.GetType(), new Dictionary<Type, Action<MsgBase>>());
            t.RegisterEvent(ddicEvent);
            return true;
        }
    }
    public void Update()
	{
		if (_currentState != null)
		{
			_currentState.Update();
		}
	}

	// 상태 전환을 위한 메소드
	public void ChangeState(Type t, MsgBase m = null)
	{
		if (_currentState != null)
		{
			_currentState.Exit();
		}

		if (dicState.ContainsKey(t) == true)
		{
			_currentState = dicState[t];

			_currentState.Enter(m);
		}
		else
			Debug.LogError($"StateMachine:: ChangeState: no state = {t}");

		cbStateChanged?.Invoke(_currentState.GetType());
	}
	public void MsgProc(MsgBase msg)
	{
        if (_currentState != null)
        {
			Type s = _currentState.GetType();
			Type m = msg.GetType();

            if (ddicEvent.ContainsKey(s) == true)
			{
				if(ddicEvent[s].ContainsKey(m) == true)
				{
					ddicEvent[s][m].Invoke(msg);
				}
			}
        }
		else
		{
			Debug.LogWarning($"StateMachine:: MsgProc: NO current state");
		}
	}
}
public class SM<T> : StateMachine
{
    public T owner { get; private set; }
    public SM(T t)
    {
        this.owner = t;
    }
	public SM(T t, Action<Type> a)
	{
		this.owner = t;
		this.cbStateChanged = a;
	}
	public class BaseState : IState
    {
        public SM<T> sm { get; private set; }
        public T owner { get; private set; }
        public BaseState(SM<T> sm) { this.sm = sm; this.owner = sm.owner; }
    }
}
#endregion
#region - msg -
public class Msg_BtnMove : MsgBase
{

}
public class Msg_BtnRest : MsgBase
{

}
public class Msg_CardClicked : MsgBase
{
    public SabreCard sc;
    public Msg_CardClicked(SabreCard sc)
    {
        this.sc = sc;
    }
}
public class Msg_Draw : MsgBase
{
	public SabreCard sc;
	public Msg_Draw(SabreCard sc)
    {
        this.sc = sc;
    }
}
public class Msg_Waiting : MsgBase
{

}
public class Msg_Turn_Attack : MsgBase
{

}
public class Msg_Turn_Defence : MsgBase
{

}
public class Msg_HighLight : MsgBase
{
	public bool active;
	public Msg_HighLight(bool active)
	{
		this.active = active;
	}
}
public class Msg_Selected : MsgBase
{

}
public class Msg_Deselected : MsgBase
{

}
public class Msg_Move : MsgBase
{
	//public Bee Bee;
	public int targetIndex;
	public Msg_Move(int targetIndex)
	{
		//this.Bee = Bee;
		this.targetIndex = targetIndex;
	}
}
public class Msg_Win : MsgBase
{
	public Bee Bee;
	public Msg_Win(Bee Bee)
    {
        this.Bee = Bee;
    }
}
public class Msg_Lose : MsgBase
{
    public Bee Bee;
    public Msg_Lose(Bee Bee)
    {
        this.Bee = Bee;
    }
}
public class Msg_End : MsgBase
{

}
public class Msg_CardRotated : MsgBase
{
	public Card card;
	public Msg_CardRotated(Card c)
	{
		card = c;
	}
}
public class Msg_SetAdjacentBlock : MsgBase
{
	public AdjacentBlock block;
	public Card c;

    public Msg_SetAdjacentBlock(AdjacentBlock block, Card c)
	{
		this.block = block;
		this.c = c;
	}
}
public class Msg_PlaceCard : MsgBase
{
	public Card from;
	public Transform to;
	public float lerpSpeed = 1f;
	public Type nextStep;
	public eSide attachedSide = eSide.NONE;
	public Vector2Int coord;
    public Msg_PlaceCard()
    {
        coord = new Vector2Int(0, 0);
    }
    public Msg_PlaceCard(Vector2Int coord, Card from, Transform to, float lerpSpeed, Type nextStep, eSide attachedSide = eSide.NONE)
    {
        this.coord = coord;
        this.from = from;
        this.to = to;
        this.lerpSpeed = lerpSpeed;
		this.nextStep = nextStep;
		this.attachedSide = attachedSide;
    }
}
public class MsgBase
{

}
#endregion