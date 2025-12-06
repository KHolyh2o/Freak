using System.Collections.Generic;

[System.Serializable]
public class BlockData
{
    public int id; // 0:길, 1:장애물, 2:시작, 3:도착
    public int x;
    public int z;

    public BlockData(int id, int x, int z)
    {
        this.id = id;
        this.x = x;
        this.z = z;
    }
}

[System.Serializable]
public class MapData
{
    public int maxCost; // 맵 코스트 제한
    public List<BlockData> blocks = new List<BlockData>(); // 블록 리스트
}