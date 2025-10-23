using System;
using UnityEngine;

[Serializable]
public class Level
{
    [Header("Level Configuration")]
    [Tooltip("The sequence of waves that make up this level")]
    public Wave[] waves;
    public GridLevelData gridLevelDatas;

}
[Serializable]
public class GridLevelData
{
    [Header("Grid Configuration")]
    public int Width;
    public int Height;

    [Header("Cell Settings")]
    public int CellSize;
    public int LockCellCount;
}