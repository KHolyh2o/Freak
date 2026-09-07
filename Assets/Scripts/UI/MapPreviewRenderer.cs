using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapPreviewRenderer : MonoBehaviour
{
    public static MapPreviewRenderer Instance;

    [Header("프리뷰 렌더링 설정")]
    public int renderTextureWidth = 512;
    public int renderTextureHeight = 512;
    public Vector3 previewWorldOffset = new Vector3(0, -1000f, 0); // 메인 게임과 안 겹치게 아주 먼 곳에 생성
    public float spacing = 100f; // 프리뷰 맵끼리의 간격
    public Material skyboxMaterial; // 프리뷰용 배경 (비워두면 투명/단색)

    private List<RenderTexture> activeTextures = new List<RenderTexture>();
    private List<GameObject> activePreviewMaps = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// CSV 텍스트와 원본 프리팹들을 받아 미니어처 3D 맵을 생성하고 RenderTexture를 반환합니다.
    /// </summary>
    public RenderTexture GeneratePreview(int stageIndex, string csvText, MapGenerator originalGenerator)
    {
        // 1. 렌더 텍스처 생성
        RenderTexture rt = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        rt.Create();
        activeTextures.Add(rt);

        // 2. 맵 프리뷰 루트 오브젝트 생성 (서로 겹치지 않게 간격 벌림)
        Vector3 basePos = previewWorldOffset + new Vector3(stageIndex * spacing, 0, 0);
        GameObject mapRoot = new GameObject($"PreviewMap_Stage_{stageIndex}");
        mapRoot.transform.position = basePos;
        activePreviewMaps.Add(mapRoot);

        // 3. CSV 파싱 및 블록 배치 (MapGenerator 로직 간소화)
        string[] rows = csvText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        for (int i = 1; i < rows.Length; i++)
        {
            string[] cols = rows[i].Split(',');
            if (cols.Length < 4) continue;

            if (!int.TryParse(cols[0], out int x) || !int.TryParse(cols[1], out int z)) continue;
            
            float y = string.IsNullOrEmpty(cols[2].Trim()) ? 0f : float.Parse(cols[2].Trim());
            if (!System.Enum.TryParse(cols[3], true, out BlockType type)) continue;

            if (type == BlockType.Tree || type == BlockType.Box || type == BlockType.Puzzle || type == BlockType.Stone)
                y += 1f;

            string extra = cols.Length > 4 ? cols[4] : "";
            Quaternion rot = Quaternion.identity;
            if ((type == BlockType.Start || type == BlockType.End) && !string.IsNullOrEmpty(extra))
            {
                if (float.TryParse(extra.Trim(), out float yRot)) rot = Quaternion.Euler(0, yRot, 0);
            }

            GameObject prefabToSpawn = null;
            switch (type)
            {
                case BlockType.Floor: prefabToSpawn = originalGenerator.floorPrefab; break;
                case BlockType.Start: prefabToSpawn = originalGenerator.startPrefab; break;
                case BlockType.End: prefabToSpawn = originalGenerator.endPrefab; break;
                case BlockType.Tree: prefabToSpawn = originalGenerator.treePrefab; break;
                case BlockType.Box: prefabToSpawn = originalGenerator.boxPrefab; break;
                case BlockType.Puzzle: prefabToSpawn = originalGenerator.puzzlePrefab; break;
                case BlockType.Stone: prefabToSpawn = originalGenerator.stonePrefab; break;
            }

            if (prefabToSpawn != null)
            {
                GameObject block = Instantiate(prefabToSpawn, basePos + new Vector3(x, y, z), rot, mapRoot.transform);
                
                // 프리뷰용이므로 모든 충돌체, 물리, 스크립트 기능 비활성화 (순수 그래픽만 남김)
                StripLogicComponents(block);

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }
        }

        // 4. 카메라 세팅
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(mapRoot.transform);
        Camera cam = camObj.AddComponent<Camera>();
        
        // ★ URP 렌더링 호환성을 위한 추가 설정 (이게 없으면 렌더텍스처가 까맣게 나올 수 있음)
        var urpCamData = camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpCamData.renderPostProcessing = false; // 성능 최적화
        
        // 배경을 투명으로 설정 (유저 요청)
        cam.clearFlags = skyboxMaterial != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        if (skyboxMaterial != null) cam.gameObject.AddComponent<Skybox>().material = skyboxMaterial;
        else cam.backgroundColor = new Color(0, 0, 0, 0);

        cam.targetTexture = rt;
        
        // 5. 카메라 앵글 잡기 (플레이 씬 CameraSwitcher와 동일한 로직 적용)
        float mapWidth = (maxX - minX) + 1f;
        float mapDepth = (maxZ - minZ) + 1f;
        float centerX = (minX + maxX) / 2f;
        float centerZ = (minZ + maxZ) / 2f;
        Vector3 mapCenter = basePos + new Vector3(centerX, 0, centerZ);

        float mapRadius = Mathf.Max(mapWidth, mapDepth) * 0.5f * 1.25f; // 플레이 씬과 동일한 1.25배 여백
        float distance = mapRadius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        
        // UI에서는 맵이 너무 작게 보이지 않도록 최소 거리 제한을 대폭 완화
        if (distance < 5f) distance = 5f;

        // 쿼터뷰 각도 (Pitch: 35, Yaw: 45)
        Quaternion camRot = Quaternion.Euler(35f, 45f, 0f);
        cam.transform.position = mapCenter + (camRot * Vector3.back) * distance;
        cam.transform.rotation = camRot;

        return rt;
    }

    private void StripLogicComponents(GameObject go)
    {
        // 최적화를 위해 MeshRenderer와 MeshFilter만 남기고 다 끕니다.
        MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = false;

        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = false;

        Rigidbody[] rbs = go.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs) rb.isKinematic = true;

        ParticleSystem[] ps = go.GetComponentsInChildren<ParticleSystem>();
        foreach (var p in ps) p.Stop();
    }

    private void OnDestroy()
    {
        foreach (var rt in activeTextures)
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
        activeTextures.Clear();
    }
}
