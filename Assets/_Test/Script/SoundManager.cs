using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public void PlayCard() => PlaySound(SoundType.Card);
    public void PlayFire() => PlaySound(SoundType.Fire);
    public void PlayUpgrade() => PlaySound(SoundType.Upgrade);
    public void PlayNewCard() => PlaySound(SoundType.NewCard);
    public void PlayHeal() => PlaySound(SoundType.Heal);
    public void PlayWin() => PlaySound(SoundType.Win);
    public void PlayReward() => PlaySound(SoundType.Reward);
    public void PlayClick() => PlaySound(SoundType.Click);
    public void PlayStartBattle() => PlaySound(SoundType.StartBattle);
    public void PlayEnemyKill() => PlaySound(SoundType.EnemyKill);
    public void PlayEnemyDamage() => PlaySound(SoundType.EnemyDamage);


    [System.Serializable]
    public class SoundData
    {
        public string name;
        public SoundType type;
        public AudioClip clip;
    }

    [Header("Audio")]
    public AudioSource sfxSource;
    public List<SoundData> sounds;

    private Dictionary<SoundType, AudioClip> soundDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildDictionary()
    {
        soundDict = new Dictionary<SoundType, AudioClip>();
        foreach (var s in sounds)
        {
            if (!soundDict.ContainsKey(s.type))
                soundDict.Add(s.type, s.clip);
        }
    }
    public void Play()
    {

    }
    public void PlaySound(SoundType type)
    {
        if (soundDict.TryGetValue(type, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + type);
        }
    }
}

public enum SoundType
{
    Card,
    Fire,
    Shoot,
    Heal,
    Win,
    Reward,
    Click,
    Upgrade,
    NewCard,
    StartBattle,
    EnemyKill,
    EnemyDamage
    // Əlavə səs tipləri...
}
