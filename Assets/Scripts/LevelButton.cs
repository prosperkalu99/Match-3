using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public int level;

    public void SetLevel()
    {
        SceneManager.LoadScene(level);
    }
}
