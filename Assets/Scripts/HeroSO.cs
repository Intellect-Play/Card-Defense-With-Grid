using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHero", menuName = "ScriptableObjects/HeroSO", order = 4)]
public class HeroSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name of the hero")]
    public string heroName;

    [Tooltip("Card prefab of the hero")]
    public GameObject heroCard;

    [Tooltip("Image name of the hero")]
    public Sprite heroImg;

    [Tooltip("Card of the hero")]
    public Sprite cardImg;

    [Tooltip("Hero cards")]
    public CardSO[] heroCards;
    
    
}
