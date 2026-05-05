using UnityEngine;
using UnityEditor;

public class ApplySettingsTweaker : MonoBehaviour
{
    [MenuItem("Tools/Apply Quarter View and Yellow Floor")]
    public static void ApplySettings()
    {
        // 1. 카메라 쿼터뷰 적용
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam.transform, "Apply Quarter View");
            Undo.RecordObject(cam, "Apply Orthographic");
            
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            Debug.Log("카메라를 쿼터뷰(정면 45도)로 변경했습니다.");

            CameraFollow cf = cam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                Undo.RecordObject(cf, "Update CameraFollow Offset");
                cf.useQuarterView = true;
                cf.quarterViewRotation = new Vector3(45f, 0f, 0f);
                cf.offset = -cam.transform.forward * 15f;
            }
        }
        else
        {
            Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
        }

        // 2. 바닥 색상 적용 (Environment 컴포넌트를 통해 찾거나 넓은 오브젝트 찾기)
        GameObject floorObj = null;
        
        // Environment 스크립트에서 terrain 변수값 가져오기 시도
        Environment env = FindAnyObjectByType<Environment>();
        if (env != null)
        {
            var fieldInfo = typeof(Environment).GetField("terrain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                floorObj = fieldInfo.GetValue(env) as GameObject;
            }
        }

        // 그래도 못 찾았다면 이름으로 찾기
        if (floorObj == null)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer r in renderers)
            {
                string name = r.gameObject.name.ToLower();
                if (name.Contains("terrain") || name.Contains("ground") || name.Contains("floor") || name.Contains("plane"))
                {
                    floorObj = r.gameObject;
                    break;
                }
            }
        }

        if (floorObj != null)
        {
            Renderer r = floorObj.GetComponent<Renderer>();
            if (r != null)
            {
                // 유니티 기본 Material은 수정이 불가능하므로 새로운 Material을 아예 생성해서 덮어씌웁니다!
                Material yellowMat = new Material(Shader.Find("Standard"));
                // URP 환경일 경우를 대비
                if (Shader.Find("Universal Render Pipeline/Lit") != null)
                {
                    yellowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                }
                
                Color warmYellow = new Color(1f, 0.9f, 0.4f);
                yellowMat.color = warmYellow;
                if (yellowMat.HasProperty("_BaseColor"))
                {
                    yellowMat.SetColor("_BaseColor", warmYellow);
                }

                Undo.RecordObject(r, "Apply Yellow Material to Floor");
                r.sharedMaterial = yellowMat;
                
                Debug.Log($"바닥({floorObj.name})에 새로운 노란색 재질을 성공적으로 적용했습니다!");
            }
            else if (floorObj.GetComponent<Terrain>() != null)
            {
                Debug.LogWarning("바닥이 유니티 Terrain 컴포넌트입니다. Terrain의 색상은 재질(Material) 대신 Terrain Layer의 텍스처를 수정해야 합니다.");
            }
            else
            {
                Debug.LogWarning("바닥 오브젝트를 찾았으나 Renderer가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("바닥(Terrain/Ground/Floor/Plane) 객체를 찾을 수 없습니다.");
        }

        // 씬 변경 사항 저장 알림
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("모든 설정이 적용되었습니다. 씬을 저장해주세요.");
    }
}
