using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;

public class GameMaster : MonoBehaviour
{
	public const int cntHandSize = 5;
	public const int idxInitHeroPositionL = 1;
	public const int idxInitHeroPositionR = 23;
	public const int idxCenter = 12;

	public static GameMaster I { get; private set; }

	public CardSeat cardSeatBoard;
	public CardPile cardPile_SabreCard;
	public CardPile cardPile_SabreCard_Discard;
	public TMP_Text txtResult;
	public Bee curActionPlayer;
	public Bee curOppositePlayer;

	SM<GameMaster> sm;

	public Bee playerL;
	public Bee playerR;

	[SerializeField] float drawSpeed = 0.2f;
	[SerializeField] public float lerpSpeed = 0.1f;

	[SerializeField] string strCurState = "";

	public Action aPlacingComplete;

	void Awake()
	{
		I = this;

		sm = new SM<GameMaster>(this, (a) => {
			Debug.Log($"[GameMaster] SM<GameMaster>:: ChangeState: type = {a}");
			strCurState = a.ToString();
		});
		sm.RegisterState(typeof(Proc_Intro), new Proc_Intro(sm));
		sm.RegisterState(typeof(Proc_WaitingInput_PlayerAction), new Proc_WaitingInput_PlayerAction(sm));
		sm.RegisterState(typeof(Proc_ChangeTurn), new Proc_ChangeTurn(sm));
		sm.RegisterState(typeof(Proc_Result), new Proc_Result(sm));
		//sm.RegisterState(typeof(Proc_DrawSabreCard_Indoor), new Proc_DrawSabreCard_Indoor(sm));
		//sm.RegisterState(typeof(Proc_Outdoor), new Proc_Outdoor(sm));
		//sm.RegisterState(typeof(Proc_WaitingInput_PlacingSabreCard), new Proc_WaitingInput_PlacingSabreCard(sm));
		//sm.RegisterState(typeof(Proc_PlacingCard), new Proc_PlacingCard(sm));
	}
	void Start()
	{
		sm.ChangeState(typeof(Proc_Intro));
	}
	void Update()
	{
		sm.Update();
	}
	public void MsgProc(MsgBase m)
	{
		sm.MsgProc(m);
	}
	#region - state -
	public class Proc_Intro : SM<GameMaster>.BaseState, IState
	{
		public Proc_Intro(SM<GameMaster> sm) : base(sm) { }
		public void Enter(MsgBase m)
		{
            GameMaster.I.StartCoroutine(_SetFirstSabreCard_CR());
		}
		IEnumerator _SetFirstSabreCard_CR()
		{
			owner.cardPile_SabreCard.Shuffle();

			owner.playerL.Init("Left Bee", idxInitHeroPositionL);
			owner.playerR.Init("Right Bee", idxInitHeroPositionR);

			for(int i = 0; i < cntHandSize; ++i)
			{
				if (GameMaster.I.cardPile_SabreCard.Draw(out Card c) == false)
				{
					Debug.LogError($"[GameMaster] Proc_Intro:: _SetFirstSabreCard_CR: no card");
					yield break;
				}

				SabreCard sc = c as SabreCard;
				//GameMaster.I.StartCoroutine(owner.Place_CR(sc.transform, owner.playerL.trnHand, owner.lerpSpeed));
				yield return new WaitForSeconds(owner.drawSpeed);

				owner.playerL.MsgProc(new Msg_Draw(sc));

                if (GameMaster.I.cardPile_SabreCard.Draw(out c) == false)
                {
                    Debug.LogError($"[GameMaster] Proc_Intro:: _SetFirstSabreCard_CR: no card");
                    yield break;
                }

                sc = c as SabreCard;
                //GameMaster.I.StartCoroutine(owner.Place_CR(sc.transform, owner.playerR.trnHand, owner.lerpSpeed));
                yield return new WaitForSeconds(owner.drawSpeed);

                owner.playerR.MsgProc(new Msg_Draw(sc));
            }

            owner.curActionPlayer = owner.playerL;
            owner.curOppositePlayer = owner.playerR;

            owner.playerL.MsgProc(new Msg_Turn_Attack());
			owner.playerR.MsgProc(new Msg_Waiting());

			sm.ChangeState(typeof(Proc_WaitingInput_PlayerAction));
		}
        public void Update() { }
		public void Exit() { }
	}
	public class Proc_WaitingInput_PlayerAction : SM<GameMaster>.BaseState, IState
	{
		public Proc_WaitingInput_PlayerAction(SM<GameMaster> sm) : base(sm) { }
		#region - interface -
		public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{
            ddic[GetType()].Add(typeof(Msg_Move), OnMove);
            ddic[GetType()].Add(typeof(Msg_Lose), OnLose);
        }
        public void Enter(MsgBase m)
		{
			if (InputControl.it != null)
			{
				InputControl.it.aMouseEnter += HandleMouseEnter;
				InputControl.it.aMouseExit += HandleMouseExit;
				InputControl.it.aMouseClick += HandleMouseClick;
			}
			else
			{
				Debug.LogError($"[GameMaster] Proc_WaitingInput_PlayerAction:: Enter: InputControl.it == null");
			}

			//Msg_Draw draw = m as Msg_Draw;
			//owner.drawnSabreCard = draw.mc;
		}
		public void Update()
		{

		}
		public void Exit()
		{
			if (InputControl.it != null)
			{
				InputControl.it.aMouseEnter -= HandleMouseEnter;
				InputControl.it.aMouseExit -= HandleMouseExit;
				InputControl.it.aMouseClick -= HandleMouseClick;
			}
			else
			{
				Debug.LogError($"[GameMaster] Proc_WaitingInput_PlayerAction:: Exit: InputControl.it == null");
			}
		}
		#endregion
		#region - input -
		void HandleMouseEnter(GameObject obj)
		{
			//Debug.Log($"[GameMaster] Proc_WaitingInput:: HandleMouseEnter: ENTER = {obj.name}");
		}
		void HandleMouseExit(GameObject obj)
		{
			//Debug.Log($"[GameMaster] Proc_WaitingInput:: HandleMouseExit: EXIT = {obj.name}");
		}
		void HandleMouseClick(GameObject obj)
		{
			if(obj == null)
			{
				owner.curActionPlayer?.MsgProc(new Msg_Deselected());

				return;
			}

			Debug.Log($"[GameMaster] Proc_WaitingInput_PlayerAction:: HandleMouseClick: CLICK = {obj}");

			switch (obj.tag)
			{
				case Tag.Board:
                    Card c = obj.GetComponent<Card>();
					c?.Selected();

                    break;
				case Tag.SabreCard:
					SabreCard sc = obj.GetComponent<SabreCard>();
					sc?.Clicked();
                    
                    break;
			}
		}
		#endregion
		#region - msg -
		void OnMove(MsgBase m)
		{
			Msg_Move mm = m as Msg_Move;
            owner.curActionPlayer?.MsgProc(m);

            if (mm.targetIndex == owner.curOppositePlayer.idxHeroPosition)
			{
                sm.ChangeState(typeof(Proc_Result));
				sm.MsgProc(new Msg_Win(owner.curActionPlayer));
            }
			else
			{
                sm.ChangeState(typeof(Proc_ChangeTurn));
            }
        }
        void OnLose(MsgBase m)
        {
            Msg_Move mm = m as Msg_Move;
            owner.curActionPlayer?.MsgProc(m);

            if (mm.targetIndex == owner.curOppositePlayer.idxHeroPosition)
            {
                sm.ChangeState(typeof(Proc_Result));
                sm.MsgProc(new Msg_Win(owner.curActionPlayer));
            }
            else
            {
                sm.ChangeState(typeof(Proc_ChangeTurn));
            }
        }
        #endregion
    }
    public class Proc_ChangeTurn : SM<GameMaster>.BaseState, IState
    {
        public Proc_ChangeTurn(SM<GameMaster> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            ddic[GetType()].Add(typeof(Msg_Lose), OnLose);
        }
        public void Enter(MsgBase m)
        {
            Debug.Log($"[GameMaster] Proc_ChangeTurn:: Enter: ");

            if(owner.cardPile_SabreCard.RemainCount() == 0)
            {
                int pL = idxCenter - owner.playerL.idxHeroPosition;
                int pR = owner.playerR.idxHeroPosition - idxCenter;

                sm.ChangeState(typeof(Proc_Result));

                if (pL > pR)
                    sm.MsgProc(new Msg_Win(owner.playerL));
                else if(pL < pR)
                    sm.MsgProc(new Msg_Win(owner.playerR));
                else
                {
                    Debug.LogWarning($"[GameMaster] Proc_ChangeTurn:: same position?");
                }    
            }
            else
            {
                if(owner.curOppositePlayer.CheckMovable(owner.curActionPlayer.idxHeroPosition) == true)
                {
                    Bee temp = owner.curActionPlayer;
                    owner.curActionPlayer = owner.curOppositePlayer;
                    owner.curOppositePlayer = temp;

                    owner.curActionPlayer.MsgProc(new Msg_Turn_Attack());
                    owner.curOppositePlayer.MsgProc(new Msg_Waiting());

                    sm.ChangeState(typeof(Proc_WaitingInput_PlayerAction));
                }
                else
                {
                    sm.ChangeState(typeof(Proc_Result));
                    sm.MsgProc(new Msg_Win(owner.curActionPlayer));
                }
            }
        }
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
        #region - input -
        void HandleMouseEnter(GameObject obj)
        {
            //Debug.Log($"[GameMaster] Proc_WaitingInput:: HandleMouseEnter: ENTER = {obj.name}");
        }
        void HandleMouseExit(GameObject obj)
        {
            //Debug.Log($"[GameMaster] Proc_WaitingInput:: HandleMouseExit: EXIT = {obj.name}");
        }
        void HandleMouseClick(GameObject obj)
        {

        }
        #endregion
        #region - msg -
        void OnLose(MsgBase m)
        {
            sm.ChangeState(typeof(Proc_Result));
            sm.MsgProc(new Msg_Win(owner.curOppositePlayer));
        }
        #endregion
    }
    public class Proc_Result : SM<GameMaster>.BaseState, IState
    {
        public Proc_Result(SM<GameMaster> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
            ddic[GetType()].Add(typeof(Msg_Win), OnWin);
        }
        public void Enter(MsgBase m)
        {
            owner.playerL.MsgProc(new Msg_End());
            owner.playerR.MsgProc(new Msg_End());
        }
        public void Update()
        {

        }
        public void Exit()
        {

        }
        #endregion
        #region - msg -
        void OnWin(MsgBase m)
        {
            Msg_Win w = m as Msg_Win;
            owner.txtResult.text = w.Bee.playerName + "' WIN";
        }
        #endregion
    }
	#region - method -
	int cntPlacingCR = 0;
    public void PlaceObject(Transform trn, Transform target, float lerpSpeed = 1f, Transform root = null)
	{
		StartCoroutine(Place_CR(trn, target, lerpSpeed, root));
    }
    IEnumerator Place_CR(Transform trn, Transform target, float lerpSpeed = 1f, Transform root = null)
    {
		++cntPlacingCR;

        trn.transform.SetParent(null);
        float ratio = 0f;

        Quaternion targetQ = Quaternion.Euler(target.eulerAngles);
        while (true)
        {
            trn.position = Vector3.Slerp(trn.position, target.position, ratio);
            trn.rotation = Quaternion.Slerp(trn.rotation, targetQ, ratio);
            if ((trn.position - target.position).sqrMagnitude < 0.0001f)
            {
                break;
            }

            yield return null;
            ratio += Time.deltaTime * lerpSpeed;
        }

        if (root == null)
        {
            trn.SetParent(target);
            trn.localPosition = Vector3.zero;
        }
        else
        {
            trn.SetParent(root);
            trn.position = target.position;
        }

        --cntPlacingCR;

		if(cntPlacingCR == 0)
			aPlacingComplete?.Invoke();
    }
    #endregion
}
#endregion