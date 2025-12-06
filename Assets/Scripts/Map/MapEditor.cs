using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO; // 파일 저장을 위해 필요
using TMPro;     // 텍스트 및 입력창 제어용

public class MapEditor : MonoBehaviour
{
    [Header("설치할 프리팹 연결 (순서 중요: 0,1,2,3)")]
    public GameObject[] prefabs;
    // 0: Road (길)
    // 1: Obstacle (장애물)
    // 2: StartBox (시작점 - Tag: StartBox 필수)
    // 3: EndPoint (도착점 - Tag: EndPoint 필수)
    // 4: Player (플레이어 프리팹)

    [Header("설정")]
    public LayerMask groundLayer; // 바닥 감지용 레이어 (Ground)

    [Header("UI 및 카메라 연결")]
    public GameObject editorUI; // 맵 에디터용 UI 
    public GameObject gameUI;   // 게임용 UI (GameUI_Canvas)
    public GameObject editorCamera; // 편집용 카메라
    public GameObject gameCameraManager; // 게임용 카메라 매니저

    [Header("저장/검증 UI")]
    public TMP_InputField costInputField; // 코스트 설정용 입력창
    public TextMeshProUGUI statusText;    // 상태 메시지 표시용 텍스트

    // 데이터 관리
    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, int> placedBlockIDs = new Dictionary<Vector2Int, int>(); // 저장용 ID 딕셔너리

    private int currentID = 0; // 현재 선택된 도구 ID
    private GameObject ghostObject;
    private Material ghostMaterial;

    private GameObject currentPlayerInstance;
    private bool isPlayMode = false;

    // 저장 관련 변수
    private bool isVerified = false; // 클리어 검증 여부
    public int mapMaxCost = 10;      // 이 맵의 코스트 제한 (기본 10)

    void Start()
    {
        // 1. 고스트용 반투명 재질 만들기 (URP 호환)
        SetupGhostMaterial();

        // 초기 선택 (길)
        SelectTool(0);

        // 코스트 입력창 초기값 설정
        if (costInputField != null)
        {
            costInputField.text = mapMaxCost.ToString();
            costInputField.onEndEdit.AddListener(UpdateMaxCost);
        }

        // --- (★ 추가된 자동 로드 기능) ---
        // 메인 화면에서 "이거 플레이해!"라고 쪽지를 보냈는지 확인
        if (PlayerPrefs.HasKey("AutoLoadSlot"))
        {
            int slotToLoad = PlayerPrefs.GetInt("AutoLoadSlot");
            PlayerPrefs.DeleteKey("AutoLoadSlot"); // 쪽지 확인했으니 삭제 (다음에 그냥 들어올 땐 편집 모드여야 하니까)

            // 1. 맵 불러오기
            LoadMap(slotToLoad);

            // 2. 바로 플레이 모드 시작!
            SetPlayMode(true);
        }
    }

    void Update()
    {
        // 플레이 모드이거나 UI 위에 마우스가 있으면 편집 중단
        if (isPlayMode)
        {
            if (ghostObject != null) ghostObject.SetActive(false);
            return;
        }
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (ghostObject != null) ghostObject.SetActive(false);
            return;
        }

        // 마우스 레이캐스트
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // 그리드 스냅 (정수 좌표)
            int x = Mathf.RoundToInt(hit.point.x);
            int z = Mathf.RoundToInt(hit.point.z);
            Vector3 finalPos = new Vector3(x, 0, z);

            // 고스트 이동
            if (ghostObject != null)
            {
                if (!ghostObject.activeSelf) ghostObject.SetActive(true);
                ghostObject.transform.position = finalPos;
            }

