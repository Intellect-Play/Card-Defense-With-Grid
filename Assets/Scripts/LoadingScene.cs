using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    [Header("UI References")]
    public Slider slider;                    // Min = 0, Max = 100 in the Inspector
    public TextMeshProUGUI sliderTxt;

    [Header("Settings")]
    [Tooltip("How long the fake load should take (in seconds)")]
    public float loadDuration = 3f;

    [Tooltip("Name of the scene to load when done")]
    public string nextSceneName = "Game";

    void Start()
    {
        StartCoroutine(FakeLoad());
    }

    private IEnumerator FakeLoad()
    {
        float elapsed = 0f;
        int currentPercent = 0;

        // initialize UI
        slider.value   = 0;
        sliderTxt.text = "0%";

        while (elapsed < loadDuration)
        {
            // pick a random wait interval so the jumps feel uneven
            float waitTime = Random.Range(0.1f, 0.5f);
            if (elapsed + waitTime > loadDuration)
                waitTime = loadDuration - elapsed;

            elapsed += waitTime;
            yield return new WaitForSeconds(waitTime);

            // compute the maximum "legit" percent for this elapsed time
            int maxPossible = Mathf.FloorToInt((elapsed / loadDuration) * 100f);

            // ensure we always jump forward by at least 5%
            int minNext = Mathf.Min(currentPercent + 5, maxPossible);

            // pick a random next percent between minNext and maxPossible
            int nextPercent = (maxPossible > minNext)
                ? Random.Range(minNext, maxPossible + 1)
                : maxPossible;

            currentPercent = nextPercent;
            slider.value   = currentPercent;
            sliderTxt.text = currentPercent + "%";
        }

        // final clamp to 100%
        slider.value   = 100;
        sliderTxt.text = "100%";

        // load the target scene
        SceneManager.LoadScene(nextSceneName);
    }
}
