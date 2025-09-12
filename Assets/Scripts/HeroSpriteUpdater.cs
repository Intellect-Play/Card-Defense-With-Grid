// HeroSpriteUpdater.cs
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor; // editor-only
#endif

public class HeroSpriteUpdater : MonoBehaviour
{
    [Header("Hero Sprites")]
    public Sprite samuraiSprite;
    public Sprite wizardSprite;
    public Sprite inventorSprite;

    void Start()
    {
        UpdateHeroSprites();
    }

    public void UpdateHeroSprites()
    {
        // Runtime-friendly: try Resources first (requires Assets/Resources/... paths)
        if (samuraiSprite == null)  samuraiSprite  = Resources.Load<Sprite>("Sprites/Hero/samurai");
        if (wizardSprite == null)   wizardSprite   = Resources.Load<Sprite>("Sprites/Hero/Wizard");
        if (inventorSprite == null) inventorSprite = Resources.Load<Sprite>("Sprites/Hero/inventor");

        // Editor-only fallback: safe in Editor, stripped in builds
        #if UNITY_EDITOR
        if (samuraiSprite == null)
            samuraiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hero/samurai.png");
        if (wizardSprite == null)
            wizardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hero/Wizard.png");
        if (inventorSprite == null)
            inventorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hero/inventor.png");
        #endif

        // Find all UI Images named "Character" and update by Y position rule
        var images = GameObject.FindObjectsOfType<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name != "Character") continue;

            // If these are UI elements, anchoredPosition.y may be more appropriate:
            // var rt = img.transform as RectTransform;
            // float y = (rt != null) ? rt.anchoredPosition.y : img.transform.position.y;
            float y = img.transform.position.y;

            if (y > 1590f) // Inventor (highest)
            {
                if (inventorSprite != null)
                {
                    img.sprite = inventorSprite;
                    Debug.Log("Updated Inventor character sprite");
                }
            }
            else if (y > 1570f) // Samurai (middle-high)
            {
                if (samuraiSprite != null)
                {
                    img.sprite = samuraiSprite;
                    Debug.Log("Updated Samurai character sprite");
                }
            }
            else if (y > 1560f) // Wizard (middle)
            {
                if (wizardSprite != null)
                {
                    img.sprite = wizardSprite;
                    Debug.Log("Updated Wizard character sprite");
                }
            }
        }
    }
}
