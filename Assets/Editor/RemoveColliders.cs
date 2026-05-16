using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class RemoveColliders
{
    static RemoveColliders()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        if (SessionState.GetBool("RemoveCollidersRun", false)) return;
        SessionState.SetBool("RemoveCollidersRun", true);

        string scenePath = "Assets/_GAME_/Scenes/Main.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject birdGO = GameObject.Find("Bird");
        if (birdGO != null)
        {
            // Remove all existing colliders in the bird and its children
            Collider[] allColliders = birdGO.GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders)
            {
                // If it's not our specific trigger SphereCollider, remove it
                // Actually, let's just remove ALL of them and add our SphereCollider back.
                GameObject.DestroyImmediate(col);
            }

            // Re-add the necessary trigger collider at the root
            var triggerCol = birdGO.AddComponent<SphereCollider>();
            triggerCol.radius = 2f;
            triggerCol.isTrigger = true;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Removed unnecessary colliders from Bird and added back the trigger SphereCollider!");
        }
    }
}
