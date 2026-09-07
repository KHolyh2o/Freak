using UnityEngine;
using System.Collections.Generic;

public enum BlockType
{
    Empty = 0,
    Floor,     
    Start,     
    End,       
    Tree,      
    Box,       
    Puzzle,    
    Stone      
}

[System.Serializable]
public class BlockDataInfo
{
    public int x;
    public int z;
    public float y;
    public BlockType type;
    public string extraOption;
}

public class MapGenerator : MonoBehaviour
{
    public Dictionary<Vector3, BlockDataInfo> mapDataDict = new Dictionary<Vector3, BlockDataInfo>();

    [Header("프리팹 연결")]
    public GameObject floorPrefab;
    public GameObject startPrefab; 
    public GameObject endPrefab;   
    public GameObject treePrefab;
    public GameObject boxPrefab;
    public GameObject puzzlePrefab;
    public GameObject stonePrefab; 

    [Header("카메라 연동")]
    public CameraSwitcher cameraSwitcher; 

    public void LoadStage(int stageNumber, bool resetPlayer = true)
    {
        if (AIManager.Instance != null)
        {
            AIManager.Instance.ResetStageData();
        }

        string fileName = $"Stage{(stageNumber + 1):D2}_Map";
        TextAsset csvData = Resources.Load<TextAsset>("MapData/" + fileName);

        if (csvData == null) 
        {
            Debug.LogError($"[MapGenerator] {fileName}.csv 파일을 Resources 폴더에서 찾을 수 없습니다.");
            return;
        }

        ParseCSVAndSpawn(csvData.text, resetPlayer);
    }

    public void LoadStageFromString(string csvText, bool resetPlayer = true)
    {
        if (AIManager.Instance != null)
        {
            AIManager.Instance.ResetStageData();
        }
        ParseCSVAndSpawn(csvText, resetPlayer);
    }

