using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class Bee : MonoBehaviour
{
	SM<Bee> sm;
	[SerializeField] string strCurState = "";

	[SerializeField] Transform _trnHand; public Transform trnHand { get { return _trnHand; } }
	[SerializeField] Transform _trnHero; public Transform trnHero {  get { return _trnHero; } }
	[SerializeField] Transform _trnTurn;
	public int idxHeroPosition = -1;
	public string playerName;

	[SerializeField] List<SabreCard> listCard = new List<SabreCard>();

	private void Awake()
	{
		sm = new SM<Bee>(this, (a) => {
			Debug.Log($"[Bee] SM<Bee>:: ChangeState: type = {a}");
			strCurState = a.ToString();
		});
		sm.RegisterState(new Idle(sm));
		sm.RegisterState(new Waiting(sm));
		sm.RegisterState(new Turn_Attack(sm));
		sm.RegisterState(new Turn_Defence(sm));
		sm.RegisterState(new End(sm));
    }
    private void Start()
    {
        
    }
    public void Init(string name, int index)
	{
		playerName = name;

        DiscardAll();

		idxHeroPosition = index;
		Card c = GameBoard.I.Get(index);
        GameMaster.I.PlaceObject(_trnHero, c.transform);

        sm.ChangeState(typeof(Idle));
    }
	public void MsgProc(MsgBase m)
	{
		sm.MsgProc(m);

        #region - special case -
        if (m is Msg_End)
			sm.ChangeState(typeof(End));
        #endregion
    }
    public void DiscardAll()
	{
		foreach(SabreCard node in listCard)
		{
			GameMaster.I.cardPile_SabreCard_Discard.Put(node);
		}

        listCard.Clear();
    }
	public bool CheckMovable(int idxOpposistePosition)
	{
        bool movable = false;
        foreach (SabreCard node in listCard)
        {
            movable = movable | GameBoard.I.CheckMovable(idxHeroPosition, node.number, idxOpposistePosition);
        }

		return movable;

        //if (movable == false)
        //{
        //    GameMaster.I.MsgProc(new Msg_Lose(this));
        //}
    }
	#region - state - 
	class Idle : SM<Bee>.BaseState, IState
	{
        Action aReservedState;

        public Idle(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{
			//ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
			ddic[GetType()].Add(typeof(Msg_Draw), OnDraw);
			ddic[GetType()].Add(typeof(Msg_Turn_Attack), OnTurn_Attack);
			ddic[GetType()].Add(typeof(Msg_Waiting), OnWaiting);
		}
		public void Enter(MsgBase m)
		{
            GameMaster.I.aPlacingComplete += OnPlacingComplete;

            Hand.I.Show(false);
		}
		public void Update()
		{

		}
		public void Exit()
		{
            GameMaster.I.aPlacingComplete -= OnPlacingComplete;
        }
		#endregion
		void OnDraw(MsgBase m)
		{
			Msg_Draw d = m as Msg_Draw;
			if (owner.listCard.Count > GameMaster.cntHandSize)
			{
				Debug.LogError($"[Bee] Drawing:: OnDraw: size over");
				GameMaster.I.cardPile_SabreCard_Discard.Put(d.sc);
				return;
			}

			GameMaster.I.PlaceObject(d.sc.transform, owner.trnHand, GameMaster.I.lerpSpeed);
			owner.listCard.Add(d.sc);
			//d.sc.OwnedByPlayer(owner);
		}
		void OnTurn_Attack(MsgBase m)
		{
			aReservedState = () => sm.ChangeState(typeof(Turn_Attack));
		}
		void OnWaiting(MsgBase m)
		{
            aReservedState = () => sm.ChangeState(typeof(Waiting));
        }
        #region - outer callback -
        void OnPlacingComplete()
        {
			aReservedState?.Invoke();
        }
        #endregion
    }
    class Waiting : SM<Bee>.BaseState, IState
    {
        public Waiting(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            ddic[GetType()].Add(typeof(Msg_Turn_Attack), OnTurn_Attack);
        }
        public void Enter(MsgBase m)
        {
			owner._trnTurn.gameObject.SetActive(false);
        }
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
        void OnTurn_Attack(MsgBase m)
        {
            sm.ChangeState(typeof(Turn_Attack));
        }
    }
    class Turn_Attack : SM<Bee>.BaseState, IState
	{
		public Turn_Attack(SM<Bee> sm) : base(sm) { }
		#region - interface -
		public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{
			//ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
			ddic[GetType()].Add(typeof(Msg_CardClicked), OnCardClicked);
			ddic[GetType()].Add(typeof(Msg_Deselected), OnDeselected);
			ddic[GetType()].Add(typeof(Msg_Move), OnMove);
            ddic[GetType()].Add(typeof(Msg_Waiting), OnWaiting);
        }
		public void Enter(MsgBase m)
		{
			Hand.I.Show(true);
			Hand.I.Set(owner.listCard);

            owner._trnTurn.gameObject.SetActive(true);
		}
		public void Update()
		{

		}
		public void Exit()
		{
            owner._trnTurn.gameObject.SetActive(false);
        }
        #endregion
        void OnCardClicked(MsgBase m)
		{
			Msg_CardClicked cc = m as Msg_CardClicked;
			GameBoard.I.HighLight(owner.idxHeroPosition, cc.sc.number, GameMaster.I.curOppositePlayer.idxHeroPosition, true);
            Hand.I.CardClicked(cc.sc, true);
        }
		void OnDeselected(MsgBase m)
		{
            GameBoard.I.Clear();
            Hand.I.Deselect();
        }
		void OnMove(MsgBase m)
		{
            Msg_Move mm = m as Msg_Move;
			owner.idxHeroPosition = mm.targetIndex;

            Card c = GameBoard.I.Get(mm.targetIndex);
            GameMaster.I.PlaceObject(owner.trnHero, c.transform);

			GameBoard.I.Clear();
			Hand.I.DiscardUsedCard(owner.listCard);
			foreach(SabreCard node in owner.listCard)
			{
                GameMaster.I.PlaceObject(node.transform, owner.trnHand, GameMaster.I.lerpSpeed);
            }
			for(int i=0; i < GameMaster.cntHandSize - owner.listCard.Count; ++i)
			{
				GameMaster.I.cardPile_SabreCard.Draw(out Card d);
				if(d != null)
				{
                    SabreCard sc = d as SabreCard;
                    owner.listCard.Add(sc);
                    //sc.OwnedByPlayer(owner);
                    GameMaster.I.PlaceObject(d.transform, owner.trnHand, GameMaster.I.lerpSpeed);
                }
            }
        }
        void OnWaiting(MsgBase m)
        {
            sm.ChangeState(typeof(Waiting));
        }
    }
    class Turn_Defence : SM<Bee>.BaseState, IState
	{
		public Turn_Defence(SM<Bee> sm) : base(sm) { }
		#region - interface -
		public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{
			//ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
			ddic[GetType()].Add(typeof(Msg_CardRotated), OnCardRotated);
			ddic[GetType()].Add(typeof(Msg_Deselected), OnDeselect);
			ddic[GetType()].Add(typeof(Msg_SetAdjacentBlock), OnSetAdjacentBlock);
		}
		public void Enter(MsgBase m)
		{

		}
		public void Update()
		{

		}
		public void Exit()
		{

		}
		#endregion
		void OnCardRotated(MsgBase m)//드로잉 카드에 따라 리프레시
		{
			Msg_CardRotated cr = m as Msg_CardRotated;
			SabreCard drawingSabreCard = cr.card as SabreCard;
			//Debug.Log($"SabreCard:: OnCardRotated: drawingSabreCard.direction = {drawingSabreCard.direction} ------");


			Debug.Log($"------ SabreCard:: OnCardRotated: ");
		}
		void OnDeselect(MsgBase m)
		{

		}
		void OnSetAdjacentBlock(MsgBase m)
		{

		}
	}
    class End : SM<Bee>.BaseState, IState
    {
        public End(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            ddic[GetType()].Add(typeof(Msg_End), OnEnd);
        }
        public void Enter(MsgBase m)
        {

        }
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
        void OnEnd(MsgBase m)//드로잉 카드에 따라 리프레시
        {

        }
    }
    #endregion
}
