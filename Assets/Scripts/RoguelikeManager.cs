using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoguelikeManager : MonoBehaviour
{
    [Header("UI & Deck References")]
    public UIManager          uiManager;
    public CardDeckAnimator[] cardDeckAnimators;

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
    public List<RoguelikeOption> CurrentOptions { get; private set; }

    // PlayerPrefs keys for card unlocks (order MUST match arrays above)
    private readonly Dictionary<string,string[]> cardPrefKeys = new Dictionary<string,string[]>
    {
        { "Inventor", new[]{ "inv_bee","inv_bomb","inv_cog","inv_sword" } },
        { "Wizard",   new[]{ "wiz_dagger","wiz_feather","wiz_ring","wiz_stone" } },
        { "Samurai",  new[]{ "sam_arrow","sam_hammer","sam_knife","sam_shuriken" } },
    };

    private static string LevelKey(string k) => $"{k}_level";
    private static bool   IsUnlocked(string unlockKey) =>true;

    private CardSO[] GetCardsForDeck(string deckName)
    {
        return deckName switch
        {
            "Inventor" => inventorCards,
            "Wizard"   => wizardCards,
            "Samurai"  => samuraiCards,
            _          => null
        };
    }

    private string GetCardKey(string deckName, CardSO card)
    {
        if (!card || !cardPrefKeys.ContainsKey(deckName)) return null;
        var arr  = GetCardsForDeck(deckName);
        if (arr == null) return null;
        int idx  = System.Array.IndexOf(arr, card);
        if (idx < 0) return null;
        var keys = cardPrefKeys[deckName];
        return (idx < keys.Length) ? keys[idx] : null;
    }

    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();

        // initialize every card’s sessionLevel from PlayerPrefs (for unlocked cards)
        InitAllCardsSessionLevelsFromPrefs();

        // Build card lists strictly from PlayerPrefs at scene start
        SyncAnimatorsWithUnlockedPrefs();
    }

    // --------------------- Setup / Hero pick ---------------------

    public void InitializeHeroSelection()
    {
        // Reset active flags only; DO NOT touch card unlock keys or levels here.
        PlayerPrefs.SetInt("Deck_Inventor_Active", 0);
        PlayerPrefs.SetInt("Deck_Wizard_Active",   0);
        PlayerPrefs.SetInt("Deck_Samurai_Active",  0);

        PlayerPrefs.SetInt("Hero_Inventor", 0);
        PlayerPrefs.SetInt("Hero_Wizard",   0);
        PlayerPrefs.SetInt("Hero_Samurai",  0);
        PlayerPrefs.Save();

        SyncAnimatorsWithUnlockedPrefs();
    }

    public void SetActiveHero(string deckName)
    {
        ActiveHeroDeckName = deckName;
        PlayerPrefs.SetInt($"Hero_{deckName}",        1);
        PlayerPrefs.SetInt($"Deck_{deckName}_Active", 1);
        PlayerPrefs.Save();

        InitDeckSessionLevelsFromPrefs(deckName);

        var anim = FindAnimatorByDeck(deckName);
        if (anim != null) ApplyUnlockedToAnimator(anim, deckName);
        else SyncAnimatorsWithUnlockedPrefs();
    }

    public List<RoguelikeOption> GetUnlockedHeroOptions()
    {
        var list = new List<RoguelikeOption>();
        if (PlayerPrefs.GetInt("Hero_Inventor", 0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Inventor", targetHero = inventorSO });
        if (PlayerPrefs.GetInt("Hero_Wizard",   0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Wizard",   targetHero = wizardSO   });
        if (PlayerPrefs.GetInt("Hero_Samurai",  0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Samurai",  targetHero = samuraiSO  });

        if (list.Count == 0)
        {
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Inventor", targetHero = inventorSO });
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Wizard",   targetHero = wizardSO   });
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Samurai",  targetHero = samuraiSO  });
        }
        return list;
    }

    // --------------------- Deck / Card helpers ---------------------

    /// Returns ONLY the unlocked cards for a deck.
    private List<CardSO> GetUnlockedCards(string deckName)
    {
        var result = new List<CardSO>();
        var cards = GetCardsForDeck(deckName);
        if (cards == null || !cardPrefKeys.ContainsKey(deckName)) return result;

        var keys  = cardPrefKeys[deckName];
        int count = Mathf.Min(cards.Length, keys.Length);

        for (int i = 0; i < count; i++)
            if (IsUnlocked(keys[i]))
                result.Add(cards[i]);

        return result;
    }

    private string NameForDeckType(CardDeckAnimator.DeckType t)
        => (t == CardDeckAnimator.DeckType.Inventor) ? "Inventor"
         : (t == CardDeckAnimator.DeckType.Wizard)   ? "Wizard"
                                                    : "Samurai";

    private CardDeckAnimator FindAnimatorByDeck(string deckName)
    {
        if (cardDeckAnimators == null) return null;
        foreach (var a in cardDeckAnimators)
            if (a && NameForDeckType(a.deckType) == deckName) return a;
        return null;
    }

    /// Rewrite animators strictly from unlocked prefs.
    public void SyncAnimatorsWithUnlockedPrefs()
    {
        if (cardDeckAnimators == null) return;
        foreach (var anim in cardDeckAnimators)
        {
            if (!anim) continue;
            var deckName = NameForDeckType(anim.deckType);
            ApplyUnlockedToAnimator(anim, deckName);
        }
    }

    private void ApplyUnlockedToAnimator(CardDeckAnimator anim, string deckName)
    {
        var allowed = GetUnlockedCards(deckName);
        anim.ApplyCardsList(allowed.ToArray()); // STRICT: no fallbacks
    }

    // --------------------- session-level initialization ---------------------

    private void InitAllCardsSessionLevelsFromPrefs()
    {
        InitDeckSessionLevelsFromPrefs("Inventor");
        InitDeckSessionLevelsFromPrefs("Wizard");
        InitDeckSessionLevelsFromPrefs("Samurai");
    }

    private void InitDeckSessionLevelsFromPrefs(string deckName)
    {
        if (!cardPrefKeys.ContainsKey(deckName)) return;

        var keys  = cardPrefKeys[deckName];
        var cards = GetCardsForDeck(deckName);
        if (cards == null) return;

        int count = Mathf.Min(cards.Length, keys.Length);
        for (int i = 0; i < count; i++)
        {
            var c   = cards[i];
            if (!c) continue;

            string unlockKey = keys[i];
            if (IsUnlocked(unlockKey))
            {
                int persistent = PlayerPrefs.GetInt(LevelKey(unlockKey), 1);
                c.sessionLevel = Mathf.Max(1, persistent);
            }
            else
            {
                c.sessionLevel = 0;
            }
        }
    }

    // --------------------- Mid-run roguelike flow ---------------------

    public IEnumerator RunRoguelike(int waveNumber, List<RoguelikeOption> options)
    {
        // Active decks right now
        var activeDecks = new List<string>();
        if (PlayerPrefs.GetInt("Deck_Inventor_Active", 0) == 1) activeDecks.Add("Inventor");
        if (PlayerPrefs.GetInt("Deck_Wizard_Active",   0) == 1) activeDecks.Add("Wizard");
        if (PlayerPrefs.GetInt("Deck_Samurai_Active",  0) == 1) activeDecks.Add("Samurai");
        if (activeDecks.Count == 0) yield break;

        // Unlocked cards by deck (used to choose upgrade targets)
        var unlockedByDeck = new Dictionary<string, List<CardSO>>
        {
            { "Inventor", GetUnlockedCards("Inventor") },
            { "Wizard",   GetUnlockedCards("Wizard")   },
            { "Samurai",  GetUnlockedCards("Samurai")  },
        };

        // Uniqueness guards for this choice screen
        var usedCards  = new HashSet<CardSO>();
        var usedHeroes = new HashSet<string>();
        bool healthUpAddedThisScreen = false;

        // Helpers -------------------------------------------------------
        List<string> InactiveHeroes()
        {
            var list = new List<string>();
            foreach (var d in new[] { "Inventor", "Wizard", "Samurai" })
                if (PlayerPrefs.GetInt($"Deck_{d}_Active", 0) == 0) list.Add(d);
            return list;
        }

        bool TryAssignNewHero(ref RoguelikeOption opt)
        {
            var candidates = InactiveHeroes();
            candidates.RemoveAll(h => usedHeroes.Contains(h));
            if (candidates.Count == 0) return false;

            string deck = candidates[Random.Range(0, candidates.Count)];
            opt.type           = OptionType.NewHero;
            opt.deckName       = deck;
            opt.targetDeckName = deck;
            opt.targetHero     = deck == "Inventor" ? inventorSO
                           : deck == "Wizard"   ? wizardSO
                                                : samuraiSO;
            usedHeroes.Add(deck);
            return true;
        }

        bool TryAssignUpgrade(ref RoguelikeOption opt)
        {
            var decks = new List<string>();
            foreach (var d in activeDecks)
            {
                if (unlockedByDeck.TryGetValue(d, out var list) && list != null &&
                    list.Exists(c => c && !usedCards.Contains(c)))
                {
                    decks.Add(d);
                }
            }
            if (decks.Count == 0) return false;

            string deck = decks[Random.Range(0, decks.Count)];
            var pool = unlockedByDeck[deck].FindAll(c => c && !usedCards.Contains(c));
            var pick = pool[Random.Range(0, pool.Count)];

            opt.type           = OptionType.UpgradeCard;
            opt.deckName       = deck;
            opt.targetDeckName = deck;
            opt.targetCard     = pick;
            opt.oldLevel       = pick.sessionLevel;
            opt.newLevel       = pick.sessionLevel + 1;

            usedCards.Add(pick);
            return true;
        }

        void AssignCooldown(ref RoguelikeOption opt)
        {
            string deck = activeDecks[Random.Range(0, activeDecks.Count)];
            opt.type           = OptionType.ReduceCooldown;
            opt.deckName       = deck;
            opt.targetDeckName = deck;

            var deckCards = GetCardsForDeck(deck);
            opt.targetCard = (deckCards != null && deckCards.Length > 0) ? deckCards[^1] : null;
        }

        bool TryAssignHealthUp(ref RoguelikeOption opt)
        {
            if (!enableHealthUp) return false;
            if (limitOneHealthUpPerScreen && healthUpAddedThisScreen) return false;

            opt.type           = OptionType.HealthUp;
            // Use any active deck name so UIManager closes the board on click (no functional side-effects).
            opt.deckName       = activeDecks[0];
            opt.targetDeckName = activeDecks[0];

            // Use the chosen CardSO for visuals
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

            switch (opt.type)
            {
                case OptionType.NewHero:
                    if (!TryAssignNewHero(ref opt))
                    {
                        if (!TryAssignUpgrade(ref opt))
                            AssignCooldown(ref opt);
                    }
                    break;

                case OptionType.UpgradeCard:
                    if (!TryAssignUpgrade(ref opt))
                    {
                        if (!TryAssignNewHero(ref opt))
                            AssignCooldown(ref opt);
                    }
                    break;

                case OptionType.ReduceCooldown:
                    AssignCooldown(ref opt);
                    break;

                case OptionType.HealthUp:
                    if (!TryAssignHealthUp(ref opt))
                    {
                        // If we can't add Health Up, fall back to something useful.
                        if (!TryAssignUpgrade(ref opt))
                        {
                            if (!TryAssignNewHero(ref opt))
                                AssignCooldown(ref opt);
                        }
                    }
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
        var anim = cardDeckAnimators[idx];
        anim.gameObject.SetActive(true);

        if (picked.type == OptionType.NewHero)
        {
            SetActiveHero(picked.deckName);
        }
        else if (picked.type == OptionType.UpgradeCard)
        {
            int prev = picked.targetCard.sessionLevel;
            picked.targetCard.IncrementSessionLevel();
            picked.oldLevel = prev;
            picked.newLevel = picked.targetCard.sessionLevel;
        }
        else // ReduceCooldown
        {
            anim.ReduceShuffleDelay(20);
        }
    }
}