            // [좌클릭] 설치
            if (Input.GetMouseButton(0))
            {
                PlaceBlock(new Vector2Int(x, z), finalPos);
            }
            // [우클릭] 삭제
            else if (Input.GetMouseButton(1))
            {
                RemoveBlock(new Vector2Int(x, z));
            }
        }
        else
        {
            if (ghostObject != null) ghostObject.SetActive(false);
        }
    }

    // --- 툴 선택 함수 ---
    public void SelectTool(int id)
    {
        if (id < 0 || id >= prefabs.Length) return;
        currentID = id;
        ChangeGhost(prefabs[id]);
    }

    // --- 설치 로직 ---
    void PlaceBlock(Vector2Int coord, Vector3 pos)
    {
        // 이미 같은 블록이 있으면 무시 (최적화)
        if (placedBlockIDs.ContainsKey(coord) && placedBlockIDs[coord] == currentID) return;

        // 기존 블록이 있다면 삭제 후 설치
        RemoveBlock(coord);

        GameObject newObj = Instantiate(prefabs[currentID], pos, Quaternion.identity);
        placedObjects.Add(coord, newObj);
        placedBlockIDs.Add(coord, currentID); // ID 저장

        // ★ 맵이 수정되었으므로 검증 취소
        isVerified = false;
        ShowMessage("맵이 수정되었습니다. (저장하려면 클리어 검증 필요)");
    }

    // --- 삭제 로직 ---
    void RemoveBlock(Vector2Int coord)
    {
        if (placedObjects.ContainsKey(coord))
        {
            Destroy(placedObjects[coord]);
            placedObjects.Remove(coord);
            placedBlockIDs.Remove(coord);

            // ★ 맵이 수정되었으므로 검증 취소
            isVerified = false;
            ShowMessage("맵이 수정되었습니다. (저장하려면 클리어 검증 필요)");
        }
    }

    // --- 코스트 설정 업데이트 ---
    public void UpdateMaxCost(string value)
    {
        if (int.TryParse(value, out int result))
        {
            mapMaxCost = result;
            isVerified = false; // 코스트를 바꿔도 검증 다시 해야 함
            ShowMessage($"코스트 변경됨: {mapMaxCost} (검증 필요)");
        }
    }

    // --- (★ 핵심) 저장 기능 ---
    public void SaveMap(int slotIndex)
    {
        // 1. 검증 체크
        if (!isVerified)
        {
            ShowMessage("먼저 플레이해서 클리어해야 저장할 수 있습니다!");
            return;
        }

        // 2. 데이터 포장
        MapData data = new MapData();
        data.maxCost = mapMaxCost;
        foreach (var kvp in placedBlockIDs)
        {
            // kvp.Key는 좌표(x,z), kvp.Value는 블록ID
            data.blocks.Add(new BlockData(kvp.Value, kvp.Key.x, kvp.Key.y));
        }

        // 3. JSON 변환 및 저장
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, $"MapSlot_{slotIndex}.json");
        File.WriteAllText(path, json);

        ShowMessage($"슬롯 {slotIndex}에 맵 저장 완료!");
        Debug.Log("Saved to: " + path);
    }

    // --- (★ 핵심) 불러오기 기능 ---
    public void LoadMap(int slotIndex)
    {
        string path = Path.Combine(Application.persistentDataPath, $"MapSlot_{slotIndex}.json");
        if (!File.Exists(path))
        {
            ShowMessage($"슬롯 {slotIndex}에 저장된 맵이 없습니다.");
            return;
        }

        // 1. 파일 읽기
        string json = File.ReadAllText(path);
        MapData data = JsonUtility.FromJson<MapData>(json);

        // 2. 기존 맵 싹 지우기
        ClearAll();

        // 3. 데이터 적용
        mapMaxCost = data.maxCost;
        if (costInputField != null) costInputField.text = mapMaxCost.ToString();

        // 4. 블록 배치
        foreach (var block in data.blocks)
        {
            currentID = block.id; // 임시로 ID 변경
            Vector2Int coord = new Vector2Int(block.x, block.z);
            Vector3 pos = new Vector3(block.x, 0, block.z);

            // PlaceBlock 내부 로직 직접 사용 (검증 초기화 방지 위해)
            if (currentID >= 0 && currentID < prefabs.Length)
            {
                GameObject newObj = Instantiate(prefabs[currentID], pos, Quaternion.identity);
                placedObjects.Add(coord, newObj);
                placedBlockIDs.Add(coord, currentID);
            }
        }

        // 불러온 맵은 이미 검증된 것으로 간주 (바로 플레이 가능)
        isVerified = true;
        ShowMessage($"슬롯 {slotIndex} 불러오기 완료!");
    }

    // --- 플레이 모드에서 클리어 시 호출됨 ---
    public void OnLevelCleared()
    {
        if (isPlayMode)
        {
            isVerified = true;
            ShowMessage("검증 완료! 이제 저장할 수 있습니다.");
        }
    }

    // --- 플레이 모드 전환 (Toggle) ---
    public void SetPlayMode(bool isOn)
    {
        isPlayMode = isOn;

        if (isPlayMode) // [플레이 모드 시작]
        {
            if (ghostObject) ghostObject.SetActive(false);
            if (editorUI) editorUI.SetActive(false);
            if (gameUI) gameUI.SetActive(true);
            if (editorCamera) editorCamera.SetActive(false);
            if (gameCameraManager) gameCameraManager.SetActive(true);

            // StartBox 찾기
            GameObject startBoxObj = null;
            foreach (var obj in placedObjects.Values)
            {
                if (obj.CompareTag("StartBox")) { startBoxObj = obj; break; }
            }

            if (startBoxObj != null)
            {
                // 플레이어 소환 (prefabs[4]가 플레이어라고 가정)
                if (prefabs.Length > 4 && prefabs[4] != null)
                {
                    currentPlayerInstance = Instantiate(prefabs[4]);
                    currentPlayerInstance.transform.position = startBoxObj.transform.position + Vector3.up * 1.33f;

                    // 플레이어에게 StartBox와 코스트 정보 전달
                    var pc = currentPlayerInstance.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.startBox = startBoxObj;
                        pc.maxCommandCost = mapMaxCost; // (★ 저장된 코스트 적용)
                    }

                    // UI 연결
                    var uiConnector = FindObjectOfType<UI_Auto_Connector>();
                    if (uiConnector != null && pc != null) uiConnector.BindPlayer(pc);
                }
                else
                {
                    Debug.LogError("Player Prefab이 연결되지 않았습니다! (Element 4)");
                }
            }
            else
            {
                ShowMessage("시작 지점(StartBox)이 없습니다!");
                SetPlayMode(false); // 복귀
            }
        }
        else // [편집 모드 복귀]
        {
            if (gameUI) gameUI.SetActive(false);
            if (editorUI) editorUI.SetActive(true);
            if (gameCameraManager) gameCameraManager.SetActive(false);
            if (editorCamera != null) editorCamera.SetActive(true);

            if (currentPlayerInstance != null) Destroy(currentPlayerInstance);
            if (ghostObject) ghostObject.SetActive(true);
        }
    }

    // --- 헬퍼 함수들 ---
    void ClearAll()
    {
        foreach (var obj in placedObjects.Values) Destroy(obj);
        placedObjects.Clear();
        placedBlockIDs.Clear();
    }

    void ShowMessage(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }

    void SetupGhostMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");

        ghostMaterial = new Material(shader);
        ghostMaterial.SetFloat("_Surface", 1);
        ghostMaterial.SetFloat("_Blend", 0);
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.renderQueue = 3000;
        ghostMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.5f));
        if (shader.name == "Standard") ghostMaterial.color = new Color(1f, 1f, 1f, 0.5f);
    }

    void ChangeGhost(GameObject prefab)
    {
        if (ghostObject != null) Destroy(ghostObject);
        ghostObject = Instantiate(prefab);
        ghostObject.name = "GhostBlock";

        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) Destroy(col);

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers) rend.sharedMaterial = ghostMaterial;
    }
}