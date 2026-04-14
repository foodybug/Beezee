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
}

public class Msg_AddFood : MsgBase
{
    public int amount;
    public Msg_AddFood(int amount)
    {
        this.amount = amount;
    }
}
