using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class RoguelikeButton : MonoBehaviour
{
    public Image buttonImage;
    public List<Image> selectedRoguelikes;
    public TextMeshProUGUI nameText;

    public void GetSetting(Sprite mainSprite, int Level, string texts)
    {
        buttonImage.sprite = mainSprite;
        nameText.text = texts;
        for (int i = 0; i < selectedRoguelikes.Count; i++)
        {
            if (i < Level)
            {
                selectedRoguelikes[i].enabled = true;
            }
            else
            {
                selectedRoguelikes[i].enabled = false;
            }
        }
    }
}
