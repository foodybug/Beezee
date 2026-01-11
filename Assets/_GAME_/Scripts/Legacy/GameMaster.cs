using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEditor.Timeline;
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

	[SerializeField] TMP_Text txtResult;

	public Bee curActionPlayer;

	SM<GameMaster> sm;

	public Bee playerBee;
	public List<Colony> listColony;
	public List<Flower> listFlower;
	[SerializeField] string strCurState = "";
	
	[SerializeField] float drawSpeed = 0.2f;
	[SerializeField] public float lerpSpeed = 0.1f;

	public Action aPlacingComplete;
	[SerializeField] PrefabContainer pc;

	void Awake()
	{
		I = this;

		sm = new SM<GameMaster>(this, (a) => {
			Debug.Log($"[GameMaster] SM<GameMaster>:: ChangeState: type = {a}");
			strCurState = a.ToString();
		});
		sm.RegisterState(new Proc_Intro(sm));
		sm.RegisterState(new Proc_Playing(sm));
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
			Bee bee = owner.playerBee = Instantiate(owner.pc.bee);
			bee.Init("Player", true);

			for(int i=0; i<100; ++i)
			{
				bee = Instantiate(owner.pc.bee);
				bee.Init("Npc");
			}
		}
        public void Update() { }
		public void Exit() { }
	}
	public class Proc_Playing : SM<GameMaster>.BaseState, IState
	{
		public Proc_Playing(SM<GameMaster> sm) : base(sm) { }
		#region - interface -
		public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{

        }
        public void Enter(MsgBase m)
		{
			if (InputControl.I != null)
			{
				InputControl.I.aMouseEnter += HandleMouseEnter;
				InputControl.I.aMouseExit += HandleMouseExit;
				InputControl.I.aMouseClickDown += HandleMouseClick;
			}
			else
			{
				Debug.LogError($"[GameMaster] Proc_WaitingInput_PlayerAction:: Enter: InputControl.I == null");
			}

			//Msg_Draw draw = m as Msg_Draw;
			//owner.drawnSabreCard = draw.mc;
		}
		public void Update()
		{

		}
		public void Exit()
		{
			if (InputControl.I != null)
			{
				InputControl.I.aMouseEnter -= HandleMouseEnter;
				InputControl.I.aMouseExit -= HandleMouseExit;
				InputControl.I.aMouseClickDown -= HandleMouseClick;
			}
			else
			{
				Debug.LogError($"[GameMaster] Proc_WaitingInput_PlayerAction:: Exit: InputControl.I == null");
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
	#region - inner class -
	[Serializable]
	class PrefabContainer
	{
		public Bee bee;
	}
	#endregion
}
#endregion