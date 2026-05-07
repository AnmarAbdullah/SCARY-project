using UnityEngine;
using UnityEditor;

public class FPSSetup
{
    [MenuItem("Tools/Setup FPS Scene")]
    static void Build()
    {
        // Remove existing Main Camera
        if (Camera.main != null)
            Object.DestroyImmediate(Camera.main.gameObject);

        // --- Player ---
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "FPS_Player";
        player.transform.position = new Vector3(0, 1, 0);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.center = Vector3.zero;

        // --- Camera Holder (acts as the head) ---
        GameObject head = new GameObject("CameraHolder");
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 0.6f, 0);
        head.transform.localRotation = Quaternion.identity;

        // --- Camera ---
        GameObject camGO = new GameObject("FPS_Camera");
        camGO.transform.SetParent(head.transform);
        camGO.transform.localPosition = Vector3.zero;
        camGO.transform.localRotation = Quaternion.identity;
        Camera cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.1f;
        camGO.AddComponent<AudioListener>();

        // --- Wire up FPSController ---
        FPSController ctrl = player.AddComponent<FPSController>();
        ctrl.cameraHolder = head.transform;

        // --- Ground ---
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5, 1, 5);

        // --- Light (if none) ---
        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightGO = new GameObject("Sun");
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        Debug.Log("FPS Scene ready! Hit Play.");
        Selection.activeGameObject = player;
    }
}
