using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [SerializeField] GameObject terrain;
    [SerializeField] List<Flower> listFlowers = new List<Flower>();
    [SerializeField] Rect rectSize;
	private void Awake()
	{
		for(int i = 0; i < listFlowers.Count; i++)
		{
			Vector3 p = listFlowers[i].transform.position;
			p.x = Random.Range(-rectSize.width, rectSize.width);
			p.z = Random.Range(-rectSize.height, rectSize.height);
			listFlowers[i].transform.position = p;
		}
	}
}
