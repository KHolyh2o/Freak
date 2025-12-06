using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemFloater : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("1초에 몇 도씩 회전할지 설정합니다.")]
    public float rotateSpeed = 50.0f;

    [Header("둥둥 떠다니기 설정")]
    [Tooltip("위아래로 움직이는 속도입니다.")]
    public float floatSpeed = 2.0f;

    [Tooltip("위아래로 움직이는 거리(폭)입니다.")]
    public float floatHeight = 0.25f;

    private Vector3 startPos;

    void Start()
    {
        // 게임 시작 시점의 위치를 기준점으로 잡습니다.
        startPos = transform.position;
    }

    void Update()
    {
        // 1. 빙글빙글 돌기 (Y축 기준)
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        // 2. 위아래 둥둥 뜨기 (Mathf.Sin 그래프 활용)
        // 시간(Time.time)이 흐름에 따라 -1 ~ 1 사이를 오가는 Sin 값을 높이에 더해줍니다.
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // 변경된 높이를 적용
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}