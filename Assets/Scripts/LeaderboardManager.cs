using System;
using System.Collections;
using UnityEngine;
using TMPro;

[Serializable]
public class LeaderboardItem
{
    public int    index;
    public string name;
    public int    cups;
}

[Serializable]
public class LeaderboardData
{
    public LeaderboardItem[] items;
}

public class LeaderboardManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Must match number of placeholder children under Content")]
    public LeaderboardItem[] lBoardItems;

    [Header("References")]
    [Tooltip("Parent transform whose children are ListFrame01_Blue placeholders")]
    public Transform content;
    [Tooltip("Your single 'You' ListFrame01_Blue placeholder")]
    public GameObject youBoard;

    [Header("Update Settings")]
    [Tooltip("Interval in seconds between fake updates")]
    public float updateInterval = 100f;

    // Example names to pick from
    private readonly string[] namePool = new[]
    {
        "Alice","Bob","Carol","Dave","Eve",
        "Frank","Grace","Heidi","Ivan","Judy",
        "Mallory","Niaj","Olivia","Peggy","Quinn",
        "Rupert","Sybil","Trent","Uma","Victor",
        "Wendy","Xander","Yvonne","Zack","Arthur",
        "Bella","Chloe","Dexter","Elena","Felix",
        "Gina","Hank","Isla","Jack","Kira",
        "Liam","Mia","Noah","Olga","Paula"
    };

    private int youCups;
    private const string YouKey = "youCups";

    void Start()
    {
        LoadOrGenerate();
        PopulatePlaceholders();
        PopulateYouBoard();
        StartCoroutine(PeriodicUpdate());
    }

    private void LoadOrGenerate()
    {
        // --- Main leaderboard load/generate ---
        bool loaded = false;
        if (PlayerPrefs.HasKey("leaderboard"))
        {
            var json = PlayerPrefs.GetString("leaderboard");
            var data = JsonUtility.FromJson<LeaderboardData>(json);
            if (data?.items != null && data.items.Length == lBoardItems.Length)
            {
                lBoardItems = data.items;
                loaded = true;
                Debug.Log("Loaded leaderboard from PlayerPrefs:");
                foreach (var it in lBoardItems)
                    Debug.Log($"  {it.index}: {it.name} – {it.cups} cups");
            }
        }

        if (!loaded)
        {
            Debug.Log("No saved leaderboard—generating fake data:");
            for (int i = 0; i < lBoardItems.Length; i++)
            {
                lBoardItems[i] = new LeaderboardItem
                {
                    index = i + 1,
                    name  = namePool[UnityEngine.Random.Range(0, namePool.Length)],
                    cups  = UnityEngine.Random.Range(0, 500)
                };
                var it = lBoardItems[i];
                Debug.Log($"  {it.index}: {it.name} – {it.cups} cups");
            }

            var outData = new LeaderboardData { items = lBoardItems };
            PlayerPrefs.SetString("leaderboard", JsonUtility.ToJson(outData));
            PlayerPrefs.Save();
        }

        // --- 'You' cups load/generate (initial) ---
        if (PlayerPrefs.HasKey(YouKey))
            youCups = PlayerPrefs.GetInt(YouKey);
        else
        {
            youCups = UnityEngine.Random.Range(0, 500);
            PlayerPrefs.SetInt(YouKey, youCups);
            PlayerPrefs.Save();
        }
    }

    private void PopulatePlaceholders()
    {
        // Sort descending by cups
        Array.Sort(lBoardItems, (a, b) => b.cups.CompareTo(a.cups));

        for (int i = 0; i < lBoardItems.Length && i < content.childCount; i++)
        {
            var slot = content.GetChild(i);
            var item = lBoardItems[i];
            item.index = i + 1;

            // Level/Text_Level
            var idxTf = slot.Find("Level/Text_Level");
            if (idxTf != null)
                idxTf.GetComponent<TextMeshProUGUI>().text = item.index.ToString();

            // Text_Name
            var nameTf = slot.Find("Text_Name");
            if (nameTf != null)
                nameTf.GetComponent<TextMeshProUGUI>().text = item.name;

            // Cups: Text_Name (1) or Icon_Trophy/Text_Name
            var cupsTf = slot.Find("Text_Name (1)") 
                      ?? slot.Find("Icon_Trophy/Text_Name");
            if (cupsTf != null)
                cupsTf.GetComponent<TextMeshProUGUI>().text = item.cups.ToString();
        }
    }

    private void PopulateYouBoard()
    {
        // Determine the cups count of the 50th place (index 49)
        int threshold;
        if (lBoardItems.Length >= 50)
            threshold = lBoardItems[49].cups;
        else
            threshold = lBoardItems[lBoardItems.Length - 1].cups;

        // Assign youCups less than that threshold => rank > 50
        youCups = UnityEngine.Random.Range(0, threshold);
        PlayerPrefs.SetInt(YouKey, youCups);
        PlayerPrefs.Save();
        Debug.Log($"You’s fake cups set to {youCups} (< {threshold}) to ensure rank >50");

        // Compute your rank
        int countHigher = 0;
        foreach (var it in lBoardItems)
            if (it.cups > youCups) countHigher++;
        int youRank = countHigher + 1;

        // Populate the youBoard placeholder
        var slot = youBoard.transform;

        var idxTf = slot.Find("Level/Text_Level");
        if (idxTf != null)
            idxTf.GetComponent<TextMeshProUGUI>().text = youRank.ToString();

        var nameTf = slot.Find("Text_Name");
        if (nameTf != null)
            nameTf.GetComponent<TextMeshProUGUI>().text = "You";

        var cupsTf = slot.Find("Text_Name (1)") 
                  ?? slot.Find("Icon_Trophy/Text_Name");
        if (cupsTf != null)
            cupsTf.GetComponent<TextMeshProUGUI>().text = youCups.ToString();
    }

    private IEnumerator PeriodicUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Tweak main leaderboard entries only
            int changes = UnityEngine.Random.Range(1, 4);
            Debug.Log($"Applying {changes} random leaderboard updates:");
            for (int j = 0; j < changes; j++)
            {
                int idx     = UnityEngine.Random.Range(0, lBoardItems.Length);
                int oldCups = lBoardItems[idx].cups;
                int delta   = UnityEngine.Random.Range(-20, 51);
                lBoardItems[idx].cups = Mathf.Max(oldCups + delta, 0);
                var it = lBoardItems[idx];
                Debug.Log($"  {it.index}: {it.name} cups {oldCups} → {it.cups}");
            }

            // Save updated main list
            var outData = new LeaderboardData { items = lBoardItems };
            PlayerPrefs.SetString("leaderboard", JsonUtility.ToJson(outData));
            PlayerPrefs.Save();

            // Refresh UI
            PopulatePlaceholders();
            PopulateYouBoard();
        }
    }
}
