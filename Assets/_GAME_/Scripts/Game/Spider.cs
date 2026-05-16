using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Spider : MonoBehaviour, IMsgProc
{
    public static List<Spider> allSpiders = new List<Spider>();

    public float webRadius = 10f;
    public float triggerRadius = 5f;
    public float chaseSpeed = 6f;
    public int hp = 100;
    public int maxHp = 100;

    public GameObject webVisual;
    public Vector3 webCenter;

    SM<Spider> sm;
    [SerializeField] public string strCurState = "";

    public Bee targetBee;

    void Awake()
    {
        if (!allSpiders.Contains(this)) allSpiders.Add(this);

        sm = new SM<Spider>(this, (a) =>
        {
            strCurState = a.ToString().Replace("Spider+", "");
        });

        sm.RegisterState(new Idle(sm));
        sm.RegisterState(new Chase(sm));
        sm.RegisterState(new Attack(sm));
    }

    void OnDestroy()
    {
        if (allSpiders.Contains(this)) allSpiders.Remove(this);
    }

    void Start()
    {
        webCenter = transform.position;
        if (webVisual != null)
        {
            webVisual.transform.position = webCenter;
            webVisual.SetActive(false);
        }
        sm.ChangeState(typeof(Idle));
    }

    void Update()
    {
        sm.Update();
    }

    public void MsgProc(MsgBase m)
    {
        ((IMsgProc)sm)?.MsgProc(m);

        if (m is Msg_TakeDamage msg)
        {
            hp -= msg.damage;
            if (hp <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    class Idle : SM<Spider>.BaseState, IState
    {
        public Idle(SM<Spider> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            if (owner.webVisual != null)
                owner.webVisual.SetActive(false);
        }
        
        public void Update()
        {
            // 중앙으로 복귀
            if (Vector3.Distance(owner.transform.position, owner.webCenter) > 0.1f)
            {
                owner.transform.position = Vector3.MoveTowards(owner.transform.position, owner.webCenter, owner.chaseSpeed * 0.5f * Time.deltaTime);
                Vector3 dir = (owner.webCenter - owner.transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, Quaternion.LookRotation(dir), 0.1f);
                }
            }

            // 벌 감지
            for (int i = 0; i < Bee.allBees.Count; i++)
            {
                var bee = Bee.allBees[i];
                if (bee == null || bee.hp <= 0 || bee.strCurState == "Death" || bee.strCurState == "Bind") continue;

                float dist = Vector3.Distance(owner.webCenter, bee.transform.position);
                if (dist <= owner.triggerRadius)
                {
                    bee.MsgProc(new Msg_Bind(owner));
                    owner.targetBee = bee;
                    sm.ChangeState(typeof(Chase));
                    return;
                }
            }
        }
        public void Exit() { }
    }

    class Chase : SM<Spider>.BaseState, IState
    {
        public Chase(SM<Spider> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            if (owner.webVisual != null)
                owner.webVisual.SetActive(true);
        }

        public void Update()
        {
            if (owner.targetBee == null || owner.targetBee.hp <= 0 || owner.targetBee.strCurState != "Bind")
            {
                sm.ChangeState(typeof(Idle));
                return;
            }

            // 속박된 벌을 향해 이동
            Vector3 targetPos = owner.targetBee.transform.position;
            float distToBee = Vector3.Distance(owner.transform.position, targetPos);

            if (distToBee <= 0.5f)
            {
                sm.ChangeState(typeof(Attack));
            }
            else
            {
                owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPos, owner.chaseSpeed * Time.deltaTime);
                Vector3 dir = (targetPos - owner.transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, Quaternion.LookRotation(dir), 0.1f);
                }
            }
        }
        public void Exit() { }
    }

    class Attack : SM<Spider>.BaseState, IState
    {
        public Attack(SM<Spider> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            if (owner.targetBee != null && owner.targetBee.hp > 0)
            {
                Vector3 hitDir = (owner.targetBee.transform.position - owner.transform.position).normalized;
                if (hitDir == Vector3.zero) hitDir = owner.transform.forward;
                owner.targetBee.MsgProc(new Msg_TakeDamage(9999, hitDir)); // 즉사 공격
            }
            sm.ChangeState(typeof(Idle));
        }
        public void Update() { }
        public void Exit() { }
    }
}

public class Msg_Bind : MsgBase
{
    public Spider sourceSpider;
    public Msg_Bind(Spider spider)
    {
        this.sourceSpider = spider;
    }
}
