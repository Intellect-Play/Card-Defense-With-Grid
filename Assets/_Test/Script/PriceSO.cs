using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Price")]
public class PriceSO : ScriptableObject
{
    public int BuyPrice;
    public int ShufflePrice;
    public int RerollPrice;
    public int DecreaseCooldownPrice;
}
