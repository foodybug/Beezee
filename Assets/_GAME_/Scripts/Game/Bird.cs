using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Bird : MonoBehaviour, IMsgProc
{
    public float flightHeight = 15f;
    public float roamSpeed = 1.0f;
    public float diveSpeed = 4.0f;
    public float detectionRange = 30f;
    
    public Bee targetBee;

    public int hp = 100;
    public int maxHp = 100;

    public static List<Bird> allBirds = new List<Bird>();

    SM<Bird> sm;
    [SerializeField] public string strCurState = "";

    void Awake()
    {
        if (!allBirds.Contains(this)) allBirds.Add(this);

        sm = new SM<Bird>(this, (a) =>
        {
            strCurState = a.ToString().Replace("Bird+", "");
        });
        sm.RegisterState(new Idle(sm));
        sm.RegisterState(new Attack(sm));
        sm.RegisterState(new RunAway(sm));
    }

    void OnDestroy()
    {
        if (allBirds.Contains(this)) allBirds.Remove(this);
    }

    void Start()
    {
        flightHeight = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 5f;
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
            if (hp <= 0 || strCurState == "RunAway") return; // 체력이 없거나 도망가는 중이면 데미지 무시

            hp -= msg.damage;
        }
    }

    class Idle : SM<Bird>.BaseState, IState
    {
        Coroutine crRoaming;
        Coroutine crDetectingBee;
        Vector3 targetPos;

        public Idle(SM<Bird> sm) : base(sm) { }

        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }

        public void Enter(MsgBase m)
        {
            GetNewWaypoint();
            crRoaming = owner.StartCoroutine(Roaming_CR());
            crDetectingBee = owner.StartCoroutine(DetectingBee_CR());
        }

        public void Update() { }

        public void Exit()
        {
            if (crRoaming != null) owner.StopCoroutine(crRoaming);
            if (crDetectingBee != null) owner.StopCoroutine(crDetectingBee);
        }

        void GetNewWaypoint()
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 20f;
            targetPos = new Vector3(owner.transform.position.x + randomCircle.x, owner.flightHeight, owner.transform.position.z + randomCircle.y);
            
            if (Environment.Instance != null)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, -Environment.Instance.worldSize.x, Environment.Instance.worldSize.x);
                targetPos.z = Mathf.Clamp(targetPos.z, -Environment.Instance.worldSize.y, Environment.Instance.worldSize.y);
            }
        }

        IEnumerator Roaming_CR()
        {
            Transform transform = owner.transform;
            while (true)
            {
                if (Vector3.Distance(transform.position, targetPos) < 1f)
                {
                    if (owner.hp <= 0)
                    {
                        sm.ChangeState(typeof(RunAway));
                        yield break;
                    }
                    GetNewWaypoint();
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.roamSpeed * Time.deltaTime);
                Vector3 dir = targetPos - transform.position;
                dir.y = 0f;
                if (dir != Vector3.zero) 
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f);
                }

                yield return null;
            }
        }

        IEnumerator DetectingBee_CR()
        {
            while (true)
            {
                if (owner.hp <= 0) yield break;
                yield return new WaitForSeconds(0.2f);
                
                float closestDist = float.MaxValue;
                Bee closestBee = null;

                foreach (var bee in Bee.allBees)
                {
                    if (bee == null || bee.hp <= 0 || bee.strCurState == "Death") continue;
                    
                    Vector3 birdPosFlat = owner.transform.position; birdPosFlat.y = 0;
                    Vector3 beePosFlat = bee.transform.position; beePosFlat.y = 0;
                    float dist = Vector3.Distance(birdPosFlat, beePosFlat);
                    if (dist < owner.detectionRange && dist < closestDist)
                    {
                        closestDist = dist;
                        closestBee = bee;
                    }
                }

                if (closestBee != null)
                {
                    owner.targetBee = closestBee;
                    sm.ChangeState(typeof(Attack));
                    yield break;
                }
            }
        }
    }

    class Attack : SM<Bird>.BaseState, IState
    {
        Coroutine crAttack;

        public Attack(SM<Bird> sm) : base(sm) { }

        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }

        public void Enter(MsgBase m)
        {
            crAttack = owner.StartCoroutine(Attack_CR());
        }

        public void Update() { }

        public void Exit()
        {
            if (crAttack != null) owner.StopCoroutine(crAttack);
        }

        IEnumerator Attack_CR()
        {
            Transform transform = owner.transform;
            Bee target = owner.targetBee;

            if (target != null)
            {
                target.MsgProc(new Msg_Stun(5f));
            }

            float attackTime = 5f;
            float elapsed = 0f;

            while (elapsed < attackTime)
            {
                elapsed += Time.deltaTime;

                if (target != null && target.hp > 0 && target.strCurState != "Death")
                {
                    Vector3 divePos = target.transform.position;
                    divePos.y = owner.flightHeight;
                    transform.position = Vector3.MoveTowards(transform.position, divePos, owner.diveSpeed * Time.deltaTime);
                    Vector3 dir = divePos - transform.position;
                    dir.y = 0f;
                    if (dir != Vector3.zero) 
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.2f);
                    }
                }

                yield return null;
            }

            if (target != null && target.hp > 0 && target.strCurState != "Death")
            {
                Vector3 posFlat = transform.position; posFlat.y = 0;
                Vector3 targetFlat = target.transform.position; targetFlat.y = 0;
                if (Vector3.Distance(posFlat, targetFlat) < 3.0f)
                {
                    // Kill bee
                    target.MsgProc(new Msg_TakeDamage(9999, transform.forward));
                }
            }

            owner.targetBee = null;
            if (owner.hp <= 0)
            {
                sm.ChangeState(typeof(RunAway));
            }
            else
            {
                sm.ChangeState(typeof(Idle));
            }
        }
    }

    class RunAway : SM<Bird>.BaseState, IState
    {
        Coroutine crRunAway;

        public RunAway(SM<Bird> sm) : base(sm) { }

        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }

        public void Enter(MsgBase m)
        {
            crRunAway = owner.StartCoroutine(RunAway_CR());
        }

        public void Update() { }

        public void Exit()
        {
            if (crRunAway != null) owner.StopCoroutine(crRunAway);
        }

        IEnumerator RunAway_CR()
        {
            Transform transform = owner.transform;
            
            // Fly away horizontally
            Vector3 runAwayDir = transform.forward;
            runAwayDir.y = 0f;
            if (runAwayDir == Vector3.zero) runAwayDir = Vector3.forward;
            runAwayDir = runAwayDir.normalized;
            
            float elapsed = 0f;
            float runAwayTime = 3f;

            if (owner.hp <= 0)
            {
                runAwayTime = 5f; // 죽었을 때는 스테이지 밖으로 충분히 멀리 날아가도록 설정
            }

            while (elapsed < runAwayTime)
            {
                elapsed += Time.deltaTime;
                Vector3 targetPos = transform.position + runAwayDir * (owner.diveSpeed * 1.5f * Time.deltaTime);
                
                transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.diveSpeed * 1.5f * Time.deltaTime);
                if (runAwayDir != Vector3.zero) 
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(runAwayDir), 0.1f);
                }

                yield return null;
            }

            if (owner.hp <= 0)
            {
                // 화면 밖에서 10초간 대기 (회복)
                yield return new WaitForSeconds(10f);
                owner.hp = owner.maxHp;
                
                // 스테이지 내부(벌들이 있는 곳 주변)로 재배치
                Vector3 center = Vector3.zero;
                if (Bee.allBees.Count > 0)
                {
                    center = Bee.allBees[UnityEngine.Random.Range(0, Bee.allBees.Count)].transform.position;
                }
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 20f;
                Vector3 newPos = new Vector3(center.x + randomCircle.x, owner.flightHeight, center.z + randomCircle.y);
                if (Environment.Instance != null)
                {
                    newPos.x = Mathf.Clamp(newPos.x, -Environment.Instance.worldSize.x, Environment.Instance.worldSize.x);
                    newPos.z = Mathf.Clamp(newPos.z, -Environment.Instance.worldSize.y, Environment.Instance.worldSize.y);
                }
                transform.position = newPos;
            }

            sm.ChangeState(typeof(Idle));
        }
    }
}

#if UNITY_EDITOR
public class BirdEditorTools
{
    [UnityEditor.MenuItem("Tools/Update Bird Speeds")]
    public static void UpdateSpeeds()
    {
        // 씬 내의 Bird 업데이트
        Bird[] sceneBirds = UnityEngine.Object.FindObjectsOfType<Bird>(true);
        foreach (Bird b in sceneBirds)
        {
            b.roamSpeed = 1.0f;
            b.diveSpeed = 4.0f;
            b.detectionRange = 30f;
            UnityEditor.EditorUtility.SetDirty(b);
        }

        // 프리팹 내의 Bird 업데이트
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Bird[] birds = prefab.GetComponentsInChildren<Bird>(true);
                foreach (Bird b in birds)
                {
                    b.roamSpeed = 1.0f;
                    b.diveSpeed = 4.0f;
                    b.detectionRange = 30f;
                    UnityEditor.EditorUtility.SetDirty(b);
                }
            }
        }

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("모든 Bird의 속도가 1.0 / 4.0 으로 성공적으로 업데이트 되었습니다!");
    }
}
#endif
