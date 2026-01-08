using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static UnityEngine.UI.GridLayoutGroup;
using Random = UnityEngine.Random;

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
	[Header("Idle")]
	[Range(0f, 5f)]
	[SerializeField] float timeIdle_Waiting = 1f;
	[SerializeField] float rangeIdle_Roaming = 10f;
	[SerializeField] float speedIdle_Roaming = 2f;


	private void Awake()
	{
		sm = new SM<Bee>(this, (a) => {
			Debug.Log($"[Bee] SM<Bee>:: ChangeState: type = {a}");
			strCurState = a.ToString();
		});
		sm.RegisterState(new Idle(sm));
		sm.RegisterState(new Transport(sm));
		sm.RegisterState(new Combat(sm));
		sm.RegisterState(new Death(sm));

		sm.RegisterState(new Following(sm));
		sm.RegisterState(new Possessed(sm));
	}
    private void Start()
    {
        
    }
    public void Init(string name, int index)
	{
		playerName = name;

		idxHeroPosition = index;
		Card c = GameBoard.I.Get(index);
        GameMaster.I.PlaceObject(_trnHero, c.transform);

        sm.ChangeState(typeof(Idle));
    }
	public void Update()
	{
		sm.Update();
	}
	public void MsgProc(MsgBase m)
	{
		sm.MsgProc(m);

        #region - special case -
   //     if (m is Msg_End)
			//sm.ChangeState(typeof(End));
        #endregion
    }
	#region - state - 
	class Idle : SM<Bee>.BaseState, IState
	{
        Action aReservedState;

        public Idle(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{

		}
		public void Enter(MsgBase m)
		{
			owner.StartCoroutine(Roaming_CR());
		}
		public void Update()
		{

		}
		public void Exit()
		{
            GameMaster.I.aPlacingComplete -= OnPlacingComplete;
        }
		#endregion
		IEnumerator Roaming_CR()
		{
			Transform transform = owner.transform;
			while(true)
			{
				// 1. 목표 지점 결정 (무작위)
				Vector3 targetPos = transform.position + Random.insideUnitSphere * owner.rangeIdle_Roaming;
				targetPos.y = 0f;

                // 2. 이동 (Moving 상태)
                Debug.Log("벌이 이동 중...");
                while (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    // 부드럽게 이동
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

                    // 이동 방향 바라보기 (부드러운 회전)
                    Vector3 direction = targetPos - transform.position;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                    }

                    yield return null; // 다음 프레임까지 대기
                }

                // 3. 멈춤 및 대기 (Idle 상태)
                Debug.Log("벌이 멈춤 (IDLE)");
                yield return new WaitForSeconds(owner.timeIdle_Waiting);
            }
		}
        #region - outer callback -
        void OnPlacingComplete()
        {
			aReservedState?.Invoke();
        }
        #endregion
    }
	class Transport : SM<Bee>.BaseState, IState
	{
        Action aReservedState;

        public Transport(SM<Bee> sm) : base(sm) { }
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

				return;
			}

			GameMaster.I.PlaceObject(d.sc.transform, owner.trnHand, GameMaster.I.lerpSpeed);
			owner.listCard.Add(d.sc);
			//d.sc.OwnedByPlayer(owner);
		}
		void OnTurn_Attack(MsgBase m)
		{
			//aReservedState = () => sm.ChangeState(typeof(Turn_Attack));
		}
		void OnWaiting(MsgBase m)
		{
            //aReservedState = () => sm.ChangeState(typeof(Waiting));
        }
        #region - outer callback -
        void OnPlacingComplete()
        {
			aReservedState?.Invoke();
        }
        #endregion
    }
	class Combat : SM<Bee>.BaseState, IState
	{
		public Combat(SM<Bee> sm) : base(sm) { }
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

            Hand.I.CardClicked(cc.sc, true);
        }
		void OnDeselected(MsgBase m)
		{
            GameBoard.I.Clear();
            Hand.I.Deselect();
        }
		void OnMove(MsgBase m)
		{

        }
        void OnWaiting(MsgBase m)
        {
            //sm.ChangeState(typeof(Waiting));
        }
    }
    class Death : SM<Bee>.BaseState, IState
	{
		public Death(SM<Bee> sm) : base(sm) { }
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
	class Following : SM<Bee>.BaseState, IState
	{
		public Following(SM<Bee> sm) : base(sm) { }
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
			//sm.ChangeState(typeof(Turn_Attack));
		}
	}
	class Possessed : SM<Bee>.BaseState, IState
	{
		public Possessed(SM<Bee> sm) : base(sm) { }
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
			foreach (SabreCard node in owner.listCard)
			{
				GameMaster.I.PlaceObject(node.transform, owner.trnHand, GameMaster.I.lerpSpeed);
			}
			for (int i = 0; i < GameMaster.cntHandSize - owner.listCard.Count; ++i)
			{

			}
		}
		void OnWaiting(MsgBase m)
		{
			//sm.ChangeState(typeof(Waiting));
		}
	}
	#endregion
}
