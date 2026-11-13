using System;
using UnityEngine;

/// <summary>
/// 홀로그램 팬 영상 촬영용. 등록된 오브젝트를 순서대로 하나씩 보여주며
/// 각각 Y축으로 회전시키고 위아래로 살짝 띄운다. (게임 로직과 무관, 촬영 씬 전용)
/// </summary>
public class HoloShowcase : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public string label;
        [Tooltip("보여줄 오브젝트 (프리팹 인스턴스 또는 맵 루트)")]
        public Transform target;
        [Tooltip("1.0 = 기본 크기. 맵처럼 큰 건 줄이고 작은 건 키운다.")]
        public float scale = 1f;
        [Tooltip("회전 축이 되는 위치 오프셋 (맵 중심 맞출 때 사용)")]
        public Vector3 pivotOffset = Vector3.zero;
        [Tooltip("한 바퀴 도는 시간(초). 0이면 전체 기본값 사용. 클수록 느리다.")]
        public float secondsPerTurn = 0f;
    }

    [Header("순서")]
    public Item[] items;
    [Tooltip("오브젝트 하나당 보여주는 시간(초). 회전 한 바퀴의 정수배 권장.")]
    public float secondsPerItem = 8f;
    public bool loop = false;

    [Header("모션")]
    [Tooltip("한 바퀴 도는 데 걸리는 시간(초)")]
    public float secondsPerTurn = 4f;
    public float bobHeight = 0.1f;
    [Tooltip("오브젝트 하나가 보이는 동안 위아래로 왕복하는 횟수(정수). 시작/끝 높이가 같아진다.")]
    public int bobCycles = 2;
    [Tooltip("전환 시 작아졌다 커지는 시간(초). 0이면 즉시 교체.")]
    public float transitionSeconds = 0.5f;
    [Tooltip("오브젝트가 놓일 월드 중심")]
    public Vector3 center = Vector3.zero;

    [Header("고정 보기 (Solo)")]
    [Tooltip("0부터 시작하는 Items 번호. 0 이상이면 그 오브젝트만 계속 보여준다. -1이면 순서대로 재생. Play 중에 바꿔도 즉시 적용.")]
    public int soloIndex = -1;
    [Tooltip("Play 중 숫자키 1~9: 해당 번호 오브젝트 고정, 0: 순서대로 재생으로 복귀")]
    public bool keyboardControl = true;

    [Header("회전축")]
    [Tooltip("켜면 각 오브젝트의 Renderer 영역 중심을 자동으로 회전축으로 사용. Pivot Offset은 그 위에 더해지는 미세 조정값.")]
    public bool autoCenter = true;

    [Header("기즈모 (Scene 뷰 전용)")]
    public bool drawGizmos = true;
    [Tooltip("화면 중앙 기준 원 반지름 (이 안에 오브젝트가 들어오게)")]
    public float gizmoRadius = 3f;

    int index = -1;
    int appliedSolo = -1;
    float timer;
    float shownTime;
    bool Solo => items != null && soloIndex >= 0 && soloIndex < items.Length;
    Vector3[] baseScales;
    Vector3[] autoOffsets;

    void Start()
    {
        if (items == null || items.Length == 0) return;

        baseScales = new Vector3[items.Length];
        autoOffsets = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].target == null) continue;
            baseScales[i] = items[i].target.localScale;
            if (autoCenter) autoOffsets[i] = ComputeCenterOffset(items[i].target);
            ReplaceItemFloaters(items[i].target);
            items[i].target.gameObject.SetActive(false);
        }
        appliedSolo = soloIndex;
        Show(Solo ? soloIndex : 0);
    }

    void Update()
    {
        if (items == null || items.Length == 0 || baseScales == null) return;

        if (keyboardControl) HandleKeys();
        if (soloIndex != appliedSolo)
        {
            appliedSolo = soloIndex;
            if (Solo) Show(soloIndex);
            else if (index < 0) Show(0); // 순차 재생 복귀 (끝나 있었다면 처음부터)
        }

        if (index < 0 || index >= items.Length) return;

        timer += Time.deltaTime;
        shownTime += Time.deltaTime;
        var it = items[index];
        if (it.target != null)
        {
            // 표시 시간 안에 정수 바퀴가 돌도록 속도를 보정 -> 끝 각도 == 시작 각도
            float turn = it.secondsPerTurn > 0f ? it.secondsPerTurn : secondsPerTurn;
            int turns = Mathf.Max(1, Mathf.RoundToInt(secondsPerItem / turn));
            float t01 = Mathf.Clamp01(timer / secondsPerItem);
            float angle = t01 * 360f * turns;
            // 정수 번 왕복 -> 끝 높이 == 시작 높이
            float bob = Mathf.Sin(t01 * Mathf.PI * 2f * Mathf.Max(1, bobCycles)) * bobHeight;

            // 전환: 처음/끝에서 스케일 0 <-> 1 (부드럽게)
            float ease = 1f;
            if (transitionSeconds > 0f)
            {
                float fadeIn = Mathf.Clamp01(shownTime / transitionSeconds);
                float fadeOut = Solo ? 1f : Mathf.Clamp01((secondsPerItem - timer) / transitionSeconds);
                ease = Mathf.SmoothStep(0f, 1f, Mathf.Min(fadeIn, fadeOut));
            }
            float s = Mathf.Max(0.0001f, it.scale * ease);
            it.target.localScale = baseScales[index] * s;

            it.target.rotation = Quaternion.Euler(0f, angle, 0f);
            it.target.position = center + Vector3.up * bob
                                 - it.target.rotation * ((it.pivotOffset + autoOffsets[index]) * s);
        }

        if (Solo)
        {
            // 고정 보기: 전환 없이 같은 오브젝트를 계속 (한 사이클 = 정수 바퀴라 끊김 없음)
            if (timer >= secondsPerItem) timer -= secondsPerItem;
            return;
        }

        if (timer >= secondsPerItem)
        {
            int next = index + 1;
            if (next >= items.Length)
            {
                if (!loop) { Hide(index); index = -1; return; }
                next = 0;
            }
            Show(next);
        }
    }

    /// <summary>
    /// 회전 0도 상태에서 Renderer 전체 영역의 중심이 target 원점에서 얼마나 떨어져 있는지 계산.
    /// </summary>
    static Vector3 ComputeCenterOffset(Transform t)
    {
        bool has = false;
        Bounds b = new Bounds();
        Quaternion oldRot = t.rotation;
        t.rotation = Quaternion.identity;
        foreach (var r in t.GetComponentsInChildren<Renderer>(false))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        Vector3 off = has ? b.center - t.position : Vector3.zero;
        t.rotation = oldRot;
        return off;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.1f);
        Gizmos.DrawLine(center + Vector3.down * gizmoRadius, center + Vector3.up * gizmoRadius);
        Gizmos.color = Color.cyan;
        const int seg = 48;
        Vector3 prev = center + new Vector3(gizmoRadius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * gizmoRadius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    /// <summary>
    /// ItemFloater는 월드 좌표로 위치를 덮어써서 부모(맵)가 돌아도 제자리에 남는다.
    /// 촬영용으로는 로컬 좌표 버전으로 교체한다. (원본 스크립트/프리팹은 그대로)
    /// </summary>
    static void ReplaceItemFloaters(Transform root)
    {
        foreach (var f in root.GetComponentsInChildren<ItemFloater>(true))
        {
            // 최상위는 HoloShowcase가 회전/높이를 직접 제어하므로 원본만 끄고 교체하지 않는다.
            if (f.transform == root) { f.enabled = false; continue; }

            var local = f.GetComponent<HoloLocalFloater>();
            if (local == null) local = f.gameObject.AddComponent<HoloLocalFloater>();
            local.rotateSpeed = f.rotateSpeed;
            local.floatSpeed = f.floatSpeed;
            local.floatHeight = f.floatHeight;
            f.enabled = false;
        }
    }

    void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            soloIndex = -1;
        for (int k = 0; k < 9; k++)
        {
            if (k >= items.Length) break;
            if (Input.GetKeyDown(KeyCode.Alpha1 + k) || Input.GetKeyDown(KeyCode.Keypad1 + k))
                soloIndex = k;
        }
    }

    void Show(int i)
    {
        if (index >= 0 && index < items.Length) Hide(index);
        index = i;
        timer = 0f;
        shownTime = 0f;
        var it = items[i];
        if (it.target == null) return;
        // 인스펙터에서 배열을 늘리면 scale이 0이 되는 경우가 있어 방어
        if (it.scale <= 0f) it.scale = 1f;
        // 첫 프레임에 원래 크기로 번쩍이지 않도록 거의 0에서 시작 (이후 Update가 보간)
        it.target.localScale = baseScales[i] * (transitionSeconds > 0f ? 0.0001f : it.scale);
        it.target.gameObject.SetActive(true);
    }

    void Hide(int i)
    {
        if (items[i].target != null) items[i].target.gameObject.SetActive(false);
    }
}
