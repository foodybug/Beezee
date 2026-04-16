using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colony : MonoBehaviour, IMsgProc
{
    #region - static -
    static Dictionary<eColony, Colony> dictColony = new Dictionary<eColony, Colony>();
    public static Colony Get(eColony c)
    {
        return dictColony[c];
    }
    public static Colony GetRandomColony()
    {
        int idx = Random.Range(0, (int)eColony.MAX);
        return dictColony[(eColony)idx];
    }
    public static IEnumerable<Colony> AllColonies => dictColony.Values;
    #endregion
    [SerializeField] Transform center;
    [SerializeField] public eColony flag = 0;
    public int food = 0;
    public int maxFood = 1000;

    public int hp;
    public int maxHp = 2000;

    public void Init(eColony c)
    {
        flag = c;
        dictColony[c] = this;
        hp = maxHp;
    }

    public void MsgProc(MsgBase m)
    {
        if (m is Msg_TakeDamage msg)
        {
            hp -= msg.damage;
            if (hp < 0) hp = 0;
        }
        else if (m is Msg_AddFood foodMsg)
        {
            food += foodMsg.amount;
            if (food > maxFood) food = maxFood;
        }
    }

    float spawnTimer = 0f;

    void Update()
    {
        // Need at least 100 food to spawn a bee
        if (food < 100) return;

        // Food drives the spawn rate. 
        // Slowest spawn rate: 10 seconds (when food is near 100)
        // Fastest spawn rate: 1 second (when food is at maxFood)
        float foodRatio = Mathf.Clamp01((float)(food - 100) / (maxFood - 100)); // 0 to 1
        float spawnCycle = Mathf.Lerp(10f, 1f, foodRatio);

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnCycle)
        {
            spawnTimer = 0f;
            SpawnBee();
        }
    }

    void SpawnBee()
    {
        food -= 100;
        
        if (GameMaster.I == null || GameMaster.I.pc == null) return;
        
        Vector3 spawnPos = center != null ? center.position : transform.position;
        // Random offset to prevent bees from stacking exactly on top of each other
        spawnPos += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), Random.Range(-0.5f, 0.5f));

        Bee bee = Instantiate(GameMaster.I.pc.bee, spawnPos, Quaternion.identity);
        bee.Init("Npc", this, false);
        bee.transform.SetParent(GameMaster.I.environment.transform);
    }
}

public class Msg_AddFood : MsgBase
{
    public int amount;
    public Msg_AddFood(int amount)
    {
        this.amount = amount;
    }
}
