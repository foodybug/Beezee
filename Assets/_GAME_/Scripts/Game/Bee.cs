using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
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

	[SerializeField] Colony colony;

	//[SerializeField] Transform _trnHand; public Transform trnHand { get { return _trnHand; } }
	//[SerializeField] Transform _trnHero; public Transform trnHero {  get { return _trnHero; } }
	//[SerializeField] Transform _trnTurn;
	//public int idxHeroPosition = -1;
	public string playerName;
	public bool isPlayer = false;

	//[SerializeField] List<SabreCard> listCard = new List<SabreCard>();
	[Header("Idle")]
	[Range(0f, 5f)]
	[SerializeField] float timeIdle_Waiting = 1f;
	[SerializeField] float rangeIdle_Roaming = 10f;
	[SerializeField] float speedIdle_Roaming = 2f;
	[SerializeField] float rangeIdle_DetectingFood = 3f;
	[SerializeField] float timeIdle_DetectingFood = 3f;
    //[SerializeField] float timeIdle_ApproachingFood = 2f;
    //[SerializeField] float timeIdle_EncounterFood = 0.2f;
    [Header("Transport")]
    [Range(0f, 5f)]
    [SerializeField] float timeTransport_ChangeDirection = 1f;

    private void Awake()
	{
		sm = new SM<Bee>(this, (a) => {
			//Debug.Log($"[Bee] SM<Bee>:: ChangeState: type = {a}");
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
    public void Init(string name, bool isPlayer = false)
	{
		playerName = name;

		//idxHeroPosition = index;
		//Card c = GameBoard.I.Get(index);
  //      GameMaster.I.PlaceObject(_trnHero, c.transform);

		if(isPlayer == true)
			sm.ChangeState(typeof(Possessed));
		else
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
		Coroutine crRoaming;
		Coroutine crDetectingFood;

		Vector3 targetPos;

        public Idle(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{

		}
		public void Enter(MsgBase m)
		{
            crRoaming = owner.StartCoroutine(Roaming_CR());
            crDetectingFood = owner.StartCoroutine(DetectingFood_CR());
		}
		public void Update()
		{

        }
		public void Exit()
		{
			owner.StopCoroutine(crRoaming);
			owner.StopCoroutine(crDetectingFood);
        }
		#endregion
		IEnumerator Roaming_CR()
		{
			Transform transform = owner.transform;
			while(true)
			{
				targetPos = transform.position + Random.insideUnitSphere * owner.rangeIdle_Roaming;
				targetPos.y = 0f;

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

                yield return new WaitForSeconds(owner.timeIdle_Waiting + Random.Range(-0.5f, 0.5f));
            }
		}
        IEnumerator DetectingFood_CR()
        {
            Transform transform = owner.transform;

			while(true)
			{
				yield return new WaitForSeconds(owner.timeIdle_DetectingFood);

                Collider[] c = Physics.OverlapSphere(transform.position, owner.rangeIdle_DetectingFood, LayerMask.GetMask("Flower"));
				if(c != null && c.Length > 0)
                {
					int index = Random.Range(0, c.Length);
					targetPos = c[index].transform.position;
                    targetPos.y = 0f;

                    owner.StopCoroutine(crRoaming);

					break;
                }
            }

            while (true)
            {
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

				sm.ChangeState(typeof(Transport));
				break;
            }
        }
    }
	class Transport : SM<Bee>.BaseState, IState
	{
		Coroutine crTransporting;
        public Transport(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
		{

		}
		public void Enter(MsgBase m)
		{
            crTransporting = owner.StartCoroutine(Transporting_CR());
        }
		public void Update()
		{

		}
		public void Exit()
		{
			owner.StopCoroutine(crTransporting);
        }
		#endregion
		IEnumerator Transporting_CR()
		{
			Transform transform = owner.transform;
			Vector3 targetPos = owner.colony.transform.position;
			targetPos.y = 0f;

			while (true)
			{
				float dist = Vector3.Magnitude(targetPos - transform.position);
				if (dist < 1f)
					break;

				targetPos += Vector3.one * dist;// * 0.5f;

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

				//yield return new WaitForSeconds(owner.timeTransport_ChangeDirection + Random.Range(-0.5f, 0.5f));
			}

			targetPos = owner.colony.transform.position;
			while (Vector3.Distance(transform.position, owner.colony.transform.position) > 0.1f)
			{
				// 부드럽게 이동
				transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

				// 이동 방향 바라보기 (부드러운 회전)
				Vector3 direction = targetPos - transform.position;
				if (direction != Vector3.zero)
				{
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
				}

				yield return null;
			}

			sm.ChangeState(typeof(Idle));
		}
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

		}
		public void Update()
		{

		}
		public void Exit()
		{

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
		public bool isClicking = false;
		public Vector3 destPoint;
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
			InputControl.I.aMouseClicking += OnClicking;
			InputControl.I.aMouseClickUp += OnClickUp;

			CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
			cf.enabled = true;
			cf.target = owner.transform;
		}
		public void Update()
		{
			if (isClicking == false)
				return;

			Transform transform = owner.transform;

			Vector3 targetPos = destPoint;
			targetPos.y = 0f;

			if(Vector3.Distance(transform.position, targetPos) > 0.1f)
			{
				// 부드럽게 이동
				transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

				// 이동 방향 바라보기 (부드러운 회전)
				Vector3 direction = targetPos - transform.position;
				if (direction != Vector3.zero)
				{
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
				}
			}
		}
		public void Exit()
		{
			InputControl.I.aMouseClicking -= OnClicking;
			InputControl.I.aMouseClickUp -= OnClickUp;
		}
		#endregion
		#region - input -
		void OnClicking(GameObject obj, Vector3 point)
		{
			if (isClicking == false)
				isClicking = true;

			destPoint = point;
		}
		void OnClickUp(GameObject obj)
		{
			isClicking = false;
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
	#endregion
}
