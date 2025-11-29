using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GrammarBowlingGame : MonoBehaviour
{
    // ===========================
    // QUESTION DATA
    // ===========================
    [System.Serializable]
    public class HomophoneQuestion
    {
        public string question;
        public string[] options = new string[3];
        public int correctIndex;
    }

    public HomophoneQuestion[] questions;

    // ===========================
    // UI
    // ===========================
    public TMP_Text questionText;
    public TMP_Text[] laneOptionTexts; // index = lane index (0,1,2)

    // ===========================
    // GAME OBJECTS
    // ===========================
    public GameObject[] hitBalls;
    public GameObject[] missBalls;

    public Animator[] pinAnimators; // must support triggers: Idle, Fall
    public GameObject[] spotlights;

    public GameObject[] hearts;
    private int lives;

    private int score = 0;

    // ===========================
    // END SCREEN
    // ===========================
    public GameObject startUI;
    public GameObject endUI;
    public TMP_Text finalScoreText;

    public GameObject winObject;
    public GameObject loseObject;

    public GameObject[] starObjects; // size 3, stars awarded based on score
    public GameObject[] starObjects1; // size 3, stars awarded based on score

    // ===========================
    // AUDIO
    // ===========================
    public AudioSource sfxSource;
    public AudioClip sfxHit;
    public AudioClip sfxMiss;
    public AudioClip sfxGameOverWin;
    public AudioClip sfxGameOverLose;

    // ===========================
    // INTERNAL
    // ===========================
    private List<int> questionOrder = new List<int>();
    private int qIndex = -1;

    // laneIndex → which option index
    private int[] optionLaneMap = new int[3];
    private bool waitingForAnswer = false;


    // ===========================
    // AUTO-ADD QUESTIONS
    // ===========================
    private void Awake()
    {
        questions = new HomophoneQuestion[]
        {
            new HomophoneQuestion {
                question = "She wants to ___ a new car this year.",
                options = new string[] {"buy", "by", "bye"},
                correctIndex = 0
            },
            new HomophoneQuestion {
                question = "He hurt his ___ while running.",
                options = new string[] {"sole", "soul", "sol"},
                correctIndex = 0
            },
            new HomophoneQuestion {
                question = "Please ___ the door behind you.",
                options = new string[] {"close", "clothes", "cloze"},
                correctIndex = 0
            },
            new HomophoneQuestion {
                question = "The wind will ___ the leaves away.",
                options = new string[] {"blow", "blue", "blew"},
                correctIndex = 0
            },
            new HomophoneQuestion {
                question = "I will ___ you after the meeting.",
                options = new string[] {"meet", "meat", "mete"},
                correctIndex = 0
            }
        };
    }


    // ===========================
    // INIT
    // ===========================
    void Start()
    {
        endUI.SetActive(false);
        HideAllSpotlights();
        HideAllBalls();
    }


    // ===========================
    // START GAME
    // ===========================
    public void StartGame()
    {
        score = 0;
        lives = hearts.Length;

        foreach (var h in hearts)
            h.SetActive(true);

        startUI.SetActive(false);
        endUI.SetActive(false);

        ShuffleQuestions();
        qIndex = -1;

        NextQuestion();
    }

    void ShuffleQuestions()
    {
        questionOrder.Clear();
        for (int i = 0; i < questions.Length; i++)
            questionOrder.Add(i);

        // shuffle
        for (int i = 0; i < questionOrder.Count; i++)
        {
            int r = Random.Range(i, questionOrder.Count);
            (questionOrder[i], questionOrder[r]) = (questionOrder[r], questionOrder[i]);
        }
    }


    // ===========================
    // NEXT QUESTION (MAIN LOGIC)
    // ===========================
    void NextQuestion()
    {
        StartCoroutine(ResetPinsToIdle());
        HideAllBalls();
        HideAllSpotlights();

        qIndex++;

        if (qIndex >= questionOrder.Count)
        {
            ShowEndScreen(true);
            return;
        }

        var q = questions[questionOrder[qIndex]];
        questionText.text = q.question;

        // shuffle option placement
        int[] order = { 0, 1, 2 };
        for (int i = 0; i < 3; i++)
        {
            int r = Random.Range(i, 3);
            (order[i], order[r]) = (order[r], order[i]);
        }

        // apply mapping & UI
        for (int lane = 0; lane < 3; lane++)
        {
            int opt = order[lane];
            laneOptionTexts[lane].text = q.options[opt];
            optionLaneMap[lane] = opt;
        }

        waitingForAnswer = true;
    }


    // ===========================
    // USER INPUT
    // ===========================
    public void SelectLane(int laneIndex)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;

        int selectedOption = optionLaneMap[laneIndex];
        int correctOption = questions[questionOrder[qIndex]].correctIndex;

        if (selectedOption == correctOption)
        {
            hitBalls[laneIndex].SetActive(true);
            StartCoroutine(CorrectFlow(laneIndex));
        }
        else
        {
            missBalls[laneIndex].SetActive(true);
            StartCoroutine(WrongFlow(correctOption));
        }
    }


    // ===========================
    // CORRECT ANSWER FLOW
    // ===========================
    IEnumerator CorrectFlow(int lane)
    {
        yield return new WaitForSeconds(1f);

        // Play fall animation ONCE
        pinAnimators[lane].SetTrigger("Fall");
        sfxSource.PlayOneShot(sfxHit);

        yield return new WaitForSeconds(1.0f);

        score++;

        yield return new WaitForSeconds(3f);

        NextQuestion();
    }


    // ===========================
    // WRONG ANSWER FLOW
    // ===========================
    IEnumerator WrongFlow(int correctOpt)
    {
        yield return new WaitForSeconds(1f);

        sfxSource.PlayOneShot(sfxMiss);
        int correctLane = FindLaneWithOption(correctOpt);
        spotlights[correctLane].SetActive(true);

        lives--;
        if (lives >= 0) hearts[lives].SetActive(false);

        yield return new WaitForSeconds(3f);

        if (lives <= 0)
        {
            ShowEndScreen(false);
        }
        else
        {
            NextQuestion();
        }
    }

    int FindLaneWithOption(int opt)
    {
        for (int lane = 0; lane < 3; lane++)
            if (optionLaneMap[lane] == opt)
                return lane;
        return -1;
    }


    // ===========================
    // PIN RESET (Idle once per question)
    // ===========================
    IEnumerator ResetPinsToIdle()
    {
        yield return new WaitForSeconds(3f);
        for (int i = 0; i < pinAnimators.Length; i++)
        {
            pinAnimators[i].ResetTrigger("Fall");
            pinAnimators[i].SetTrigger("Idle");
            pinAnimators[i].Update(0f); // snap to pose immediately
        }
    }


    // ===========================
    // END SCREEN
    // ===========================
    void ShowEndScreen(bool wonAll)
    {
        endUI.SetActive(true);
        finalScoreText.text = score.ToString() + "/5";

        winObject.SetActive(wonAll);
        loseObject.SetActive(!wonAll);

        // Stars based on score (0–5 scale)
        foreach (var s in starObjects)
            s.SetActive(false);

        if (score == 0) { /* no stars */ }
        else if (score <= 2)
        {
            starObjects[0].SetActive(true);
            starObjects1[0].SetActive(true);
        }
        else if (score <= 4)
        {
            starObjects[0].SetActive(true);
            starObjects[1].SetActive(true);
            starObjects1[0].SetActive(true);
            starObjects1[1].SetActive(true);
        }
        else if (score == 5)
        {
            starObjects[0].SetActive(true);
            starObjects[1].SetActive(true);
            starObjects[2].SetActive(true);
            starObjects1[0].SetActive(true);
            starObjects1[1].SetActive(true);
            starObjects1[2].SetActive(true);
        }

        if (wonAll)
            sfxSource.PlayOneShot(sfxGameOverWin);
        else
            sfxSource.PlayOneShot(sfxGameOverLose);
    }


    // ===========================
    // UTIL
    // ===========================
    void HideAllBalls()
    {
        foreach (var b in hitBalls) b.SetActive(false);
        foreach (var b in missBalls) b.SetActive(false);
    }

    void HideAllSpotlights()
    {
        foreach (var s in spotlights) s.SetActive(false);
    }


    // ===========================
    // BUTTONS
    // ===========================
    public void GoHome()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Replay()
    {
        StartGame();
    }
}
