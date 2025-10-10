using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RoguelikeManager : MonoBehaviour
{
    [Header("UI & Deck References")]
    public UIManager uiManager;

    [Header("Hero Definitions")]
    public HeroSO inventorSO;
    public HeroSO wizardSO;
    public HeroSO samuraiSO;

    [Header("Hero Deck Cards (must match key order)")]
    public CardSO[] inventorCards;
    public CardSO[] wizardCards;
    public CardSO[] samuraiCards;

    [Header("Game Ref (optional; auto-found if empty)")]
    public GameManager gameManager;

    [Header("Utility Options")]
    [Tooltip("If ON, Health Up can appear in roguelike choices.")]
    public bool enableHealthUp = true;

    [Tooltip("Default heal amount if the option in a Wave doesn't specify one.")]
    public int defaultHealthUpAmount = 25;

    [Tooltip("Pick a CardSO to represent the Health Up option (art, frame, etc.).")]
    public CardSO healthUpCard;

    [Tooltip("Prevents more than one Health Up from spawning in the same pick screen.")]
    public bool limitOneHealthUpPerScreen = true;

    // runtime state
    public string ActiveHeroDeckName { get; private set; }
    public List<RoguelikeOption> CurrentOptions;
    public List<Image> SelectedSprites;
    public int SelectedCount=0;
    private WeaponSetting weaponSetting;

    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

    }





    public void SetActiveHero(string deckName)
    {
        ActiveHeroDeckName = deckName;
        PlayerPrefs.SetInt($"Hero_{deckName}",        1);
        PlayerPrefs.SetInt($"Deck_{deckName}_Active", 1);
        PlayerPrefs.Save();



       
    }





    // --------------------- Mid-run roguelike flow ---------------------

    public IEnumerator RunRoguelike(int waveNumber, List<RoguelikeOption> options)
    {
        Debug.Log($"RoguelikeManager: Running roguelike options for wave {waveNumber}.");
        // Active decks right now
   
       

        // Unlocked cards by deck (used to choose upgrade targets)
     
        // Uniqueness guards for this choice screen
        var usedCards  = new HashSet<CardSO>();
        var usedHeroes = new HashSet<string>();
        bool healthUpAddedThisScreen = false;

       

        void TryAssignUpgrade(ref RoguelikeOption opt)
        {
            var decks = new List<string>();
           

            string deck = decks[Random.Range(0, decks.Count)];
       

            opt.type           = OptionType.UpgradeCard;
            opt.deckName       = deck;
            opt.targetDeckName = deck;
            

          
          
        }
        void AssignNewWeapon(ref RoguelikeOption opt)
        {

            opt.oldLevel = 0;
            opt.newLevel = 0;
            opt.overrideArtwork = weaponSetting.slotWeaponsSO.openNewWeaponSprite;
        }
        void AssignUpgrade(ref RoguelikeOption opt)
        {

            opt.oldLevel = weaponSetting.Level;
            opt.newLevel = weaponSetting.Level+1;
            opt.overrideArtwork = weaponSetting.slotWeaponsSO.GetNewIcon(opt.newLevel);
        }
        void AssignCooldown(ref RoguelikeOption opt)
        {

            opt.oldLevel = weaponSetting.LevelCountdawn;
            opt.newLevel = weaponSetting.LevelCountdawn + 1;
            opt.overrideArtwork = weaponSetting.slotWeaponsSO.openNewWeaponSprite;
        }
        bool TryAssignHealthUp(ref RoguelikeOption opt)
        {
            if (!enableHealthUp) return false;
            if (limitOneHealthUpPerScreen && healthUpAddedThisScreen) return false;

            opt.type           = OptionType.HealthUp;
         
            opt.targetCard     = healthUpCard;

            // Amount: keep designer-provided value if set; otherwise default
            if (opt.healthAmount <= 0) opt.healthAmount = Mathf.Max(1, defaultHealthUpAmount);

            healthUpAddedThisScreen = true;
            return true;
        }
        // ---------------------------------------------------------------

        // Build/repair each slot while enforcing uniqueness + fallbacks
        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            //Debug.Log(opt.type + " Type");
            switch (opt.type)
            {
                case OptionType.OpenNewWeapon:
                    weaponSetting = TetrisWeaponManager.instance.SelectWeaponforUnlock();
                    AssignNewWeapon(ref opt);
                    break;
                case OptionType.UpgradeCard:
                    weaponSetting = TetrisWeaponManager.instance.SelectWeaponforUpgrade();
                    AssignUpgrade(ref opt);
                    break;
                  
               
                case OptionType.ReduceCooldown:
                    weaponSetting = TetrisWeaponManager.instance.SelectWeaponforReduceCooldown();
                    AssignCooldown(ref opt);
                    break;

                case OptionType.HealthUp:
                    TryAssignHealthUp(ref opt);
                    break;
            }

            options[i] = opt;
        }
 
        // Show UI
        CurrentOptions = options;
        uiManager.SetRougelikeText($"Wave {waveNumber} complete!\nChoose an option:");
        uiManager.ShowRoguelikeOptions();
        uiManager.DisableSlotsWithoutImage();
        uiManager.SetRougelikeBoardActive(true);

        // Wait for selection
        yield return new WaitUntil(() => !uiManager.roguelikeBoard.activeSelf);

        int choice = uiManager.selectedRoguelikeIndex;
        if (choice < 0 || choice >= options.Count) yield break;

        var picked = options[choice];

        // Health Up is handled right here and returns
        if (picked.type == OptionType.HealthUp)
        {
            if (gameManager) gameManager.HealFortress(Mathf.Max(1, picked.healthAmount));
            yield break;
        }

        int idx = picked.deckName == "Inventor" ? 0
                : picked.deckName == "Wizard"   ? 1
                                                : 2;
        SelectedSprites[SelectedCount].color = new Color(1, 1, 1, 1);
        SelectedSprites[SelectedCount].sprite=picked.GetArtwork();
        SelectedCount++;
        switch (picked.type)
        {
            case OptionType.OpenNewWeapon:
                TetrisWeaponManager.instance.OpenNewWeapon();
                break;
            case OptionType.UpgradeCard:
                TetrisWeaponManager.instance.UpgradeWeapon();
                break;


            case OptionType.ReduceCooldown:
                TetrisWeaponManager.instance.ReduceCooldown();


                break;

            case OptionType.HealthUp:
               
                break;
        }
    }
}
