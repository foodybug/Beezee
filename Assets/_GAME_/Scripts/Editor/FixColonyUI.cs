using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FixColonyUI
{
    public static void Execute()
    {
        string prefabPath = "Assets/_GAME_/Prefabs/Colony.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found!");
            return;
        }

        // Instantiate the prefab to edit it
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // Find the UI_Stats / HpSlider
        Transform uiStats = instance.transform.Find("ColonyUI/UI_Stats");
        if (uiStats == null)
        {
            uiStats = instance.transform.Find("UI_Stats"); // Try direct child
        }
        
        if (uiStats == null)
        {
            // Just search in all children
            Slider[] sliders = instance.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s.gameObject.name.Contains("Hp") || s.gameObject.name.Contains("HP"))
                {
                    Debug.Log("Destroying " + s.gameObject.name);
                    GameObject.DestroyImmediate(s.gameObject);
                }
            }
            
            // Search for anything named HP
            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t != null && t.gameObject.name == "HP")
                {
                    Debug.Log("Destroying " + t.gameObject.name);
                    GameObject.DestroyImmediate(t.gameObject);
                }
            }
        }
        else
        {
            Transform hp = uiStats.Find("HP");
            if (hp != null) GameObject.DestroyImmediate(hp.gameObject);
            
            Transform hpSlider = uiStats.Find("HpSlider");
            if (hpSlider != null) GameObject.DestroyImmediate(hpSlider.gameObject);
        }

        // Apply changes
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        GameObject.DestroyImmediate(instance);
        
        Debug.Log("Colony UI Prefab updated!");
    }
}
