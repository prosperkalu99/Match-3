using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject backgroundPanel;
    public GameObject vicroryPanel;
    public GameObject losePanel;

    public int goal; // the amount of point you need to get to win
    public int moves; // the number if turns you can take
    private int points; // the current point you have earned

    public bool isGameEnded;

    public TMP_Text pointsTxt;
    public TMP_Text movessTxt;
    public TMP_Text goalTxt;
    public TMP_Text levelTxt;

    public TMP_Text congratsTxt;
    public TMP_Text unfortunateTxt;

    public Button successButton;
    private TextMeshProUGUI successButtonText;

    public AudioClip winSound;
    public AudioClip matchSound;
    public AudioClip loseSound;

    public AudioSource playerAudio;
    private AudioSource gameMusicAudio;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetupNewLevel();
    }

    private void SetupNewLevel()
    {
        successButtonText = successButton.GetComponentInChildren<TextMeshProUGUI>();
        playerAudio = GetComponent<AudioSource>();
        gameMusicAudio = GameObject.Find("Main Camera").GetComponent<AudioSource>();
        gameMusicAudio.Play();
    }

    public void Initialize(int _moves, int _goal)
    {
        moves = _moves;
        goal = _goal;
    }

    // Update is called once per frame
    void Update()
    {
        pointsTxt.text = "Points: " + points.ToString();
        movessTxt.text = "Moves Left: " + moves.ToString();
        goalTxt.text = "Goal: " + goal.ToString();
        levelTxt.text = $"Level {SceneManager.GetActiveScene().buildIndex}";
    }

    public void ProcessTurn(int _pointsToGain, bool _subtractMoves)
    {
        points += _pointsToGain;

        playerAudio.PlayOneShot(matchSound, 0.2f);

        if (_subtractMoves)
        {
            moves--;
        }
        if (points >= goal)
        {
            // You've won the game
            gameMusicAudio.Stop();
            playerAudio.PlayOneShot(winSound, 1.0f);
            isGameEnded = true;
            // Display a victory screen
            backgroundPanel.SetActive(true);
            vicroryPanel.SetActive(true);
            congratsTxt.text = $"Congratulations you won. You scored {points} points";
            PotionBoard.instance.potionParent.SetActive(false);

            if (SceneManager.GetActiveScene().buildIndex == 3)
            {
                successButtonText.text = "Back to Menu";
            }
            else
            {
                successButtonText.text = "Next Level";
            }
            return;
        }

        if (moves == 0)
        {
            // You've lost the game
            gameMusicAudio.Stop();
            playerAudio.PlayOneShot(loseSound, 1.0f);
            isGameEnded = true;
            backgroundPanel.SetActive(true);
            losePanel.SetActive(true);
            unfortunateTxt.text = $"Unfortunately you only got  {points} points \n\n Better luck next time";
            PotionBoard.instance.potionParent.SetActive(false);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        SetupNewLevel();
    }

    // attached to a button to change scene when winning
    public void WinGame()
    {
        int currentLevel = SceneManager.GetActiveScene().buildIndex;
        if (currentLevel == 3)
        {
            SceneManager.LoadScene(0);
        }
        else SceneManager.LoadScene(currentLevel + 1);
    }

    // attached to a button to change scene when losing
    public void LoseGame()
    {
        SceneManager.LoadScene(0);
    }

}
