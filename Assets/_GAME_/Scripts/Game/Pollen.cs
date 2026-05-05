using UnityEngine;
using System.Collections.Generic;

public class Pollen : MonoBehaviour
{
    public int splitCount = 0;
    public int maxSplits = 6;
    public int foodValue = 1;
    public float splitCooldown = 1.0f; // 연쇄 분열 쿨타임

    [Header("Suction")]
    public float suctionRadius = 4f; // 빨려들어가기 시작하는 반경
    public float suctionAcceleration = 30f; // 빨려들어갈 때의 가속도
    public Bee targetBee;
    private float currentSuctionSpeed = 0f;

    private float spawnTime;

    // --- 프레임 최적화 (Object Pooling & NonAlloc) ---
    private static Queue<Pollen> pool = new Queue<Pollen>();
    private static Collider[] overlapResults = new Collider[50]; // OverlapSphere 할당 제거용 배열
    private float nextSearchTime = 0f;
    // ------------------------------------------------

    void Awake()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void OnEnable()
    {
        spawnTime = Time.time;
        nextSearchTime = Time.time + 0.5f; // 스폰 직후 0.5초 뒤부터 탐색 시작
        currentSuctionSpeed = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        TrySplit(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrySplit(other);
    }

    void TrySplit(Collider other)
    {
        // 생성된 직후 아주 짧은 시간(힘을 받기 전)에는 무시
        if (Time.time - spawnTime < 0.2f) return;

        // 수평(XZ)으로 흩어지는 중(속도가 남아있음)에는 연쇄 분열하지 않고 멈췄을 때만 분열
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.5f) 
            return;

        Bee bee = other.GetComponentInParent<Bee>();
        if (bee != null)
        {
            if (splitCount < maxSplits)
            {
                Split(bee.transform.forward, bee);
            }
        }
    }

    void Split(Vector3 splitDirection, Bee hitBee)
    {
        for (int i = 0; i < 2; i++)
        {
            Pollen pScript = null;
            
            // 풀에서 유효한 꽃가루를 꺼냄
            while (pool.Count > 0)
            {
                pScript = pool.Dequeue();
                if (pScript != null) break;
            }

            if (pScript == null)
            {
                // 풀이 비어있으면 새로 Instantiate
                GameObject pObj = Instantiate(gameObject, transform.position, Random.rotation);
                pScript = pObj.GetComponent<Pollen>();
            }
            else
            {
                // 풀에서 꺼낸 객체 위치/회전 초기화
                pScript.transform.position = transform.position;
                pScript.transform.rotation = Random.rotation;
                pScript.gameObject.SetActive(true);
            }

            pScript.splitCount = this.splitCount + 1;
            
            if (pScript.splitCount >= maxSplits)
            {
                pScript.targetBee = hitBee;
            }
            else
            {
                pScript.targetBee = null;
            }
            
            pScript.transform.localScale = transform.localScale * 0.8f;
            
            Rigidbody rb = pScript.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // 흡수 시 켜졌을 수 있는 kinematic 리셋
                
                float angle = (i == 0) ? -60f : 60f;
                // 수평(XZ) 평면으로만 방향을 설정합니다.
                Vector3 forceDir = Quaternion.Euler(0, angle, 0) * splitDirection;
                forceDir.y = 0f; // 상하(Y) 튀어오름 완전 제거

                float forceMultiplier = Mathf.Lerp(1.0f, 0.4f, (float)this.splitCount / maxSplits);
                float forceAmount = Random.Range(5f, 8f) * forceMultiplier;

                rb.linearVelocity = Vector3.zero;
                rb.AddForce(forceDir.normalized * forceAmount, ForceMode.Impulse);
            }

            Collider col = pScript.GetComponentInChildren<Collider>();
            if (col != null) col.isTrigger = true; // 흡수 시 변경되었을 수 있으므로 리셋
        }
        
        // Destroy 대신 풀로 반환
        ReturnToPool();
    }

    void ReturnToPool()
    {
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }

    void Update()
    {
        if (splitCount >= maxSplits)
        {
            if (Time.time - spawnTime > 0.5f)
            {
                if (targetBee == null)
                {
                    // 매 프레임 OverlapSphere를 호출하는 심각한 성능 저하를 방지하기 위해 0.5초마다 탐색 & NonAlloc 사용
                    if (Time.time >= nextSearchTime)
                    {
                        nextSearchTime = Time.time + 0.5f;

                        float sqrDist = float.MaxValue;
                        int count = Physics.OverlapSphereNonAlloc(transform.position, suctionRadius, overlapResults);
                        for (int i = 0; i < count; i++)
                        {
                            Bee b = overlapResults[i].GetComponentInParent<Bee>();
                            if (b != null && b.hp > 0 && b.strCurState != "Death")
                            {
                                float sqrDistCur = (b.transform.position - transform.position).sqrMagnitude;
                                if (sqrDistCur < sqrDist)
                                {
                                    sqrDist = sqrDistCur;
                                    targetBee = b;
                                }
                            }
                        }
                    }
                }

                if (targetBee != null)
                {
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                    
                    currentSuctionSpeed += suctionAcceleration * Time.deltaTime;
                    Vector3 destPos = targetBee.transform.position + Vector3.up * 0.5f;
                    transform.position = Vector3.MoveTowards(transform.position, destPos, currentSuctionSpeed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, destPos) < 0.3f)
                    {
                        targetBee.food = Mathf.Min(targetBee.food + foodValue, targetBee.maxFood);
                        ReturnToPool(); // 파괴하지 않고 풀로 반환
                    }
                    return;
                }
            }
        }

        // 고도를 정확하게 beeFlightHeight로 고정하고 마찰력을 주어 서서히 멈추게 함
        float flightY = Environment.Instance != null ? Environment.Instance.beeFlightHeight : 5f;
        Vector3 pos = transform.position;
        pos.y = flightY;
        transform.position = pos;

        Rigidbody myRb = GetComponent<Rigidbody>();
        if (myRb != null && !myRb.isKinematic)
        {
            Vector3 vel = myRb.linearVelocity;
            vel.x *= 0.95f; // 공기 저항(마찰력)으로 인해 서서히 멈춤
            vel.z *= 0.95f;
            vel.y = 0f;
            myRb.linearVelocity = vel;
        }
    }
}
