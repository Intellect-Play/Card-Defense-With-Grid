using System;
using UnityEngine;

public enum SlotState
{
    Locked,
    Purchaseable,
    Empty,
    Filled
}

[Serializable]
public class CardTypeSlot
{
    public GameObject contentBoard;

    [Tooltip("The UI holder for this slot (must have an Image)")]
    public GameObject slot;

    [Tooltip("How much gold it costs to unlock")]
    public int unlockFee;

    [Tooltip("Which CardSO is placed in this slot")]
    public CardSO cardSO;

    [Tooltip("Lock icon overlay")]
    public GameObject icon;

    [Tooltip("Gold+fee display")]
    public GameObject goldObj;

    [Tooltip("Computed slot state")]
    public SlotState state = SlotState.Empty;
}
