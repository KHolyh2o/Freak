using UnityEngine;
using System.Collections.Generic;

namespace Showroom
{
    public class ShowroomManager : MonoBehaviour
    {
        [Header("맵 프리팹 연결 (MapGenerator와 동일하게)")]
        public GameObject floorPrefab;
        public GameObject startPrefab; 
        public GameObject endPrefab;   
        public GameObject treePrefab;
        public GameObject boxPrefab;
        public GameObject puzzlePrefab;
        public GameObject stonePrefab; 

        [Header("전시 설정")]
        public int totalStages = 15;
        public float gridSpacingX = 12f; // 맵 간 좌우 간격 (줄임)
        public float gridSpacingZ = 12f; // 맵 간 상하 간격 (줄임)
        public int columns = 5;          // 한 줄에 몇 개의 맵을 전시할지
        public float miniatureScale = 0.3f; // 맵 미니어처 스케일
        
        [Header("텍스트 라벨 설정")]
        public float labelHeight = 5f;          // 기본 글씨 높이 (기존 10f에서 5f로 낮춤)
        public float labelCharacterSize = 0.4f; // 글씨 스케일
        public int labelFontSize = 50;          // 글씨 해상도

        [Header("상호작용 안내 텍스트 (가까이 갔을 때)")]
        public int guiFontSize = 24;            // 안내 문구 해상도
        public Color guiTextColor = Color.yellow; // 안내 문구 색상
        public float guiOffsetY = 100f;         // 안내 문구를 맵에서 위로 얼마나 띄울지 (화면 픽셀 단위)

        [Header("플레이어 및 상호작용")]
        public Transform playerTransform; // 씬에 있는 Player 할당

        void Start()
        {
            GenerateMuseum();
        }

        void GenerateMuseum()
        {
            for (int i = 0; i < totalStages; i++)
            {
                int stageNum = i + 1; // 1 ~ 15
                string fileName = $"Stage{stageNum:D2}_Map";
                TextAsset csvData = Resources.Load<TextAsset>("MapData/" + fileName);
                
                if (csvData == null) 
                {
                    Debug.LogWarning($"[ShowroomManager] {fileName} 맵 데이터를 찾을 수 없어 스킵합니다.");
                    continue;
                }

                // 1. 전시대 Root 생성
                GameObject stageRoot = new GameObject($"Pedestal_Stage{stageNum}");
                
                // 2. 위치 계산 (그리드 배치)
                int row = i / columns;
                int col = i % columns;
                
                // 좌우(X축)는 중앙 정렬
                float offsetX = (col - (columns - 1) / 2f) * gridSpacingX;
                
                // 플레이어가 (0,0,0)에 있으므로, 맨 앞줄(row=0, 즉 1~5스테이지)이 플레이어와 가장 가까운 Z=15 지점에 배치되도록 설정
                // 뒷줄로 갈수록 Z값이 커져서 더 멀어짐
                float offsetZ = 15f + (row * gridSpacingZ);
                
                Vector3 pos = new Vector3(offsetX, 0, offsetZ); 
                stageRoot.transform.position = pos;

                // 3. 맵 생성 (CSV 읽어서 Root의 자식으로)
                ParseAndGenerate(csvData, stageRoot.transform);

                // 4. 미니어처 스케일 축소
                stageRoot.transform.localScale = new Vector3(miniatureScale, miniatureScale, miniatureScale);

                // 5. 인터랙션용 콜라이더 추가 (클릭 감지용 큰 박스)
                BoxCollider box = stageRoot.AddComponent<BoxCollider>();
                box.size = new Vector3(25f, 15f, 25f); // 로컬 스케일 기준 크기
                box.center = new Vector3(0f, 2f, 0f);
                box.isTrigger = true;

                // 6. 상호작용 컴포넌트 추가
                StageInteractable interactable = stageRoot.AddComponent<StageInteractable>();
                interactable.stageNumber = stageNum;
                interactable.playerTransform = playerTransform;
                // 인스펙터 값을 전달
                interactable.guiFontSize = guiFontSize;
                interactable.guiTextColor = guiTextColor;
                interactable.guiOffsetY = guiOffsetY;
                
                // 7. 허공에 떠 있는 스테이지 이름 라벨 달아주기
                GameObject textObj = new GameObject("StageLabel");
                textObj.transform.SetParent(stageRoot.transform);
                
                // 인스펙터에서 설정한 높이값(labelHeight)을 사용합니다.
                textObj.transform.localPosition = new Vector3(0, labelHeight, 0); 
                
                TextMesh tm = textObj.AddComponent<TextMesh>();
                tm.text = $"Stage {stageNum}";
                tm.characterSize = labelCharacterSize; // 인스펙터에서 설정한 스케일 적용
                tm.fontSize = labelFontSize;           // 인스펙터에서 설정한 폰트 해상도 적용
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
                
                // 쿼터뷰용 고정 회전을 지우고, 카메라를 항시 바라보도록 Billboard 스크립트를 부착합니다.
                textObj.AddComponent<Billboard>();
            }
            
            Debug.Log("[ShowroomManager] 박물관 맵 배치 완료!");
        }

