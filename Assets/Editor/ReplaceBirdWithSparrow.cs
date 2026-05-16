using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class ReplaceBirdWithSparrow
{
    static ReplaceBirdWithSparrow()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        if (SessionState.GetBool("ReplaceBirdWithSparrowRun", false)) return;
        SessionState.SetBool("ReplaceBirdWithSparrowRun", true);

        string scenePath = "Assets/_GAME_/Scenes/Main.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // Find the old Bird
        GameObject oldBird = GameObject.Find("Bird");
        Vector3 spawnPos = new Vector3(0, 5, 0);
        if (oldBird != null)
        {
            spawnPos = oldBird.transform.position;
            GameObject.DestroyImmediate(oldBird);
        }

        // Try to find the Quirky Sparrow prefab
        string[] guids = AssetDatabase.FindAssets("Sparrow t:Prefab", new[] { "Assets/Quirky Series Ultimate/FREE/Prefabs" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("lb_sparrowHQ t:Prefab"); // fallback

        GameObject birdGO = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                birdGO = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
        }
        
        if (birdGO == null)
        {
            birdGO = new GameObject("Bird");
            Debug.LogWarning("Sparrow prefab not found. Created empty GameObject.");
        }
        
        if (birdGO != null)
        {
            birdGO.name = "Bird";
            birdGO.transform.position = spawnPos;
            
            // Add custom Bird component
            if (birdGO.GetComponent<Bird>() == null)
                birdGO.AddComponent<Bird>();
                
            // Add trigger collider for interactions
            if (birdGO.GetComponent<SphereCollider>() == null)
            {
                var col = birdGO.AddComponent<SphereCollider>();
                col.radius = 2f;
                col.isTrigger = true;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bird replaced with Sparrow in Main scene successfully!");
        }
    }
}
