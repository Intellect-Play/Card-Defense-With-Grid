using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene1 : MonoBehaviour
{
    private void Awake()
    {
        if(PlayerPrefs.GetInt("TutorialFinish",0) == 0)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            SceneManager.LoadScene(2);

        }
    }
}
