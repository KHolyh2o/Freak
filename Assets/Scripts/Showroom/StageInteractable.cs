using UnityEngine;
using UnityEngine.SceneManagement;

namespace Showroom
{
    public class StageInteractable : MonoBehaviour
    {
        public int stageNumber;
        public Transform playerTransform;
        public float interactDistance = 8f;

        // ShowroomManager에서 전달받는 설정값
        public int guiFontSize = 24;
        public Color guiTextColor = Color.yellow;
        public float guiOffsetY = 100f;

        private void OnMouseDown()
        {
            // 거리가 가깝거나, 플레이어 정보가 할당 안되었을 때 바로 클릭으로 로드
            if (playerTransform == null || Vector3.Distance(transform.position, playerTransform.position) <= interactDistance)
            {
                LoadStage();
            }
            else
            {
                Debug.Log($"[Showroom] {stageNumber}번 스테이지에 접근하기엔 너무 멉니다.");
            }
        }

        private void Update()
        {
            // 플레이어가 가까이 있을 때 Space바 입력 처리
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactDistance)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    LoadStage();
                }
            }
        }

        private void OnGUI()
        {
            // 가까이 있을 때 화면에 가이드 텍스트 띄워주기 (간이 UI)
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactDistance)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
                // 화면 앞쪽에 있을 때만 (뒤통수에 있지 않을 때)
                if (screenPos.z > 0)
                {
                    GUIStyle style = new GUIStyle();
                    style.fontSize = guiFontSize;
                    style.normal.textColor = guiTextColor;
                    style.alignment = TextAnchor.MiddleCenter;
                    
                    // guiOffsetY 값에 따라 텍스트의 높낮이가 결정됨
                    GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - guiOffsetY, 300, 50), 
                        $"[Stage {stageNumber}]\n클릭 또는 스페이스바로 플레이!", style);
                }
            }
        }

        public void LoadStage()
        {
            Debug.Log($"[Showroom] 스테이지 {stageNumber} 선택 → Play 씬으로 이동");
            // 기존 씬 방식과 동일하게 선택된 스테이지 저장 (0-indexed 라면 stageNumber - 1)
            PlayerPrefs.SetInt("SelectedStage", stageNumber - 1); 
            
            // 쇼룸에서 출발했다는 표식 및 플레이어의 현재 위치와 방향을 기억
            PlayerPrefs.SetInt("ReturnToShowroom", 1);
            if (playerTransform != null)
            {
                PlayerPrefs.SetFloat("ShowroomPosX", playerTransform.position.x);
                PlayerPrefs.SetFloat("ShowroomPosZ", playerTransform.position.z);
                PlayerPrefs.SetFloat("ShowroomRotY", playerTransform.rotation.eulerAngles.y);
            }
            PlayerPrefs.Save();
            
            SceneManager.LoadScene("Play");
        }
    }
}
