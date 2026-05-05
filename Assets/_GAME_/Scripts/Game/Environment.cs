using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    public static Environment Instance { get; private set; }

    [Header("Global Height Settings")]
    public float beeFlightHeight = 5f;

    [Header("Environment References")]
    [SerializeField] GameObject terrain;
    [SerializeField] List<Flower> listFlowers = new List<Flower>();
    [SerializeField] Rect rectSize;

	private void Awake()
	{
        Instance = this;
        // 씬에 배치된 모든 꽃을 자동으로 찾아서 리스트 갱신
        Flower[] allFlowers = FindObjectsByType<Flower>(FindObjectsSortMode.None);
        listFlowers = new List<Flower>(allFlowers);

        // 인스펙터에서 terrain이 할당되지 않았을 경우 이름으로 찾기 시도
        if (terrain == null)
        {
            terrain = GameObject.Find("Terrain");
            if (terrain == null) terrain = GameObject.Find("Ground");
            if (terrain == null) terrain = GameObject.Find("Floor");
            if (terrain == null) terrain = GameObject.Find("Plane");
        }

        // 바닥(terrain) 색상을 부드러운 노란색으로 변경
        if (terrain != null)
        {
            Renderer r = terrain.GetComponent<Renderer>();
            if (r != null)
            {
                Color warmYellow = new Color(1f, 0.9f, 0.4f);
                r.material.color = warmYellow; 
                
                // URP(유니버설 렌더 파이프라인)를 사용할 경우 _BaseColor 속성을 변경해야 적용됨
                if (r.material.HasProperty("_BaseColor"))
                {
                    r.material.SetColor("_BaseColor", warmYellow);
                }
            }
        }

		for(int i = 0; i < listFlowers.Count; i++)
		{
			Vector3 p = listFlowers[i].transform.position;
			p.x = Random.Range(-rectSize.width, rectSize.width);
			p.z = Random.Range(-rectSize.height, rectSize.height);
			listFlowers[i].transform.position = p;
		}
	}
}
