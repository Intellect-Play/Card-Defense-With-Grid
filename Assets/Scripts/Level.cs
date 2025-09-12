using System;
using UnityEngine;

[Serializable]
public class Level
{
    [Header("Level Configuration")]
    [Tooltip("The sequence of waves that make up this level")]
    public Wave[] waves;

}
