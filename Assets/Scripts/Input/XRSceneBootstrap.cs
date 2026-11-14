using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
///   처음부터 비활성인 캔버스(설정창 등)도 포함하며, 켜질 때 변환된다. 런타임에 새로 생기는 캔버스는 변환되지 않는다.
///   중첩 캔버스에는 TrackedDeviceGraphicRaycaster만 붙인다.
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

        // 실제 헤드셋에서는 씬에 남아 있는 XR Device Simulator가 가짜 머리/컨트롤러를 만들어 진짜 컨트롤러를 가로챈다(Play 씬).
        if (UnityEngine.XR.XRSettings.isDeviceActive) RemoveSimulators();

        if (VRMapViewer.Instance == null) SpawnRig();
        VRMenuTrim.Apply(); // 메뉴 씬의 설정 버튼 제거(변환 전에 해서 변환된 패널에 반영되게 한다)
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

    private static void RemoveSimulators()
    {
        var sims = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sim in sims) Object.Destroy(sim.gameObject);
        Debug.Log($"[XRSceneBootstrap] XR Device Simulator {sims.Length}개 제거 (씬: {SceneManager.GetActiveScene().name})");
    }

    private static void SpawnRig()
    {
        var prefab = Resources.Load<GameObject>(RigResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[XRSceneBootstrap] Resources/{RigResourcePath} 프리팹을 찾지 못했습니다.");
            return;
        }

        // 레이 인터랙터는 켜질 때 그 시점의 EventSystem에 UI 입력 모듈을 등록한다.
        // 씬의 기존 EventSystem이 살아 있으면 레이가 거기(나중에 꺼질 모듈)에 등록되어 UI를 못 맞추므로,
        // 기존 것을 먼저 끄고 EventSystem_XR을 리그보다 먼저 켠 뒤 리그를 켠다.
        DisableSceneEventSystems();

        var holder = new GameObject("XR_Set_Holder");
        holder.SetActive(false);
        var rig = Object.Instantiate(prefab, holder.transform); // 비활성 부모 아래라 아직 OnEnable이 돌지 않는다
        rig.name = prefab.name;

        // 프리팹의 EventSystem_XR은 꺼진 상태로 저장되어 있으므로 여기서 켠다.
        foreach (var module in rig.GetComponentsInChildren<XRUIInputModule>(true))
        {
            var esTransform = module.transform;
            esTransform.SetParent(null);
            esTransform.gameObject.SetActive(true);
            esTransform.SetParent(rig.transform);
        }

        rig.transform.SetParent(null);
        Object.Destroy(holder);

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
        DisableSceneEventSystems();

        Debug.Log("[XRSceneBootstrap] XR 리그를 자동 생성했습니다.");
    }

    // XR 리그의 EventSystem_XR만 남기고 씬의 EventSystem은 모두 끈다.
    private static void DisableSceneEventSystems()
    {
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (es.GetComponent<XRUIInputModule>() != null) continue;
            foreach (var module in es.GetComponents<BaseInputModule>()) module.enabled = false;
            es.enabled = false;
        }
    }

    private static void ConvertCanvases()
    {
        // 설정창처럼 꺼져 있다가 켜지는 캔버스도 포함한다(어댑터는 켜질 때 Start에서 변환한다).
        // 꺼진 오브젝트는 isRootCanvas가 부정확하므로 부모 계층으로 루트 여부를 판단한다.
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // CanvasScaler가 붙은 캔버스는 다른 캔버스 안에 있어도 화면 전체용 UI(설정창 등)이므로 독립 패널로 변환한다.
            // 드롭다운 목록 같은 작은 중첩 캔버스(CanvasScaler 없음)는 부모를 따라가게 둔다.
            bool isRoot = canvas.transform.parent == null ||
                          canvas.transform.parent.GetComponentInParent<Canvas>(true) == null ||
                          canvas.GetComponent<CanvasScaler>() != null;
            Debug.Log($"[XRSceneBootstrap] 캔버스 '{canvas.name}' 루트={isRoot} 모드={canvas.renderMode} 활성={canvas.gameObject.activeInHierarchy} 씬={canvas.gameObject.scene.name}");
            if (!isRoot)
            {
                // 중첩 캔버스는 자기 그래픽을 자기 레이캐스터로만 맞출 수 있다.
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                continue;
            }
            if (canvas.renderMode == RenderMode.WorldSpace) continue;
            if (canvas.GetComponent<VRCanvasAdapter>() != null) continue;
            canvas.gameObject.AddComponent<VRCanvasAdapter>();
        }
    }
}
