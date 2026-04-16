using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flower : MonoBehaviour
{
    [SerializeField] Transform center;
    public int food = 10; // Default food amount

    void Start()
    {

    }

    void Update()
    {

    }

    public int TakeFood(int amount)
    {
        if (food <= 0) return 0;

        int taken = Mathf.Min(food, amount);
        food -= taken;

        if (food <= 0)
        {
            // Turn color to yellow
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material.color = Color.yellow;
            }

            // Remove from 'Flower' layer so bees won't target it anymore
            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        return taken;
    }
}
