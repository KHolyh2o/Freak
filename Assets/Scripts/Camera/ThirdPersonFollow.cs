using UnityEngine;
using System.Collections.Generic;

public class ThirdPersonFollow : MonoBehaviour
{
    [Header("연결")]
    public Transform cameraTransform; // 자식 카메라 (T_Camera)
    public Transform target;          // 타겟 (Player_Root)

    [Header("설정")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public float fixedXRotation = 20.0f;

    [Header("가림막 처리 설정")]
    public LayerMask obstructionMask; // 가리는 물체로 인식할 레이어
    public float transparencyStep = 0.05f; // 투명도 (0에 가까울수록 투명) - 0.05f 추천
    
    // 원래 재질을 복원하기 위해 저장하는 딕셔너리
    // Key: Renderer, Value: 원래 Material
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    // 현재 가리고 있는 오브젝트들 (매 프레임 갱신)
    private List<Renderer> currentObstructors = new List<Renderer>();

    // 투명 처리를 위한 템플릿 메테리얼 (URP/Standard 호환)
    private Material transparentTemplate;

    void Start()
    {
        // 기본적으로 Player 레이어 빼고 전부 검사하도록 설정 (필요시 Inspector에서 수정)
        if (obstructionMask == 0)
        {
             // Player, UI, Road(바닥) 레이어는 투명화 대상에서 제외
             obstructionMask = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI", "Road");
        }

        // 투명 재질 템플릿 생성 (쉐이더 호환성 문제 해결을 위해 Standard/URP Lit 사용)
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard"); // URP 아니면 스탠다드

        transparentTemplate = new Material(shader);
        transparentTemplate.SetFloat("_Surface", 1); // Transparent
        transparentTemplate.SetFloat("_Blend", 0);   // Alpha
        transparentTemplate.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        transparentTemplate.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        transparentTemplate.SetInt("_ZWrite", 0);
        transparentTemplate.DisableKeyword("_ALPHATEST_ON");
        transparentTemplate.EnableKeyword("_ALPHABLEND_ON");
        transparentTemplate.renderQueue = 3000;
        
        // 초기 색상은 흰색에 반투명
        if (transparentTemplate.HasProperty("_BaseColor")) transparentTemplate.SetColor("_BaseColor", new Color(1,1,1, transparencyStep));
        else transparentTemplate.color = new Color(1,1,1, transparencyStep);
    }

    void LateUpdate()
    {
        if (target == null || cameraTransform == null) return;

        // 1. 위치 이동
        Vector3 desiredPos = target.position + (target.rotation * offset);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPos, smoothSpeed);

        // 2. 회전 (X축 고정, Y축 타겟 추적)
        Quaternion targetRot = Quaternion.Euler(fixedXRotation, target.eulerAngles.y, 0);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, targetRot, smoothSpeed);

        // 3. 가림막 처리 (재질 교체 방식)
        HandleObstructions();
    }

    void HandleObstructions()
    {
        currentObstructors.Clear();
        
        // 카메라 -> 플레이어 방향 거리 계산
        float dist = Vector3.Distance(cameraTransform.position, target.position);
        Vector3 dir = (target.position - cameraTransform.position).normalized;
        // 0.25f 반지름의 구체로 검사 (레이저보다 두꺼워서 잘 감지됨)
        RaycastHit[] hits = Physics.SphereCastAll(cameraTransform.position, 0.25f, dir, dist, obstructionMask);
        
        foreach (var hit in hits)
        {
            // 플레이어 자신은 제외
            if (hit.transform == target) continue;

            // (수정) 부모만 Renderer가 있는 게 아니라, 자식들에게 Mesh가 있는 경우를 대비해
            // 그 오브젝트에 딸린 '모든' Renderer를 다 가져와서 투명하게 만듭니다.
            Renderer[] renderers = hit.collider.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;

                // 현재 가리고 있는 목록에 추가
                currentObstructors.Add(rend);
                
                // 만약 처음 발견된 가림막이라면 -> 재질 교체(Swapping) 실행
                if (!originalMaterials.ContainsKey(rend))
                {
                    // 1. 원본 저장
                    originalMaterials.Add(rend, rend.material); // Instance 저장
                    
                    // 2. 새 투명 재질 생성 (템플릿 복제)
                    Material newMat = new Material(transparentTemplate);
                    
                    // 3. 텍스처 옮기기 (가능하다면)
                    if (rend.material.mainTexture != null)
                    {
                        if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", rend.material.mainTexture);
                        else newMat.mainTexture = rend.material.mainTexture;
                    }

                    // 4. 색상 설정 (투명도 적용)
                    Color baseColor = Color.white;
                    // 원본 색상을 가져올 수 있으면 가져옴
                    if (rend.material.HasProperty("_BaseColor")) baseColor = rend.material.GetColor("_BaseColor");
                    else if (rend.material.HasProperty("_Color")) baseColor = rend.material.color;
                    
                    baseColor.a = transparencyStep;

                    if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", baseColor);
                    else newMat.color = baseColor;

                    // 5. 적용
                    rend.material = newMat;
                }
            }
        }

        // 더 이상 가리지 않는 오브젝트들 복구
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in originalMaterials)
        {
            Renderer r = kvp.Key;
            if (!currentObstructors.Contains(r))
            {
                // 복구 수행
                if (r != null) r.material = kvp.Value; // 원본 재질로 교체
                toRemove.Add(r);
            }
        }

        // 관리 목록에서 제거
        foreach (var r in toRemove)
        {
            originalMaterials.Remove(r);
        }
    }
}