using UnityEditor;
using UnityEngine;
using System.Linq;

public class EnvironmentTweaker : Editor
{
    [MenuItem("Tools/Tweak Environment (Halve Rocks, Double Flowers)")]
    public static void Tweak()
    {
        // 1. Halve Rocks
        var rocks = FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.parent != null && (t.gameObject.name.ToLower().Contains("rock") || t.gameObject.name.ToLower().Contains("stone")))
            .ToArray();
            
        int rocksToRemove = rocks.Length / 2;
        int removedCount = 0;
        for (int i = 0; i < rocksToRemove; i++)
        {
            if (rocks[i] != null)
            {
                Undo.DestroyObjectImmediate(rocks[i].gameObject);
                removedCount++;
            }
        }
        
        // 2. Double Flowers
        var flowers = FindObjectsByType<Flower>(FindObjectsSortMode.None);
        int addedCount = 0;
        foreach (var flower in flowers)
        {
            // 씬에 있는 꽃 오브젝트를 그대로 복제하여 컴포넌트(Flower 등) 누락을 방지합니다.
            GameObject newFlower = Instantiate(flower.gameObject, flower.transform.parent);
            newFlower.name = flower.gameObject.name;
            
            // Random offset slightly so they don't overlap completely before Awake randomizes them
            newFlower.transform.position = flower.transform.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            Undo.RegisterCreatedObjectUndo(newFlower, "Duplicate Flower");
            addedCount++;
        }
        
        // 3. Update Environment Script List so they get randomized properly in Awake
        Environment env = FindFirstObjectByType<Environment>();
        if (env != null)
        {
            var serializedObject = new SerializedObject(env);
            var listProp = serializedObject.FindProperty("listFlowers");
            if (listProp != null)
            {
                var allFlowers = FindObjectsByType<Flower>(FindObjectsSortMode.None);
                listProp.ClearArray();
                for(int i = 0; i < allFlowers.Length; i++) {
                    listProp.InsertArrayElementAtIndex(i);
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = allFlowers[i];
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        Debug.Log($"Environment Adjusted: Removed {removedCount} Rocks/Stones. Added {addedCount} Flowers.");
    }
}
