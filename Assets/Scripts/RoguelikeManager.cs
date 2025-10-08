using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoguelikeManager : MonoBehaviour
{
    [Header("UI & Deck References")]
    public UIManager          uiManager;

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

    


    private static string LevelKey(string k) => $"{k}_level";
    private static bool   IsUnlocked(string unlockKey) =>true;




    private void Awake()
    {

        Debug.Log("RoguelikeManager: Awake - Set Deck_Inventor_Active to 1");
    }
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        // initialize every card’s sessionLevel from PlayerPrefs (for unlocked cards)
        InitAllCardsSessionLevelsFromPrefs();

        // Build card lists strictly from PlayerPrefs at scene start
    }

    // --------------------- Setup / Hero pick ---------------------



    public void SetActiveHero(string deckName)
    {
        ActiveHeroDeckName = deckName;
        PlayerPrefs.SetInt($"Hero_{deckName}",        1);
        PlayerPrefs.SetInt($"Deck_{deckName}_Active", 1);
        PlayerPrefs.Save();



       
    }

  

 

   

    /// Rewrite animators strictly from unlocked prefs.
   

    private void ApplyUnlockedToAnimator(CardDeckAnimator anim, string deckName)
    {
   
 
    }

    // --------------------- session-level initialization ---------------------

    private void InitAllCardsSessionLevelsFromPrefs()
    {
      
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

       

        bool TryAssignUpgrade(ref RoguelikeOption opt)
        {
            var decks = new List<string>();
           

            string deck = decks[Random.Range(0, decks.Count)];
       

            opt.type           = OptionType.UpgradeCard;
            opt.deckName       = deck;
            opt.targetDeckName = deck;
            

          
            return true;
        }

        void AssignCooldown(ref RoguelikeOption opt)
        {
           
            //opt.type           = OptionType.ReduceCooldown;
          

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
            Debug.Log(opt.type + " Type");
            switch (opt.type)
            {
                case OptionType.OpenNewWeapon:
                    TetrisWeaponManager.instance.OpenNewWeapon();
                    break;
                case OptionType.UpgradeCard:
                    TetrisWeaponManager.instance.UpgradeWeapon();
                    break;
                  
               
                case OptionType.ReduceCooldown:
                    TetrisWeaponManager.instance.ReduceCooldown();

                    AssignCooldown(ref opt);
                    break;

                case OptionType.HealthUp:
                    AssignCooldown(ref opt);
                    break;
            }

            options[i] = opt;
        }
        void AddPiece()
        {
            Debug.Log("AddPiece method called in RoguelikeManager.");
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

       if (picked.type == OptionType.UpgradeCard)
        {
            //int prev = picked.targetCard.sessionLevel;
            //picked.targetCard.IncrementSessionLevel();
            //picked.oldLevel = prev;
            //picked.newLevel = picked.targetCard.sessionLevel;
        }
        else // ReduceCooldown
        {
        }
    }
}
