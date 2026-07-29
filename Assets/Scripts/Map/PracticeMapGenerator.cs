using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PracticeMapGenerator : MonoBehaviour
{
    public static PracticeMapGenerator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void GenerateAndSavePracticeMap(string missingSkill)
    {
        // 1. 필요한 스킬을 저장해 둡니다.
        PlayerPrefs.SetString("PracticeSkill", missingSkill);
        PlayerPrefs.Save();
        
        // 2. Practice 씬으로 이동합니다.
        SceneController sc = FindObjectOfType<SceneController>();
        if (sc != null)
        {
            sc.ChangeScene("Practice");
        }
        else
        {
            SceneManager.LoadScene("Practice");
        }
    }

    public string GeneratePracticeMapCSV(string missingSkill)
    {
        switch (missingSkill)
        {
            case "Spatial": return GenerateSpatialMapCSV();
            case "Function": return GenerateFunctionMapCSV();
            case "If": return GenerateIfMapCSV();
            case "While": return GenerateWhileMapCSV();
            case "ObstacleAvoidance": return GenerateObstacleMapCSV();
            default: return GenerateBasicMapCSV();
        }
    }

    private string GenerateSpatialMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int dir = Random.value > 0.5f ? 1 : -1;
        sb.AppendLine($"0,0,0,Start,90,6,Spatial"); // Cost increased to 6 for safety
        sb.AppendLine($"1,0,0,Floor,,,");
        sb.AppendLine($"2,0,0,Floor,,,");
        sb.AppendLine($"2,{dir},0,Floor,,,");
        sb.AppendLine($"2,{dir * 2},0,End,,,");
        return sb.ToString();
    }

    private string GenerateFunctionMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int dir = Random.value > 0.5f ? 1 : -1;
        // Function declaration costs 1 + inner blocks (e.g. 3) = 4. Main panel needs ~3 blocks. Total ~7. 
        sb.AppendLine($"0,0,0,Start,90,8,Function"); // Cost fixed to 8
        sb.AppendLine($"1,0,0,Floor,,,");
        sb.AppendLine($"1,{dir},0,Floor,,,");
        sb.AppendLine($"2,{dir},0,Floor,,,");
        sb.AppendLine($"2,{dir * 2},0,Floor,,,");
        sb.AppendLine($"3,{dir * 2},0,End,,,");
        return sb.ToString();
    }

    private string GenerateIfMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int obsDist = Random.Range(2, 4);
        int dir = Random.value > 0.5f ? 1 : -1;
        
        sb.AppendLine($"0,0,0,Start,90,12,If"); // Cost increased to 12
        
        for (int i = 1; i <= obsDist + 1; i++)
        {
            sb.AppendLine($"{i},0,0,Floor,,,");
            if (i == obsDist) {
                sb.AppendLine($"{i},0,0,Stone,,,");
            }
        }
        
        sb.AppendLine($"{obsDist - 1},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist + 1},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist + 2},0,0,End,,,");
        return sb.ToString();
    }

    private string GenerateWhileMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int length = Random.Range(5, 9);
        sb.AppendLine($"0,0,0,Start,90,4,While"); // Cost increased to 4 for safety
        for (int i = 1; i <= length; i++)
        {
            sb.AppendLine($"{i},0,0,Floor,,,");
        }
        sb.AppendLine($"{length + 1},0,0,End,,,");
        return sb.ToString();
    }

    private string GenerateObstacleMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int obsDist = Random.Range(1, 3);
        int dir = Random.value > 0.5f ? 1 : -1;
        
        sb.AppendLine($"0,0,0,Start,90,10,ObstacleAvoidance"); // Cost increased to 10
        for (int i = 1; i <= obsDist; i++)
        {
            sb.AppendLine($"{i},0,0,Floor,,,");
        }
        sb.AppendLine($"{obsDist},0,0,Tree,,,");
        
        // Fix disconnected path by adding a floor before the obstacle
        sb.AppendLine($"{obsDist - 1},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist + 1},{dir},0,Floor,,,");
        sb.AppendLine($"{obsDist + 1},0,0,End,,,");
        return sb.ToString();
    }

    private string GenerateBasicMapCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("x,z,y,type,extraOption,cost,tags");
        int dist = Random.Range(1, 3);
        sb.AppendLine($"0,0,0,Start,90,6,"); // Cost increased to 6
        for(int i = 1; i <= dist; i++) {
            sb.AppendLine($"{i},0,0,Floor,,,");
        }
        sb.AppendLine($"{dist + 1},0,0,End,,,");
        return sb.ToString();
    }
}