        // MapGenerator의 파싱 로직을 차용하여 맵 중앙을 기준으로 배치
        void ParseAndGenerate(TextAsset csvData, Transform parent)
        {
            string[] rows = csvData.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;
            
            List<BlockDataInfo> blocks = new List<BlockDataInfo>();

            // CSV 데이터 파싱 (헤더 건너뛰고 index 1부터)
            for (int i = 1; i < rows.Length; i++)
            {
                string rowData = rows[i].Trim();
                if (string.IsNullOrEmpty(rowData)) continue;
                
                string[] columnsArr = rowData.Split(',');
                if (columnsArr.Length < 4) continue;

                BlockDataInfo block = new BlockDataInfo();
                if (!int.TryParse(columnsArr[0], out block.x)) continue;
                if (!int.TryParse(columnsArr[1], out block.z)) continue;
                
                if (block.x < minX) minX = block.x;
                if (block.x > maxX) maxX = block.x;
                if (block.z < minZ) minZ = block.z;
                if (block.z > maxZ) maxZ = block.z;

                string yStr = columnsArr[2].Trim();
                block.y = string.IsNullOrEmpty(yStr) ? 0f : float.Parse(yStr);

                if (!System.Enum.TryParse(columnsArr[3], true, out block.type)) continue;

                // 장애물, 박스 등은 1칸 위에 배치
                if (block.type == BlockType.Tree || block.type == BlockType.Box || 
                    block.type == BlockType.Puzzle || block.type == BlockType.Stone)
                {
                    block.y += 1f;
                }

                block.extraOption = columnsArr.Length > 4 ? columnsArr[4] : "";
                blocks.Add(block);
            }

            // 맵을 중앙(0,0,0)을 기준으로 정렬하기 위한 오프셋
            float centerX = minX <= maxX ? (minX + maxX) / 2f : 0f;
            float centerZ = minZ <= maxZ ? (minZ + maxZ) / 2f : 0f;

            foreach (var block in blocks)
            {
                Vector3 pos = new Vector3(block.x - centerX, block.y, block.z - centerZ);
                GameObject targetPrefab = GetPrefab(block.type);

                if (targetPrefab != null)
                {
                    Quaternion spawnRotation = Quaternion.identity;
                    if ((block.type == BlockType.Start || block.type == BlockType.End) && !string.IsNullOrEmpty(block.extraOption))
                    {
                        if (float.TryParse(block.extraOption.Trim(), out float yRotation))
                        {
                            spawnRotation = Quaternion.Euler(0f, yRotation, 0f);
                        }
                    }
                    
                    GameObject obj = Instantiate(targetPrefab, parent.position + pos, spawnRotation, parent);
                    
                    // 미니어처 맵과 플레이어 간의 원치 않는 물리 충돌(공중에 뜨는 현상 등)을 방지하기 위해 모든 콜라이더 제거
                    Collider[] cols = obj.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) Destroy(c);
                }
            }
        }

        GameObject GetPrefab(BlockType type)
        {
            switch (type)
            {
                case BlockType.Floor: return floorPrefab;
                case BlockType.Start: return startPrefab; 
                case BlockType.End: return endPrefab;     
                case BlockType.Tree: return treePrefab;
                case BlockType.Box: return boxPrefab;
                case BlockType.Puzzle: return puzzlePrefab;
                case BlockType.Stone: return stonePrefab; 
            }
            return null;
        }
    }

    // 텍스트가 항상 카메라를 바라보게 만드는 빌보드 스크립트
    public class Billboard : MonoBehaviour
    {
        private Camera mainCam;

        void Start()
        {
            mainCam = Camera.main;
            if (mainCam == null) mainCam = FindObjectOfType<Camera>();
        }

        void LateUpdate()
        {
            if (mainCam != null)
            {
                // 텍스트가 카메라 방향을 향하게 회전 (180도 뒤집히는 것을 방지하기 위해 forward 활용)
                transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                                 mainCam.transform.rotation * Vector3.up);
            }
        }
    }
}
