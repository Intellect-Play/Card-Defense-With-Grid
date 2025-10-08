using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("References")]
    public LevelManager        levelManager;
    public UIManager           uIManager;
    public RoguelikeManager    roguelikeManager;
    public TetrisWeaponManager tetrisWeaponManager;

    public CardDeckAnimator[]  cardDeckAnimators;   // 0=Inventor, 1=Wizard, 2=Samurai
    public Transform           enemyParent;

    [Header("Spawn Settings")]
    public float enemySpawnY    = 3.5f;
    public float enemySpawnXMin = -1.5f;
    public float enemySpawnXMax = 1.5f;
    public float enemySpawnYMin = 3;
    public float enemySpawnYMax = 3.5f;
    public float checkInterval = 0.1f;
    public float checkIntervalGroup = 0.1f;
    public float IntervalWave = 0.1f;
    public float bulletInterval = 3f;

    [Header("Fortress Health")]
    public int health = 100;

    [Header("Wave Flow")]
    [Tooltip("World-space Y threshold; when a wave’s front-most enemy crosses below this, spawn the next wave.")]
    public float midpointY = 0f;

    [Header("Z Ordering")]
    [Tooltip("Starting Z for the very first spawned enemy (e.g. 0.84).")]
    public float spawnZStart = 0.84f;
    [Tooltip("Each newer enemy gets +this much Z so it draws in front (use 0.01 as requested).")]
    public float spawnZStep  = 0.01f;

    // --- Runtime state ---
    private Camera mainCam;

    private const string PrefNextLevelIndex   = "NextLevelIndex";
    private const string PrefArenaIndex       = "Arena_Index";
    private const string PrefMatchIndex       = "Arena_Match_Index";
    private const string PrefBattlesCompleted = "battlesCompleted";
    private const string PrefBattlesTotal     = "battlesTotal";

    private int _currentLevelIndex = 0;

    // Pause helpers
    private int  _pauseDepth  = 0;
    private bool _loseStarted = false;

    // Per-wave bookkeeping
    private Transform[] _waveRoots;
    private int[]       _waveCountsDeclared;
    private int[]       _cumuCounts;
    private bool[]      _waveSpawnFinished;
    private bool[]      _waveSpawned;
    private bool[]      _roguelikeShown;

    // Kills (global)
    private int _totalEnemies;
    private int _totalKills;

    // Global subscription guard
    private bool _subscribed = false;

    // Global spawn index to advance Z every enemy across the whole level
    private int _spawnSequenceIndex = 0;

    private static readonly string[] DeckActiveKeys = {
        "Deck_Inventor_Active", "Deck_Wizard_Active", "Deck_Samurai_Active"
    };
    private static readonly string[] HeroKeys = {
        "Hero_Inventor", "Hero_Wizard", "Hero_Samurai"
    };

    // =================== Unity ===================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        _currentLevelIndex = ResolveCurrentLevelIndex();
        Debug.Log(PlayerPrefs.GetInt(PrefMatchIndex) + $"[GameManager] Current level index is {_currentLevelIndex}.");
    }

    void Start()
    {
        mainCam = Camera.main;

        if (mainCam)
        {
            mainCam.transparencySortMode = TransparencySortMode.CustomAxis;
            mainCam.transparencySortAxis = new Vector3(0f, 0f, 1f);
        }

        uIManager.SetCoins(PlayerPrefs.GetInt("gold", 0));
        uIManager.SetHealthText(health.ToString());
        uIManager.SetWinBoardActive(false);
        uIManager.SetLoseBoardActive(false);

        DeactivateAllDecks();
        InitialHeroSelectionAndThenStart();
        //StartCoroutine(InitialHeroSelectionAndThenStart());
    }

    void Update()
    {
        if (health <= 0 && !_loseStarted)
        {
            _loseStarted = true;
            health = 0;
            uIManager.SetHealthText("0");
            StartCoroutine(LoseSequence());
        }
    }

    void OnDestroy()
    {
        if (_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed -= HandleEnemyDestroyed;
            _subscribed = false;
        }
    }

    // =================== Level index resolve ===================

    /// <summary>
    /// Priority:
    /// 1) Use NextLevelIndex if Lobby set it.
    /// 2) Otherwise use current match index.
    /// </summary>
    private int ResolveCurrentLevelIndex()
    {
        int max = (levelManager != null && levelManager.levels != null) ? levelManager.levels.Length : 0;
        if (max <= 0) return 0;

        if (PlayerPrefs.HasKey(PrefNextLevelIndex))
        {
            int idx = PlayerPrefs.GetInt(PrefNextLevelIndex, 0);
            return Mathf.Clamp(idx, 0, max - 1);
        }

        int matchIdx = PlayerPrefs.GetInt(PrefMatchIndex, 0);
        return Mathf.Clamp(matchIdx, 0, max - 1);
    }

    // =================== Deck helpers ===================

    private void DeactivateAllDecks()
    {
        if (cardDeckAnimators == null) return;

        foreach (var cda in cardDeckAnimators)
        {
            if (!cda) continue;
            cda.PauseFiring();
            var parent = cda.transform.parent ? cda.transform.parent.gameObject : null;
            if (cda.gameObject.activeSelf) cda.gameObject.SetActive(false);
            if (parent && parent.activeSelf) parent.SetActive(false);
        }
    }

    private void ActivateDecksFromPrefs()
    {
        if (cardDeckAnimators == null) return;

        string[] keys = DeckActiveKeys;

        for (int i = 0; i < cardDeckAnimators.Length && i < keys.Length; i++)
        {
            var cda = cardDeckAnimators[i];
            if (!cda) continue;

            bool wantActive = PlayerPrefs.GetInt(keys[i], 0) == 1;
            var parent = cda.transform.parent ? cda.transform.parent.gameObject : null;

            if (wantActive)
            {
                if (parent && !parent.activeSelf) parent.SetActive(true);
                if (!cda.gameObject.activeSelf)  cda.gameObject.SetActive(true);
                cda.ResumeFiring();
            }
            else
            {
                cda.PauseFiring();
                if (cda.gameObject.activeSelf) cda.gameObject.SetActive(false);
                if (parent && parent.activeSelf) parent.SetActive(false);
            }
        }
    }

    public void ActivateDeckAnimatorParent(int index)
    {
        if (cardDeckAnimators == null || index < 0 || index >= cardDeckAnimators.Length) return;
        var animator = cardDeckAnimators[index];
        if (!animator) return;

        var parent = animator.transform.parent ? animator.transform.parent.gameObject : null;
        if (parent && !parent.activeSelf) parent.SetActive(true);
        if (!animator.gameObject.activeSelf) animator.gameObject.SetActive(true);

        animator.ResumeFiring();
    }

    // =================== Pause helpers ===================

    private void PauseGameForRoguelike()
    {
        _pauseDepth++;
        if (_pauseDepth == 1)
        {
            if (cardDeckAnimators != null)
                foreach (var cda in cardDeckAnimators)
                    if (cda) cda.PauseFiring();

            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
    }

    private void ResumeGameAfterRoguelike()
    {
        _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
        if (_pauseDepth == 0)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (cardDeckAnimators != null)
                foreach (var cda in cardDeckAnimators)
                    if (cda && cda.gameObject.activeInHierarchy) cda.ResumeFiring();
        }
    }

    // =================== Intro / Lose ===================

    private void InitialHeroSelectionAndThenStart()
    {
   
      

       
        uIManager.DisableSlotsWithoutImage();

        uIManager.SetRougelikeBoardActive(false);
        //PauseGameForRoguelike();



        //ResumeGameAfterRoguelike();
        ActivateDecksFromPrefs();

        StartCoroutine(RunLevel());
    }

    private IEnumerator LoseSequence()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        uIManager.SetLoseBoardActive(true);
        enabled = false;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health < 0) health = 0;
        uIManager.SetHealthText(health.ToString());
    }

    // =================== Main Level Flow ===================

    private IEnumerator RunLevel()
    {
        var level      = levelManager.levels[_currentLevelIndex];
        var waves      = level.waves;
        int totalWaves = waves.Length;

        _waveRoots          = new Transform[totalWaves];
        _waveCountsDeclared = new int[totalWaves];
        _cumuCounts         = new int[totalWaves];
        _waveSpawnFinished  = new bool[totalWaves];
        _waveSpawned        = new bool[totalWaves];
        _roguelikeShown     = new bool[totalWaves];

        _totalEnemies = 0;
        _totalKills   = 0;

        for (int i = 0; i < totalWaves; i++)
        {
            int c = 0;
            foreach (var ec in waves[i].enemies) c += ec.count;
            _waveCountsDeclared[i] = c;
            _totalEnemies += c;

            _cumuCounts[i] = (i == 0) ? c : _cumuCounts[i - 1] + c;

            var root = new GameObject($"Wave {i + 1}");
            root.transform.SetParent(enemyParent, false);
            _waveRoots[i] = root.transform;
            Debug.Log($"[GameManager] Wave {i + 1} has {c} enemies (cumulative {_cumuCounts[i]}).");
        }

        uIManager.SetLevelText($"Lvl {_currentLevelIndex + 1}");
        uIManager.SetWaveText($"Wave {Mathf.Min(1, totalWaves)}/{totalWaves}");
        uIManager.SetSliderValue(0f);

        if (!_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed += HandleEnemyDestroyed;
            _subscribed = true;
        }

        yield return SpawnWave(0, _waveCountsDeclared[0], waves[0], _waveRoots[0]);
        _waveSpawnFinished[0] = true;
        _waveSpawned[0]       = true;
        Debug.Log("[GameManager] First wave spawned.");
        StartCoroutine(MidpointSpawnCoordinator(waves));
        StartCoroutine(RoguelikeCoordinator(waves));

        // Wait for victory
        yield return new WaitUntil(() => _totalKills >= _totalEnemies);

        // Record progression immediately upon win
        AdvanceProgressOnWin();
        BumpNextLevelIndexAfterWin();

        // Win board
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        uIManager.SetWinBoardActive(true);
        Debug.Log("[GameManager] Player has won the level.");
        AwardGold(30);
    }

    private IEnumerator MidpointSpawnCoordinator(Wave[] waves)
    {
        int totalWaves = waves.Length;

        for (int wi = 0; wi < totalWaves - 1; wi++)
        {
            yield return new WaitForSecondsRealtime(0f); // safety
            yield return new WaitUntil(() => _waveSpawnFinished[wi]);
            //yield return new WaitUntil(() => FrontMostBelowMidpoint(wi) || WaveIsFullyDeadByCumulative(wi));
            TetrisWeaponManager.instance.StartTetrisWawe();
            int next = wi + 1;
            if (!_waveSpawned[next])
            {
                _waveSpawned[next] = true;
                yield return SpawnWave(next, _waveCountsDeclared[next], waves[next], _waveRoots[next]);
                _waveSpawnFinished[next] = true;
            }
        }
    }

    private bool WaveIsFullyDeadByCumulative(int waveIndex) => _totalKills >= _cumuCounts[waveIndex];

    // Look for EnemyBehaviour anywhere under the wave's enemy root
    private bool FrontMostBelowMidpoint(int waveIndex)
    {
        var root = _waveRoots[waveIndex];
        if (root == null) return true;

        bool anyAlive = false;
        float minY = float.MaxValue;

        foreach (Transform enemyRoot in root)
        {
            if (!enemyRoot || !enemyRoot.gameObject.activeInHierarchy) continue;

            var eb = enemyRoot.GetComponentInChildren<EnemyBehaviour>(true);
            if (!eb || !eb.gameObject.activeInHierarchy) continue;

            anyAlive = true;
            float y = eb.transform.position.y;
            if (y < minY) minY = y;
        }

        if (!anyAlive) return true;
        return (minY <= midpointY);
    }

    // =================== Enemy death / UI progress ===================

    private void HandleEnemyDestroyed(EnemyBehaviour eb)
    {
        if (eb == null) return;

        _totalKills++;
        AwardGold(eb.rewardGold);
        UpdateWaveUIFromTotals();
    }

    private void UpdateWaveUIFromTotals()
    {
        int totalWaves = _waveCountsDeclared.Length;

        int completedWaves = 0;
        while (completedWaves < totalWaves && _totalKills >= _cumuCounts[completedWaves])
            completedWaves++;

        int labelWave = Mathf.Clamp(completedWaves + 1, 1, totalWaves);
        uIManager.SetWaveText($"Wave {labelWave}/{totalWaves}");

        if (completedWaves >= totalWaves)
        {
            uIManager.SetSliderValue(1f);
            return;
        }

        int prevThreshold   = (completedWaves == 0) ? 0 : _cumuCounts[completedWaves - 1];
        int killsIntoWave   = Mathf.Max(0, _totalKills - prevThreshold);
        int neededThisWave  = Mathf.Max(1, _waveCountsDeclared[completedWaves]);
        float progress      = Mathf.Clamp01(killsIntoWave / (float)neededThisWave);

        uIManager.SetSliderValue(progress);
    }

    // =================== Roguelikes after each wave ===================

    private IEnumerator RoguelikeCoordinator(Wave[] waves)
    {
        int totalWaves = waves.Length;

        for (int wi = 0; wi < totalWaves - 1; wi++) // none after last
        {
            yield return new WaitUntil(() => _totalKills >= _cumuCounts[wi]);

            if (_roguelikeShown[wi]) continue;
            _roguelikeShown[wi] = true;
            if (waves[wi].RoguelikeBool)
            {
                Debug.Log($"[GameManager] Starting roguelike after wave {wi + 1}.");
                PauseGameForRoguelike();
                yield return roguelikeManager.RunRoguelike(wi + 1, waves[wi].roguelikeOptions);
                if (uIManager.roguelikeBoard.activeSelf)
                    yield return new WaitUntil(() => !uIManager.roguelikeBoard.activeSelf);
                ResumeGameAfterRoguelike();
                ActivateDecksFromPrefs();
                TetrisWeaponManager.instance.StartTetrisWawe();
            }
            else
            {
                TetrisWeaponManager.instance.StartTetrisWawe();
            }


        }
    }

    // =================== Spawning ===================

    private IEnumerator SpawnWave(int wi, int enemiesThis, Wave wave, Transform waveRoot)
    {
        var queue = new List<EnemySO>(enemiesThis);
        bool added = true; int li = 0;
        while (queue.Count < enemiesThis && added)
        {
            added = false;
            foreach (var ec in wave.enemies)
                if (li < ec.count) { queue.Add(ec.enemy); added = true; }
            li++;
        }

        var xs = GenerateShuffledXPositions(enemySpawnXMin, enemySpawnXMax, enemiesThis);
        var ys = GenerateShuffledXPositions(enemySpawnYMin, enemySpawnYMax, enemiesThis);

        for (int i = 0; i < queue.Count; i++)
        {
            //Debug.Log($"[SpawnWave] Spawning enemy {i + 1}/{queue.Count} of wave {wi + 1}.");
            var eso = queue[i];
            if (eso == null || eso.prefab == null)
            {
                Debug.LogError($"[SpawnWave] EnemySO or prefab missing at index {i} in wave {wi + 1}.");
                continue;
            }

            Vector3 pos = new Vector3(xs[i], ys[i], 0f);
            while (!IsSpawnPositionClear(pos.x))
                yield return new WaitForSeconds(checkIntervalGroup);
            //while (!IsSpawnPositionClear(pos.x))
            //    yield return new WaitForSeconds(checkInterval);

            float z = spawnZStart + (_spawnSequenceIndex * spawnZStep);
            _spawnSequenceIndex++;
            pos.z = z;

            var go = Instantiate(eso.prefab, pos, Quaternion.identity, waveRoot);

            var eb = go.GetComponentInChildren<EnemyBehaviour>(true);
            if (eb == null)
            {
                Debug.LogError($"[SpawnWave] EnemyBehaviour not found on prefab '{eso.prefab.name}'.");
                continue;
            }

            eb.health           = eso.health;
            eb.attack           = eso.attack;
            eb.moveSpeed        = eso.moveSpeed;
            eb.rewardGold       = eso.rewardedGold;
            eb.onFortressDamage = TakeDamage;
            yield return new WaitForSeconds(checkInterval);
        }
    }
    private void BumpNextLevelIndexAfterWin()
    {
        int max = (levelManager != null && levelManager.levels != null) ? levelManager.levels.Length : 0;
        if (max <= 0) return;

        int next = PlayerPrefs.GetInt(PrefNextLevelIndex, 0) + 1;
        //if (next >= max) next = 0; // istəsən clamp də edə bilərsən

        PlayerPrefs.SetInt(PrefNextLevelIndex, next);
        PlayerPrefs.Save();
    }
    // =================== Util ===================

    private void AwardGold(int amount)
    {
        int g = PlayerPrefs.GetInt("gold", 0) + amount;
        PlayerPrefs.SetInt("gold", g);
        PlayerPrefs.Save();
        uIManager.SetCoins(g);
    }

    private List<float> GenerateShuffledXPositions(float min, float max, int count)
    {
        var pos = new List<float>();
        if (count <= 0) return pos;
        if (count == 1) { pos.Add((min + max) / 2f); return pos; }

        float step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++) pos.Add(min + i * step);

        for (int i = 0; i < pos.Count; i++)
        {
            int j = Random.Range(i, pos.Count);
            float tmp = pos[i]; pos[i] = pos[j]; pos[j] = tmp;
        }
        return pos;
    }

    private bool IsSpawnPositionClear(float x)
    {
        foreach (Transform w in enemyParent)
        {
            foreach (Transform e in w)
            {
                if (!e) continue;
                if (Mathf.Abs(e.position.x - x) < 0.5f &&
                    Mathf.Abs(e.position.y - enemySpawnY) < 0.5f)
                    return false;
            }
        }
        return true;
    }

    // =================== Public: Next Level (HARD RESET) =====================

    public void NextLevel()
    {
        if (_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed -= HandleEnemyDestroyed;
            _subscribed = false;
        }

        int next = _currentLevelIndex + 1;
        //if (levelManager != null && levelManager.levels != null && levelManager.levels.Length > 0)
        //{
        //    if (next >= levelManager.levels.Length) next = 0;
        //}
        //else
        //{
        //    next = 0;
        //}

        PlayerPrefs.SetInt(PrefNextLevelIndex, next);

        ResetRunPrefs();
        ResetCardSessionLevelsToOne();
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetRunPrefs()
    {
        foreach (var k in DeckActiveKeys) PlayerPrefs.SetInt(k, 0);
        foreach (var k in HeroKeys)       PlayerPrefs.SetInt(k, 0);
    }

    private void ResetCardSessionLevelsToOne()
    {
        if (roguelikeManager != null)
        {
            void ResetArray(CardSO[] arr)
            {
                if (arr == null) return;
                foreach (var c in arr) { if (c) c.sessionLevel = 1; }
            }
            ResetArray(roguelikeManager.inventorCards);
            ResetArray(roguelikeManager.wizardCards);
            ResetArray(roguelikeManager.samuraiCards);
        }
    }

    // =================== Progress Advance on Win ===================

    private void AdvanceProgressOnWin()
    {
        int arena  = PlayerPrefs.GetInt(PrefArenaIndex, 0);
        int match  = PlayerPrefs.GetInt(PrefMatchIndex, 0);
        int total  = Mathf.Max(1, PlayerPrefs.GetInt(PrefBattlesTotal, 10));

        match++;
        if (match >= total)
        {
            match = 0;
            arena = Mathf.Max(0, arena + 1);
        }

        PlayerPrefs.SetInt(PrefArenaIndex, arena);
        PlayerPrefs.SetInt(PrefMatchIndex, match);
        PlayerPrefs.SetInt(PrefBattlesCompleted, PlayerPrefs.GetInt(PrefBattlesCompleted, 0) + 1);

        PlayerPrefs.Save();
    }

    public void HealFortress(int amount)
    {
        if (amount <= 0) return;
        health = Mathf.Max(0, health + amount);
        if (uIManager != null) uIManager.SetHealthText(health.ToString());
    }

}
