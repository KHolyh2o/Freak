using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// VR일 때 모든 씬(앞으로 추가될 씬 포함)에 XR 리그를 자동으로 넣고 화면 고정 UI를 VR용으로 바꾼다.
/// 씬마다 따로 조립할 필요가 없다. 데스크톱/폰(비VR)에서는 아무것도 하지 않는다.
///
/// - 씬에 이미 XR 리그가 있으면(Play 씬처럼 직접 구성한 경우) 리그는 새로 만들지 않는다.
/// - 리그는 Resources/XR_Set 프리팹을 쓴다. 리그 구성을 바꾸려면 이 프리팹만 고치면 된다.
/// - 화면에 그리는 카메라/AudioListener/EventSystem은 XR 리그 것만 남기고 끈다
///   (RenderTexture로 그리는 미리보기 카메라는 건드리지 않는다).
/// - 루트 캔버스 중 World Space가 아닌 것에는 VRCanvasAdapter를 자동으로 붙인다.
///   처음부터 비활성인 캔버스나 런타임에 새로 생기는 캔버스는 변환되지 않는다.
/// </summary>
public static class XRSceneBootstrap
{
    private const string RigResourcePath = "XR_Set";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup(); // 첫 씬은 sceneLoaded보다 늦게 구독되므로 직접 한 번 실행
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup();
    }

    private static void Setup()
    {
        if (!XRMode.Wanted) return;

        PlatformRigSwitcher.IsXR = true;

        if (VRMapViewer.Instance == null) SpawnRig();
        ConvertCanvases();
#if UNITY_EDITOR
        SpawnSimulatorIfNeeded();
#endif
    }

#if UNITY_EDITOR
    private const string SimulatorPath =
        "Assets/Samples/XR Interaction Toolkit/2.5.4/XR Device Simulator/XR Device Simulator.prefab";

    // 헤드셋 없이 에디터에서 XR 강제로 테스트할 때, 어느 씬에서든 머리/컨트롤러 조작이 되도록 시뮬레이터를 넣는다.
    private static void SpawnSimulatorIfNeeded()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;
        if (Object.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>() != null) return;

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[XRSceneBootstrap] XR Device Simulator 프리팹을 찾지 못했습니다: {SimulatorPath}");
            return;
        }
        Object.Instantiate(prefab).name = prefab.name;
    }
#endif

    private static void SpawnRig()
    {
        var prefab = Resources.Load<GameObject>(RigResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[XRSceneBootstrap] Resources/{RigResourcePath} 프리팹을 찾지 못했습니다.");
            return;
        }

        var rig = Object.Instantiate(prefab);
        rig.name = prefab.name;

        // 프리팹의 EventSystem_XR은 꺼진 상태로 저장되어 있으므로 여기서 켠다.
        foreach (var module in rig.GetComponentsInChildren<XRUIInputModule>(true))
            module.gameObject.SetActive(true);

        // 화면에 그리는 건 XR 리그의 카메라 하나여야 한다.
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.transform.IsChildOf(rig.transform) || cam.targetTexture != null) continue;
            cam.enabled = false;
        }
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (!listener.transform.IsChildOf(rig.transform)) listener.enabled = false;
        }
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (es.transform.IsChildOf(rig.transform)) continue;
            foreach (var module in es.GetComponents<BaseInputModule>()) module.enabled = false;
            es.enabled = false;
        }

        Debug.Log("[XRSceneBootstrap] XR 리그를 자동 생성했습니다.");
    }

    private static void ConvertCanvases()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (canvas.GetComponent<VRCanvasAdapter>() != null) continue;
            canvas.gameObject.AddComponent<VRCanvasAdapter>();
        }
    }
}
