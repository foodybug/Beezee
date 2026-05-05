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
using Random = UnityEngine.Random;

public class Bee : MonoBehaviour, IMsgProc
{
    SM<Bee> sm;
    [SerializeField] public string strCurState = "";

    [SerializeField] public Colony colony;

    public string playerName;
    [SerializeField] bool isPlayer = false;

    [Header("Idle")]
    [Range(0f, 5f)]
    [SerializeField] float timeIdle_Waiting = 1f;
    [SerializeField] float rangeIdle_Roaming = 10f;
    [SerializeField] float speedIdle_Roaming = 2f;
    [SerializeField] float rangeIdle_DetectingFood = 3f;
    [SerializeField] float timeIdle_DetectingFood = 3f;
    //[SerializeField] float timeIdle_ApproachingFood = 2f;
    //[SerializeField] float timeIdle_EncounterFood = 0.2f;
    [Header("Gather")]
    [SerializeField] float gatheringSpeed = 1f;
    [Header("Transport")]
    [Range(0f, 5f)]
    [SerializeField] float timeTransport_ChangeDirection = 1f;
    [SerializeField] float irregularityFactor = 0.5f; // 불규칙성 정도
    [Header("Storing")]
    [SerializeField] float storingSpeed = 1f;

    [Header("Combat")]
    [SerializeField] public int attackPower = 10;
    [SerializeField] float detectionRangeCombat = 5f;
    [SerializeField] float combatRunSpeed = 4f;
    [SerializeField] float dashDistance = 0.25f; // 대시 거리를 줄여서 근접에서 타격하도록 변경
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float knockbackDuration = 0.2f;
    [SerializeField] float knockbackSpeed = 10f;

    public Bee targetBee;
    public Colony targetColony;
    public Flower targetFlower;
    public Vector3 knockbackDir;
    public float knockbackMultiplier = 1f; // 넉백 배율 (후방 타격 시 감소)

    [Header("Status")]
    public int hp = 100;
    public int maxHp = 100;
    public int food = 0;
    public int maxFood = 100;

    public static List<Bee> allBees = new List<Bee>();

    private void Awake()
    {
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null)
        {
            sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 1f; // Adjust size for easier clicking
            // Make it a trigger if you don't want physical bumps, but raycast might ignore triggers by default if not set.
            // Leaving it as a regular collider for typical raycast detection.
        }

        if (!allBees.Contains(this)) allBees.Add(this);

        sm = new SM<Bee>(this, (a) =>
        {
            //Debug.Log($"[Bee] SM<Bee>:: ChangeState: type = {a}");
            strCurState = a.ToString().Replace("Bee+", "");
        });
        sm.RegisterState(new Idle(sm));
        sm.RegisterState(new Gather(sm));
        sm.RegisterState(new Transport(sm));
        sm.RegisterState(new Storing(sm));
        sm.RegisterState(new Combat(sm));
        sm.RegisterState(new Knockback(sm));
        sm.RegisterState(new Death(sm));