    private void ParseCSVAndSpawn(string csvText, bool resetPlayer = true)
    {
        // 1. 기존 블록 초기화
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        mapDataDict.Clear();

        string[] rows = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        // ★ 맵 범위 및 코스트 저장용
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minZ = int.MaxValue;
        int maxZ = int.MinValue;
        int parsedMaxCost = -1; // -1이면 설정 안 됨
        GameObject spawnedStartBox = null;

        // 첫 줄은 헤더이므로 index 1부터
        for (int i = 1; i < rows.Length; i++)
        {
            string rowData = rows[i].Trim();
            if (string.IsNullOrEmpty(rowData)) continue;

            string[] columns = rowData.Split(',');
            if (columns.Length < 4) continue;

            BlockDataInfo block = new BlockDataInfo();
            
            if (!int.TryParse(columns[0], out block.x)) continue;
            if (!int.TryParse(columns[1], out block.z)) continue;

            // Min/Max 갱신
            if (block.x < minX) minX = block.x;
            if (block.x > maxX) maxX = block.x;
            if (block.z < minZ) minZ = block.z;
            if (block.z > maxZ) maxZ = block.z;

            string yStr = columns[2].Trim();
            block.y = string.IsNullOrEmpty(yStr) ? 0f : float.Parse(yStr);

            if (!System.Enum.TryParse(columns[3], true, out block.type))
            {
                Debug.LogWarning($"[MapGenerator] 알 수 없는 블록 타입: {columns[3]} at ({block.x}, {block.z})");
                continue;
            }

            // 장애물은 바닥 위로 (+1)
            if (block.type == BlockType.Tree || block.type == BlockType.Box || 
                block.type == BlockType.Puzzle || block.type == BlockType.Stone)
            {
                block.y += 1f;
            }

            block.extraOption = columns.Length > 4 ? columns[4] : "";
            
            // 6번째 열(Cost) 파싱
            if (columns.Length > 5 && block.type == BlockType.Start)
            {
                if (int.TryParse(columns[5].Trim(), out int parsedCost))
                {
                    parsedMaxCost = parsedCost;
                }
            }

            // 7번째 열(Tags) 파싱 (AI 어시스턴트용)
            if (columns.Length > 6 && block.type == BlockType.Start)
            {
                string tags = columns[6].Trim();
                if (AIManager.Instance != null)
                {
                    AIManager.Instance.SetStageTags(tags);
                }
            }

            Vector3 pos = new Vector3(block.x, block.y, block.z);
            mapDataDict[pos] = block;

            GameObject spawnedBlock = SpawnBlock(block, pos);
            if (block.type == BlockType.Start && spawnedBlock != null)
            {
                spawnedStartBox = spawnedBlock;
            }
        }

        // 2. 맵의 중앙 밎 범위 계산
        if (minX <= maxX && minZ <= maxZ)
        {
            float centerX = (minX + maxX) / 2f;
            float centerZ = (minZ + maxZ) / 2f;
            Vector3 mapCenter = new Vector3(centerX, 0f, centerZ);

            float mapWidth = (maxX - minX) + 1f; // +1은 블록 자신의 크기 보정
            float mapDepth = (maxZ - minZ) + 1f;

            Debug.Log($"[MapGenerator] 생성 완료! 중앙: {mapCenter}, 크기: {mapWidth} x {mapDepth}");

            // 3. 카메라에 맵 크기 정보 전달하여 자동 세팅
            if (cameraSwitcher != null)
            {
                cameraSwitcher.AlignCamerasToMapBounds(mapCenter, mapWidth, mapDepth);
            }
            else
            {
                Debug.LogWarning("[MapGenerator] CameraSwitcher가 연결되어 있지 않아 카메라 자동 세팅을 건너뜁니다.");
            }

            // 4. 플레이어 컨트롤러 업데이트 (맵 생성이 완료된 후 실행)
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null && spawnedStartBox != null)
            {
                pc.startBox = spawnedStartBox; // 새로운 StartBox 할당
                if (parsedMaxCost > 0)
                {
                    pc.maxCommandCost = parsedMaxCost; // 코스트 설정
                    Debug.Log($"[MapGenerator] Start 블록에서 코스트 한도를 {parsedMaxCost}로 설정했습니다.");
                }
                
                if (resetPlayer)
                {
                    pc.ResetGame(); // 할당된 StartBox 기준으로 전체 리셋 (UI 슬롯 등 전부 파괴)
                }
                else
                {
                    pc.ResetPlayerPosition(false); // 위치만 StartBox 기준으로 갱신 (유저 코드 보존 및 코루틴 유지)
                }
            }
        }
    }

    private GameObject SpawnBlock(BlockDataInfo block, Vector3 pos)
    {
        GameObject targetPrefab = null;

        switch (block.type)
        {
            case BlockType.Floor: targetPrefab = floorPrefab; break;
            case BlockType.Start: targetPrefab = startPrefab; break; 
            case BlockType.End: targetPrefab = endPrefab; break;     
            case BlockType.Tree: targetPrefab = treePrefab; break;
            case BlockType.Box: targetPrefab = boxPrefab; break;
            case BlockType.Puzzle: targetPrefab = puzzlePrefab; break;
            case BlockType.Stone: targetPrefab = stonePrefab; break; 
        }

        if (targetPrefab != null)   
        {
            // Start/End 블록은 extraOption에 Y축 회전값(각도)이 들어있음
            Quaternion spawnRotation = Quaternion.identity;
            if ((block.type == BlockType.Start || block.type == BlockType.End) 
                && !string.IsNullOrEmpty(block.extraOption))
            {
                if (float.TryParse(block.extraOption.Trim(), out float yRotation))
                {
                    spawnRotation = Quaternion.Euler(0f, yRotation, 0f);
                }
            }

            return Instantiate(targetPrefab, pos, spawnRotation, this.transform);
        }
        return null;
    }
}
