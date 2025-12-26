using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

public class SabreCard : Card
{
    SM<SabreCard> sm;

    Player playerOwned;
    [SerializeField] int _number = 0; public int number { get { return _number; } }
    [SerializeField] TMP_Text txt;
    void Awake()
	{
        txt.text = _number.ToString();

        Init();

        sm = new SM<SabreCard>(this, (a) => {
            //Debug.Log($"[SabreCard] SM<SabreCard>:: ChangeState: type = {a}");
            strCurState = a.ToString();
        });
        sm.RegisterState(typeof(Drawing), new Drawing(sm));
        sm.RegisterState(typeof(Idle), new Idle(sm));
        sm.RegisterState(typeof(Selecting), new Selecting(sm));
        sm.ChangeState(typeof(Drawing));
    }
    //public new void Init(int num)
    //{
    //    _number = num;
    //    txt.text = num.ToString();
    //}
    public void OwnedByPlayer(Player p)
    {
        playerOwned = p;
    }
    public void Clicked()
    {
        playerOwned?.MsgProc(new Msg_CardClicked(this));
    }
    //public new void MsgProc(MsgBase m)
    //{
    //    sm.MsgProc(m);
    //}
    protected void OnAdjacentBlockSelected(AdjacentBlock block, Card c)
    {
        sm.MsgProc(new Msg_SetAdjacentBlock(block, c));
    }
    //public bool CheckConnectableAbsolutely(eDirection d)
    //{
    //    eDirection realSide = Direction.GetCounterDirection(d);
    //    int sideIndex = (int)Direction.GetActualDirection(direction, d);

    //    return listAdjacentBlock.Count > sideIndex && listAdjacentBlock[sideIndex] != null;
    //}
	#region - state - 
	class Drawing : SM<SabreCard>.BaseState, IState
	{
		public Drawing(SM<SabreCard> sm) : base(sm) { }
		#region - interface -
		public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{
			//ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
			//ddic[GetType()].Add(typeof(Msg_CardRotated), OnCardRotated);
			//ddic[GetType()].Add(typeof(Msg_PlaceCard), OnPlaceCard);
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
		void OnCardRotated(MsgBase m)
        {
			//eDirection dir = Direction.Rotate_ClockWise(owner.direction);
			//float angle = Direction.GetAngle(dir);
   //         owner.transform.eulerAngles = new Vector3(0f, 0f, angle);
   //         owner._direction = dir;
        }
		void OnPlaceCard(MsgBase m)
		{
            Msg_PlaceCard pc = m as Msg_PlaceCard;
            owner.SetCoord(pc.coord);
            owner._number = pc.coord.x;

            GameBoard.I.Set(pc.coord, pc.from);

			sm.ChangeState(typeof(Idle));
		}
	}
	class Idle : SM<SabreCard>.BaseState, IState
    {
        public Idle(SM<SabreCard> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            //ddic.Add(GetType(), new Dictionary<Type, Action<MsgBase>>());
            //ddic[GetType()].Add(typeof(Msg_CardClicked), OnCardClicked);
            ddic[GetType()].Add(typeof(Msg_Selected), OnSelected);
        }     
        public void Enter(MsgBase m)
        {
            owner.HighLight(false);
        }     
        public void Update()
        {

        }     
        public void Exit()
        {

        }
        #endregion
        void OnSelected(MsgBase m)
        {
            sm.ChangeState(typeof(Selecting));
        }
    }
	class Selecting : SM<SabreCard>.BaseState, IState
    {
        public Selecting(SM<SabreCard> sm):base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
			ddic[GetType()].Add(typeof(Msg_Deselected), OnDeselected);
		}
        public void Enter(MsgBase m)
        {
			owner.HighLight(true);
		}
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
		void OnDeselected(MsgBase m)
        {
            sm.ChangeState(typeof(Idle));
        }
    }
    #endregion
}