        sm.RegisterState(new Following(sm));
        sm.RegisterState(new Possessed(sm));
    }

    private void OnDestroy()
    {
        if (allBees.Contains(this)) allBees.Remove(this);
    }

    public Vector3 baseScale;

    private void Start()
    {
        transform.localScale = transform.localScale / 3f;
        baseScale = transform.localScale;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // 벌의 정면을 나타내는 삼각형(LineRenderer) 지시선 추가
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();
        lr.startWidth = 0.26f; // 삼각형의 밑변 (기존 0.4에서 2/3 크기)
        lr.endWidth = 0.0f;   // 뾰족한 끝
        lr.material = new Material(Shader.Find("Sprites/Default"));
        
        Color colColor = Color.white;
        if (colony != null)
            colColor = colony.flag == eColony.Red ? Color.red : (colony.flag == eColony.Blue ? Color.blue : Color.white);
            
        lr.startColor = new Color(colColor.r, colColor.g, colColor.b, 0.8f);
        lr.endColor = new Color(colColor.r, colColor.g, colColor.b, 0.8f);
        lr.positionCount = 2;
        lr.useWorldSpace = false; // 로컬 좌표계 사용 (벌과 함께 회전/이동)
        lr.SetPosition(0, new Vector3(0, 0.5f, 0.75f)); // 벌 앞쪽부터 시작
        lr.SetPosition(1, new Vector3(0, 0.5f, 1.08f)); // 정면 꼭짓점 (길이도 2/3 크기)
    }
    public void Init(string name, Colony c, bool isPlayer = false)
    {
        playerName = name;
        colony = c;

        if (colony != null)
        {
            Vector3 startPos = colony.transform.position;
            // 시작 위치가 완전히 겹치지 않도록 약간의 무작위 변동 추가
            startPos += Random.insideUnitSphere * 1.5f;
            startPos.y = 0f;
            transform.position = startPos;

            // 정면 지시선(LineRenderer) 색상을 소속 콜로니 색상으로 변경
            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr != null)
            {
                Color colColor = colony.flag == eColony.Red ? Color.red : (colony.flag == eColony.Blue ? Color.blue : Color.white);
                lr.startColor = new Color(colColor.r, colColor.g, colColor.b, 0.8f);
                lr.endColor = new Color(colColor.r, colColor.g, colColor.b, 0.8f); // 끝까지 불투명하게 유지
            }
        }

        if (isPlayer == true)
            sm.ChangeState(typeof(Possessed));
        else
            sm.ChangeState(typeof(Idle));
    }
    public bool CheckEnemy()
    {
        for (int i = 0; i < allBees.Count; i++)
        {
            var other = allBees[i];
            if (other == null) continue;
            if (other == this || other.hp <= 0 || other.strCurState == "Death" || other.colony == null || this.colony == null || other.colony.flag == this.colony.flag) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist <= detectionRangeCombat)
            {
                targetBee = other;
                targetColony = null;
                sm.ChangeState(typeof(Combat));
                return true;
            }
        }

        if (this.colony != null)
        {
            foreach (var col in Colony.AllColonies)
            {
                if (col == null || col.hp <= 0 || col.flag == this.colony.flag) continue;
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist <= detectionRangeCombat)
                {
                    targetColony = col;
                    targetBee = null;
                    sm.ChangeState(typeof(Combat));
                    return true;
                }
            }
        }

        return false;
    }

    public void Update()
    {
        sm.Update();
    }

    public void MsgProc(MsgBase m)
    {
        ((IMsgProc)sm)?.MsgProc(m);

        #region - special case -
        if (m is Msg_TakeDamage msg)
        {
            int damage = msg.damage;
            if (!string.IsNullOrEmpty(strCurState) && strCurState.Contains("Knockback"))
            {
                damage *= 2;
            }

            hp -= damage;
            knockbackDir = msg.hitDir;
            knockbackMultiplier = msg.knockbackMultiplier;
            sm.ChangeState(typeof(Knockback));
        }
        #endregion
    }
    #region - state - 
    class Idle : SM<Bee>.BaseState, IState
    {
        Coroutine crRoaming;
        Coroutine crDetectingFood;
        Coroutine crDetectingEnemy;

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
            crDetectingEnemy = owner.StartCoroutine(DetectingEnemy_CR());
        }
        public void Update()
        {
            if (owner.food >= owner.maxFood * 0.7f)
            {
                sm.ChangeState(typeof(Transport));
            }
        }
        public void Exit()
        {
            if (crRoaming != null) owner.StopCoroutine(crRoaming);
            if (crDetectingFood != null) owner.StopCoroutine(crDetectingFood);
            if (crDetectingEnemy != null) owner.StopCoroutine(crDetectingEnemy);
        }
        #endregion

        IEnumerator DetectingEnemy_CR()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                if (owner.CheckEnemy()) yield break;
            }
        }
        IEnumerator Roaming_CR()
        {
            Transform transform = owner.transform;
            while (true)
            {
                targetPos = transform.position + Random.insideUnitSphere * owner.rangeIdle_Roaming;
                targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;

                while (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    // 부드럽게 이동
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

                    // 이동 방향 바라보기 (부드럽게 회전)
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

            while (true)
            {
                yield return new WaitForSeconds(owner.timeIdle_DetectingFood);

                Collider[] c = Physics.OverlapSphere(transform.position, owner.rangeIdle_DetectingFood, LayerMask.GetMask("Flower"));
                if (c != null && c.Length > 0)
                {
                    int index = Random.Range(0, c.Length);
                    owner.targetFlower = c[index].GetComponent<Flower>();
                    if (owner.targetFlower == null) owner.targetFlower = c[index].GetComponentInParent<Flower>();
                    targetPos = c[index].transform.position;
                    targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;

                    owner.StopCoroutine(crRoaming);

                    break;
                }
            }

            while (true)
            {
                while (Vector3.Distance(transform.position, targetPos) > 0.3f)
                {
                    // 부드럽게 이동
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

                    // 이동 방향 바라보기 (부드럽게 회전)
                    Vector3 direction = targetPos - transform.position;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                    }

                    yield return null; // 다음 프레임까지 대기
                }

                sm.ChangeState(typeof(Gather));
                break;
            }
        }
    }

    class Gather : SM<Bee>.BaseState, IState
    {
        Coroutine crGathering;
        public Gather(SM<Bee> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            crGathering = owner.StartCoroutine(Gathering_CR());
        }
        public void Update() { }
        public void Exit()
        {
            if (crGathering != null) owner.StopCoroutine(crGathering);
        }

        IEnumerator Gathering_CR()
        {
            Transform transform = owner.transform;
            while (true)
            {
                if (owner.food >= owner.maxFood)
                {
                    sm.ChangeState(typeof(Transport));
                    yield break;
                }

                if (owner.targetFlower == null)
                {
                    sm.ChangeState(typeof(Idle));
                    yield break;
                }

                // 꽃 주변을 배회하며 꽃가루(Pollen)와 충돌 유도
                Vector3 targetPos = owner.targetFlower.transform.position + UnityEngine.Random.insideUnitSphere * 1.5f;
                targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;

                float roamTime = 1.0f;
                float elapsed = 0f;
                while (elapsed < roamTime)
                {
                    elapsed += Time.deltaTime;
                    if (owner.food >= owner.maxFood)
                    {
                        sm.ChangeState(typeof(Transport));
                        yield break;
                    }

                    if (Vector3.Distance(transform.position, targetPos) > 0.1f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);
                        Vector3 direction = targetPos - transform.position;
                        if (direction != Vector3.zero)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.2f);
                        }
                    }
                    yield return null;
                }

                // 꽃의 꿀을 감소시키지만, 벌의 food를 직접 올리지는 않음 (Pollen 충돌로만 획득)
                int taken = owner.targetFlower.TakeFood(2);
                if (taken <= 0)
                {
                    owner.targetFlower = null;
                    sm.ChangeState(typeof(Idle));
                    yield break;
                }
            }
        }
    }
    class Transport : SM<Bee>.BaseState, IState
    {
        Coroutine crTransporting;
        Coroutine crDetectingEnemy;

        public Transport(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {

        }
        public void Enter(MsgBase m)
        {
            crTransporting = owner.StartCoroutine(Transporting_CR());
            crDetectingEnemy = owner.StartCoroutine(DetectingEnemy_CR());
        }
        public void Update()
        {

        }
        public void Exit()
        {
            if (crTransporting != null) owner.StopCoroutine(crTransporting);
            if (crDetectingEnemy != null) owner.StopCoroutine(crDetectingEnemy);
        }
        #endregion

        IEnumerator DetectingEnemy_CR()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                if (owner.CheckEnemy()) yield break;
            }
        }
        IEnumerator Transporting_CR()
        {
            Transform transform = owner.transform;

            while (true)
            {
                Vector3 targetPos = owner.colony.transform.position;
                targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;

                if (Vector3.Distance(transform.position, targetPos) <= 0.1f)
                {
                    break;
                }

                // 부드럽게 이동
                transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

                // 이동 방향 바라보기 (부드럽게 회전)
                Vector3 direction = targetPos - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                }

                yield return null; // 다음 프레임까지 대기
            }

            sm.ChangeState(typeof(Storing));
        }
    }
    class Storing : SM<Bee>.BaseState, IState
    {
        Coroutine crStoring;
        public Storing(SM<Bee> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {

        }
        public void Enter(MsgBase m)
        {
            crStoring = owner.StartCoroutine(Storing_CR());
        }
        public void Update()
        {
        }
        public void Exit()
        {
            if (crStoring != null)
                owner.StopCoroutine(crStoring);
        }
        IEnumerator Storing_CR()
        {
            yield return new WaitForSeconds(owner.storingSpeed);
            // Send food directly avoiding interface cast issues
            if (owner.colony != null)
                owner.colony.MsgProc(new Msg_AddFood(owner.food));
            owner.food = 0;
            sm.ChangeState(typeof(Idle));
        }
    }
    class Combat : SM<Bee>.BaseState, IState
    {
        Coroutine crCombat;
        public Combat(SM<Bee> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            crCombat = owner.StartCoroutine(Combat_CR());
        }
        public void Update() { }
        public void Exit()
        {
            if (crCombat != null) owner.StopCoroutine(crCombat);
        }

        IEnumerator Combat_CR()
        {
            Transform transform = owner.transform;

            while ((owner.targetBee != null && owner.targetBee.hp > 0 && owner.targetBee.strCurState != "Death") || (owner.targetColony != null && owner.targetColony.hp > 0))
            {
                Vector3 targetPos = owner.targetBee != null ? owner.targetBee.transform.position : owner.targetColony.transform.position;
                targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;
                float dist = Vector3.Distance(transform.position, targetPos);

                if (dist <= owner.dashDistance)
                {
                    // Dash
                    float dashTime = dist / owner.dashSpeed;
                    float elapsed = 0f;

                    while (elapsed < dashTime)
                    {
                        elapsed += Time.deltaTime;
                        if ((owner.targetBee == null && owner.targetColony == null) ||
                            (owner.targetBee != null && (owner.targetBee.hp <= 0 || owner.targetBee.strCurState == "Death")) ||
                            (owner.targetColony != null && owner.targetColony.hp <= 0))
                        {
                            break;
                        }

                        targetPos = owner.targetBee != null ? owner.targetBee.transform.position : owner.targetColony.transform.position;
                        targetPos.y = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 0f;
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.dashSpeed * Time.deltaTime);

                        Vector3 dir = (targetPos - transform.position).normalized;
                        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                        if (Vector3.Distance(transform.position, targetPos) < 0.3f)
                            break;

                        yield return null;
                    }

                    if (owner.targetBee != null && owner.targetBee.hp > 0 && owner.targetBee.strCurState != "Death")
                    {
                        Vector3 hitDir = (owner.targetBee.transform.position - transform.position).normalized;
                        hitDir.y = 0f;
                        if (hitDir == Vector3.zero) hitDir = transform.forward;

                        float angle = Vector3.Angle(owner.targetBee.transform.forward, hitDir);
                        float finalDamageFloat = owner.attackPower;
                        if (angle < 45f) // Back
                        {
                            finalDamageFloat *= 2.5f;
                        }
                        else if (angle < 135f) // Side
                        {
                            finalDamageFloat *= 1.5f;
                        }

                        int finalDamage = Mathf.RoundToInt(finalDamageFloat);
                        if (owner.targetBee != null)
                            owner.targetBee.MsgProc(new Msg_TakeDamage(finalDamage, hitDir));

                        // 후방 타격이 아닐 경우에만 공격자 넉백 (공격자는 대상의 절반만큼 밀림)
                        if (angle >= 45f)
                        {
                            owner.MsgProc(new Msg_TakeDamage(0, -hitDir, 0.5f));
                        }
                        yield break;
                    }
                    else if (owner.targetColony != null && owner.targetColony.hp > 0)
                    {
                        Vector3 hitDir = (owner.targetColony.transform.position - transform.position).normalized;
                        hitDir.y = 0f;
                        if (hitDir == Vector3.zero) hitDir = transform.forward;

                        int finalDamage = owner.attackPower;
                        if (owner.targetColony != null)
                            owner.targetColony.MsgProc(new Msg_TakeDamage(finalDamage, hitDir));

                        // Attacker knockback
                        owner.MsgProc(new Msg_TakeDamage(0, -hitDir));
                        yield break;
                    }
                }
                else
                {
                    // Approach normally
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.combatRunSpeed * Time.deltaTime);
                    Vector3 direction = targetPos - transform.position;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                    }
                }

                yield return null;
            }

            if (owner.isPlayer)
                sm.ChangeState(typeof(Possessed));
            else
                sm.ChangeState(typeof(Idle));
        }
    }

    class Knockback : SM<Bee>.BaseState, IState
    {
        Coroutine crKnockback;
        GameObject goStunEffect;
        Quaternion originalRotation;

        public Knockback(SM<Bee> sm) : base(sm) { }
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic) { }
        public void Enter(MsgBase m)
        {
            originalRotation = owner.transform.localRotation;
            crKnockback = owner.StartCoroutine(Knockback_CR());
        }
        public void Update() { }
        public void Exit()
        {
            if (crKnockback != null) owner.StopCoroutine(crKnockback);
            if (goStunEffect != null) GameObject.Destroy(goStunEffect);
            
            // 상태를 벗어날 때 크기/회전 원상 복구
            owner.transform.localScale = owner.baseScale;
            owner.transform.localRotation = originalRotation;
        }

        IEnumerator Knockback_CR()
        {
            float elapsed = 0f;
            Vector3 dir = owner.knockbackDir.normalized;
            dir.y = 0f;

            while (elapsed < owner.knockbackDuration)
            {
                elapsed += Time.deltaTime;
                // Decelerate over time so a new hit (which resets elapsed) is visibly a new knockback
                float speedMultiplier = Mathf.Lerp(1f, 0f, elapsed / owner.knockbackDuration);

                // 넉백 속도를 온전히 적용 (감속 효과 포함)
                owner.transform.position += dir * (owner.knockbackSpeed * owner.knockbackMultiplier * speedMultiplier * Time.deltaTime);
                yield return null;
            }

            if (owner.hp <= 0)
            {
                owner.hp = 0;
                sm.ChangeState(typeof(Death));
            }
            else
            {
                // 밀려남이 끝난 후 스턴 이펙트 (기존 3.75초에서 1.5초로 대폭 감소)
                float stunElapsed = 0f;
                float stunDuration = 1.5f;

                // 스턴 이펙트 별 텍스트 생성
                goStunEffect = new GameObject("StunEffect");
                goStunEffect.transform.SetParent(owner.transform);
                goStunEffect.transform.localPosition = new Vector3(0, 1.5f, 0);
                
                TextMesh tm = goStunEffect.AddComponent<TextMesh>();
                tm.text = "★ ★ ★";
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.05f; // 크기 축소 (기존 0.1f)
                tm.fontSize = 30; // 폰트 사이즈 축소 (기존 40)
                tm.color = new Color(1f, 0.8f, 0f); // 쨍한 금/노란색
                tm.fontStyle = FontStyle.Bold;

                while (stunElapsed < stunDuration)
                {
                    stunElapsed += Time.deltaTime;

                    // 1. 크기를 줄였다 늘렸다 반복 (맥박처럼 뜀)
                    float scaleMultiplier = 1f - 0.2f * Mathf.Abs(Mathf.Sin(stunElapsed * Mathf.PI * 3f));
                    owner.transform.localScale = owner.baseScale * scaleMultiplier;

                    // 2. 좌우로 부들부들 떨리는 효과 (진동)
                    float shake = Mathf.Sin(stunElapsed * 40f) * 15f; 
                    owner.transform.localRotation = originalRotation * Quaternion.Euler(0, shake, 0);

                    // 3. 스턴 이펙트가 위아래로 움직이며, 카메라를 바라보되 Z축으로 빙글빙글 돌게 함
                    if (goStunEffect != null)
                    {
                        goStunEffect.transform.localPosition = new Vector3(0, 1.5f + Mathf.Sin(stunElapsed * 8f) * 0.15f, 0);
                        if (Camera.main != null)
                        {
                            goStunEffect.transform.forward = Camera.main.transform.forward; // 빌보드 효과
                            goStunEffect.transform.Rotate(0, 0, stunElapsed * -200f); // 어지럽게 도는 효과 (역방향)
                        }
                    }

                    yield return null;
                }

                if (owner.isPlayer)
                    sm.ChangeState(typeof(Possessed));
                else
                    sm.ChangeState(typeof(Combat));
            }
        }
    }

    class Death : SM<Bee>.BaseState, IState
    {
        public Death(SM<Bee> sm) : base(sm) { }
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
        }
        public void Enter(MsgBase m)
        {
            BeeUI beeUI = owner.GetComponentInChildren<BeeUI>();
            if (beeUI != null) beeUI.gameObject.SetActive(false);

            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r is SpriteRenderer sr)
                {
                    Color c = sr.color;
                    c.a = 0.5f;
                    sr.color = c;
                }
                else
                {
                    if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        c.a = 0.5f;
                        r.material.color = c;

                        // Standard shader transparency setup
                        r.material.SetFloat("_Mode", 3);
                        r.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        r.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        r.material.SetInt("_ZWrite", 0);
                        r.material.DisableKeyword("_ALPHATEST_ON");
                        r.material.EnableKeyword("_ALPHABLEND_ON");
                        r.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        r.material.renderQueue = 3000;
                    }
                    if (r.material.HasProperty("_BaseColor"))
                    {
                        Color c = r.material.GetColor("_BaseColor");
                        c.a = 0.5f;
                        r.material.SetColor("_BaseColor", c);
                    }
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
        public bool isMovingToPoint = false;
        public Vector3 destPoint;
        Coroutine crCombat;
        #region - interface -
        public void RegisterEvent(Dictionary<Type, Dictionary<Type, Action<MsgBase>>> ddic)
        {
        }
        public void Enter(MsgBase m)
        {
            InputControl.I.aMouseClicking += OnClicking;
            InputControl.I.aMouseClickUp += OnClickUp;

            owner.name = "Player";
            owner.isPlayer = true;

            CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
            cf.enabled = true;
            cf.target = owner.transform;

            if ((owner.targetBee != null && owner.targetBee.hp > 0 && owner.targetBee.strCurState != "Death") || (owner.targetColony != null && owner.targetColony.hp > 0))
            {
                crCombat = owner.StartCoroutine(PossessedCombat_CR());
            }
        }
        public void Update()
        {
            if (crCombat != null) return;

            if (isClicking == false && isMovingToPoint == false)
                return;

            Transform transform = owner.transform;

            // 전역 설정된 비행 고도를 사용합니다.
            float flightHeight = Environment.Instance != null ? Environment.Instance.beeFlightHeight : transform.position.y;

            Vector3 targetPos = destPoint;
            
            // 쿼터뷰 환경에서 클릭한 바닥 좌표를 벌의 전역 비행 고도로 시각적 오차 없이 끌어올림
            if (Camera.main != null)
            {
                Vector3 camFwd = Camera.main.transform.forward;
                if (Mathf.Abs(camFwd.y) > 0.001f)
                {
                    float k = (flightHeight - destPoint.y) / camFwd.y;
                    targetPos = destPoint + camFwd * k;
                }
                else
                {
                    targetPos.y = flightHeight;
                }
            }
            else
            {
                targetPos.y = flightHeight;
            }

            // 벌이 현재 높이에서 벗어났을 경우, 이동 시 자연스럽게 지정된 비행 고도로 맞춰줌
            Vector3 currentPos = transform.position;
            if (Mathf.Abs(currentPos.y - flightHeight) > 0.01f)
            {
                // Y축(고도) 보정도 부드럽게 이루어지도록 MoveTowards 대상의 고도를 일치시킵니다.
                // targetPos는 위에서 계산된 목표 지점(X, Z)이며 Y축도 정확히 flightHeight를 가리킵니다.
            }

            if (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                // 부드럽게 이동
                transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.speedIdle_Roaming * Time.deltaTime);

                // 이동 방향 바라보기 (부드럽게 회전)
                Vector3 direction = targetPos - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.2f);
                }
            }
            else
            {
                isMovingToPoint = false;
            }
        }
        public void Exit()
        {
            if (crCombat != null) owner.StopCoroutine(crCombat);
            InputControl.I.aMouseClicking -= OnClicking;
            InputControl.I.aMouseClickUp -= OnClickUp;

            owner.name = "NPC";
        }
        #endregion
        #region - input -
        void OnClicking(GameObject obj, Vector3 point)
        {
            if (obj != null)
            {
                Bee clickedBee = obj.GetComponent<Bee>();
                if (clickedBee == null) clickedBee = obj.GetComponentInParent<Bee>();

                if (clickedBee != null && clickedBee != owner && clickedBee.hp > 0 && clickedBee.strCurState != "Death")
                {
                    if (owner.colony != null && clickedBee.colony != null && owner.colony.flag != clickedBee.colony.flag)
                    {
                        owner.targetBee = clickedBee;
                        owner.targetColony = null;
                        isClicking = false;
                        isMovingToPoint = false;
                        if (crCombat != null) owner.StopCoroutine(crCombat);
                        crCombat = owner.StartCoroutine(PossessedCombat_CR());
                        return;
                    }
                }

                Colony clickedColony = obj.GetComponent<Colony>();
                if (clickedColony == null) clickedColony = obj.GetComponentInParent<Colony>();

                if (clickedColony != null && clickedColony.hp > 0)
                {
                    if (owner.colony != null && owner.colony.flag != clickedColony.flag)
                    {
                        owner.targetColony = clickedColony;
                        owner.targetBee = null;
                        isClicking = false;
                        isMovingToPoint = false;
                        if (crCombat != null) owner.StopCoroutine(crCombat);
                        crCombat = owner.StartCoroutine(PossessedCombat_CR());
                        return;
                    }
                }
            }

            if (crCombat != null)
            {
                owner.StopCoroutine(crCombat);
                crCombat = null;
            }
            owner.targetBee = null;
            owner.targetColony = null;

            if (isClicking == false)
                isClicking = true;

            isMovingToPoint = true;
            destPoint = point;
        }

        IEnumerator PossessedCombat_CR()
        {
            Transform transform = owner.transform;

            while ((owner.targetBee != null && owner.targetBee.hp > 0 && owner.targetBee.strCurState != "Death") || (owner.targetColony != null && owner.targetColony.hp > 0))
            {
                Vector3 targetPos = owner.targetBee != null ? owner.targetBee.transform.position : owner.targetColony.transform.position;
                targetPos.y = 0f;
                float dist = Vector3.Distance(transform.position, targetPos);

                if (dist <= owner.dashDistance)
                {
                    // Dash
                    float dashTime = dist / owner.dashSpeed;
                    float elapsed = 0f;

                    while (elapsed < dashTime)
                    {
                        elapsed += Time.deltaTime;
                        if ((owner.targetBee == null && owner.targetColony == null) ||
                            (owner.targetBee != null && (owner.targetBee.hp <= 0 || owner.targetBee.strCurState == "Death")) ||
                            (owner.targetColony != null && owner.targetColony.hp <= 0))
                        {
                            break;
                        }
                        targetPos = owner.targetBee != null ? owner.targetBee.transform.position : owner.targetColony.transform.position;
                        targetPos.y = 0f;
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.dashSpeed * Time.deltaTime);

                        Vector3 dir = (targetPos - transform.position).normalized;
                        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                        if (Vector3.Distance(transform.position, targetPos) < 0.3f)
                            break;

                        yield return null;
                    }

                    if (owner.targetBee != null && owner.targetBee.hp > 0 && owner.targetBee.strCurState != "Death")
                    {
                        Vector3 hitDir = (owner.targetBee.transform.position - transform.position).normalized;
                        hitDir.y = 0f;
                        if (hitDir == Vector3.zero) hitDir = transform.forward;

                        float angle = Vector3.Angle(owner.targetBee.transform.forward, hitDir);
                        float finalDamageFloat = owner.attackPower;
                        if (angle < 45f) // Back
                        {
                            finalDamageFloat *= 2.5f;
                        }
                        else if (angle < 135f) // Side
                        {
                            finalDamageFloat *= 1.5f;
                        }

                        int finalDamage = Mathf.RoundToInt(finalDamageFloat);
                        if (owner.targetBee != null)
                            owner.targetBee.MsgProc(new Msg_TakeDamage(finalDamage, hitDir));

                        // 후방 타격이 아닐 경우에만 공격자 넉백 (공격자는 대상의 절반만큼 밀림)
                        if (angle >= 45f)
                        {
                            owner.MsgProc(new Msg_TakeDamage(0, -hitDir, 0.5f));
                        }
                        yield break;
                    }
                    else if (owner.targetColony != null && owner.targetColony.hp > 0)
                    {
                        Vector3 hitDir = (owner.targetColony.transform.position - transform.position).normalized;
                        hitDir.y = 0f;
                        if (hitDir == Vector3.zero) hitDir = transform.forward;

                        int finalDamage = owner.attackPower;
                        if (owner.targetColony != null)
                            owner.targetColony.MsgProc(new Msg_TakeDamage(finalDamage, hitDir));

                        // Attacker knockback
                        owner.MsgProc(new Msg_TakeDamage(0, -hitDir));
                        yield break;
                    }
                }
                else
                {
                    // Approach normally
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, owner.combatRunSpeed * Time.deltaTime);
                    Vector3 direction = targetPos - transform.position;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                    }
                }

                yield return null;
            }

            owner.targetBee = null;
            owner.targetColony = null;
        }

        void OnClickUp(GameObject obj)
        {
            isClicking = false;
        }
        #endregion
    }
    #endregion
}

public class Msg_TakeDamage : MsgBase
{
    public int damage;
    public Vector3 hitDir;
    public float knockbackMultiplier; // 넉백 배율 (1.0 = 기본, 0.3 = 후방타격 등)

    public Msg_TakeDamage(int damage, Vector3 hitDir, float knockbackMultiplier = 1f)
    {
        this.damage = damage;
        this.hitDir = hitDir;
        this.knockbackMultiplier = knockbackMultiplier;
    }
}
