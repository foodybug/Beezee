using System;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour, IMsgProc
{
    public static GameMaster I { get; private set; }

    public Bee playerBee;
    public List<Colony> listColony;
    public Environment environment;

    [SerializeField] public PrefabContainer pc;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        for (int i = 0; i < (int)eColony.MAX; ++i)
        {
            var c = Instantiate(listColony[i]);
            c.Init((eColony)i);
            c.transform.SetParent(environment.transform);
        }

        Bee bee = playerBee = Instantiate(pc.bee);
        bee.Init("Player", Colony.GetRandomColony(), true);
        bee.transform.SetParent(environment.transform);

        for (int i = 0; i < 100; ++i)
        {
            bee = Instantiate(pc.bee);
            bee.Init("Npc", Colony.GetRandomColony(), false);
            bee.transform.SetParent(environment.transform);
        }
    }

    void IMsgProc.MsgProc(MsgBase m)
    {
    }

    [Serializable]
    public class PrefabContainer
    {
        public Bee bee;
    }
}