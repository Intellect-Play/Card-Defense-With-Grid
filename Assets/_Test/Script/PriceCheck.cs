using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriceCheck : MonoBehaviour
{
    public static PriceCheck instance;

    public PriceSO priceSO;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }
   
}
