using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using Unity.VisualScripting;
using System.Threading;
using UnityEngine.SceneManagement;

public class ChickenCrossing : MonoBehaviour
{
    [Header("Player")]
    public Transform chicken;
    public int currentLane = -1;
    public GameObject SideMove;

    [Header("Bet System")]
    public int playerBalance = 1000;   // starting balance
    public int currentBet = 0;
    public Text balanceText;
    public int currentBalance;
    public Text currentBalanceText;
    public Text[] betText;
    public Button cashoutButton;
    public Button JumpButton;
    public Button[] Diffbuttons;
    public Text DiffText;
    private const string BalanceKey = "BalanceKey";
    public Text WinAmount;
    public GameObject Youlose;
    public GameObject YouWon;
    public GameObject InfoPanel;


    [Header("Lanes & Difficulty")]
    public Transform[] lanePositions;
    public Lane[] lanes;
    public enum Difficulty { Easy, Medium, Hard, Impossible }
    public Difficulty currentDifficulty = Difficulty.Easy;
    private float[] laneHitProbabilities;
    public Transform Lanepoint;

    [Header("Multiplier UI")]
    public Text multiplierText; // Global multiplier display
    private float[] multipliers;
    public Text AmountWonText;
    public int AmountWon;

    [Header("ScrollView Settings")]
    public ScrollRect scrollRect;
    public float scrollSpeed = 10f;
    private float targetVertical = 1f;

    [Header("Barrier Settings")]
    public GameObject barrierPrefab;
    private List<GameObject> activeBarriers = new List<GameObject>();

    private bool gameOver = false;

    [Header("Jump Settings")]
    public float jumpCooldown = 0.6f;
    public bool canJump = true;
    public ChickenAnim chickenAnim;

    void Start()
    {
        chickenAnim = FindObjectOfType<ChickenAnim>();
        SetupMultipliers();
        AssignLaneMultipliers();
        UpdateMultiplierUI();

        foreach (var lane in lanes)
            lane.difficulty = currentDifficulty;

        ActivateNearbyLanes();
        UpdateUI();
        GetBalance();
        balanceText.text = playerBalance.ToString();
    }

    void AssignLaneMultipliers()
    {
        for (int i = 0; i < lanes.Length; i++)
        {
            lanes[i].laneMultiplier = multipliers[i];
            if (lanes[i].multiplierText != null)
                lanes[i].multiplierText.text = "x" + multipliers[i].ToString("F2");
        }
    }

    void Update()
    {
        if (gameOver) return;

        SetupMultipliers();
        AssignLaneMultipliers();
        UpdateMultiplierUI();

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            Jump();
            StartCoroutine(JumpCooldown());
        }
        if (currentLane == 29)
        {
            YouWon.gameObject.SetActive(true);
            Cashout();
        }
    }

    public void SetDiff(string tag)
    {
        if (tag == "Easy")
        {
            currentDifficulty = Difficulty.Easy;
            DiffText.text = "Your difficulty : Easy";
            for (int i = 0; i < Diffbuttons.Length; i++)
            {
                Diffbuttons[i].interactable = true;
            }
            Diffbuttons[0].interactable = false;
        }
        if (tag == "Medium")
        {
            currentDifficulty = Difficulty.Medium;
            DiffText.text = "Your difficulty : Medium";
            for (int i = 0; i < Diffbuttons.Length; i++)
            {
                Diffbuttons[i].interactable = true;
            }
            Diffbuttons[1].interactable = false;
        }
        if (tag == "Hard")
        {
            currentDifficulty = Difficulty.Hard;
            DiffText.text = "Your difficulty : Hard";
            for (int i = 0; i < Diffbuttons.Length; i++)
            {
                Diffbuttons[i].interactable = true;
            }
            Diffbuttons[2].interactable = false;
        }
        if (tag == "Impossible")
        {
            currentDifficulty = Difficulty.Impossible;
            DiffText.text = "Your difficulty : Impossible";
            for (int i = 0; i < Diffbuttons.Length; i++)
            {
                Diffbuttons[i].interactable = true;
            }
            Diffbuttons[3].interactable = false;
        }
    }

    public void InfoPanelMeth()
    {
        if (InfoPanel.activeSelf)
        {
            InfoPanel.gameObject.SetActive(false);
        }
        else
        {
            InfoPanel.gameObject.SetActive(true);
        }
    }
    private IEnumerator JumpCooldown()
    {
        canJump = false;
        yield return new WaitForSecondsRealtime(jumpCooldown);
        canJump = true;
    }

    void SetupMultipliers()
    {
        multipliers = new float[lanePositions.Length];

        switch (currentDifficulty)
        {
            case Difficulty.Easy:
                for (int i = 0; i < lanePositions.Length; i++)
                    multipliers[i] = 1f + 0.15f * (i + 1);
                break;

            case Difficulty.Medium:
                for (int i = 0; i < lanePositions.Length; i++)
                    multipliers[i] = 1f + 0.30f * (i + 1);
                break;

            case Difficulty.Hard:
                for (int i = 0; i < lanePositions.Length; i++)
                    multipliers[i] = 1f + 0.50f * (i + 1);
                break;

            case Difficulty.Impossible:
                for (int i = 0; i < lanePositions.Length; i++)
                    multipliers[i] = 1f + 0.75f * (i + 1);
                break;
        }
    }


    public void GetBalance()
    {
        playerBalance = PlayerPrefs.GetInt(BalanceKey, playerBalance);
    }
    public void SetBalance()
    {
        PlayerPrefs.SetInt(BalanceKey, playerBalance);
        PlayerPrefs.Save();
    }
    bool isJumping;
    public void Jump()
    {
        if (isJumping) return;
        StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        currentLane++;
        chickenAnim.Jump();

        if (currentLane >= lanePositions.Length)
        {
            WinGame();
            Time.timeScale = 0;
        }

        if (currentLane <= 1)
        {
            Transform bottomPoint = lanePositions[currentLane].Find("BottomPoint");
            chicken.position = bottomPoint != null ? bottomPoint.position : lanePositions[currentLane].position;
        }


        if (barrierPrefab != null && currentLane < lanes.Length)
            StartCoroutine(SpawnBarrier(lanes[currentLane], 0.35f));

        lanes[currentLane].StopLane();

        for (int i = currentLane + 1; i <= Mathf.Min(lanes.Length - 1, currentLane + 3); i++)
            lanes[i].StartLane();

        if (currentLane > 1)
        {
            StartCoroutine(LerpScrollAndSideMove());
        }

        UpdateLaneStates();
        UpdateMultiplierUI();
        StartCoroutine(JumpCooldown());
        for (int i = 0; i < Diffbuttons.Length; i++)
        {
            Diffbuttons[i].interactable = false;
        }
        yield return new WaitForSeconds(0.25f);
        chickenAnim.Idle();

        isJumping = false;
    }

    IEnumerator LerpScrollAndSideMove()
    {
        Vector3 startScrollPos = scrollRect.content.localPosition;
        Vector3 targetScrollPos = startScrollPos + new Vector3(-295f, 0, 0);

        Vector3 startSidePos = SideMove.transform.localPosition;
        Vector3 targetSidePos = startSidePos + new Vector3(-295f, 0, 0);

        float duration = 0.4f; // how smooth/fast it moves
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            scrollRect.content.localPosition = Vector3.Lerp(startScrollPos, targetScrollPos, t);
            SideMove.transform.localPosition = Vector3.Lerp(startSidePos, targetSidePos, t);
            yield return null;
        }

        scrollRect.content.localPosition = targetScrollPos;
        SideMove.transform.localPosition = targetSidePos;
    }


    void UpdateLaneStates()
    {
        for (int i = 0; i < lanes.Length; i++)
        {
            if (i == currentLane)
            {
                if (lanes[i].multiplierText != null)
                {
                    lanes[i].multiplierText.gameObject.SetActive(false);

                    string text = lanes[i].multiplierText.text.Replace("x", "").Trim();
                    if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float multiplier))
                    {
                        int bonus = Mathf.RoundToInt(multiplier * 10f);
                        currentBet += bonus;
                        currentBalance += bonus;
                        currentBalanceText.text = currentBalance.ToString();
                        betText[0].text = "Cash out $" + currentBet.ToString() + " USD";
                        StartCoroutine(ShowBonusEffect(bonus, lanes[i].transform.position));
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Couldn't parse multiplier text: {lanes[i].multiplierText.text}");
                    }
                }
                lanes[i].FirstCircle.gameObject.SetActive(false);
            }
            else if (i < currentLane)
            {
                if (lanes[i].multiplierText != null)
                {
                    lanes[i].multiplierText.gameObject.SetActive(true);
                    lanes[i].multiplierText.color = Color.white;
                    lanes[i].FirstCircle.gameObject.SetActive(false);
                    lanes[i].SecondsCircle.gameObject.SetActive(true);

                }
            }
            else
            {
                if (lanes[i].multiplierText != null)
                {
                    lanes[i].multiplierText.gameObject.SetActive(true);
                    lanes[i].multiplierText.color = Color.yellow;
                    lanes[i].FirstCircle.gameObject.SetActive(true);
                    lanes[i].SecondsCircle.gameObject.SetActive(false);
                }
            }
        }
    }

    IEnumerator ShowBonusEffect(int bonus, Vector3 pos)
    {
        AmountWonText.text = $"+{bonus}";
        AmountWonText.color = Color.yellow;

        Color startColor = AmountWonText.color;
        startColor.a = 1f;
        AmountWonText.color = startColor;

        RectTransform rect = AmountWonText.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = startPos + Vector2.up * 50f;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            AmountWonText.color = new Color(1f, 1f, 0f, 1f - (elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = startPos;
        AmountWonText.text = "";
    }
    public int FetchBetamount;
    public void PlaceBet(int amount)
    {
        if (amount <= 0) return;
        if (amount > playerBalance)
        {
            Debug.Log("Not enough balance!");
            return;
        }
        FetchBetamount = amount;
        currentBalance = amount;
        currentBet = amount;
        playerBalance -= amount;
        UpdateUI();
    }

    public void WinBet(int multiplier)
    {
        int winnings = currentBet * multiplier;
        playerBalance += winnings;
        AmountWon += winnings;
        currentBet = 0;
        UpdateUI();
        StartCoroutine(ShowBonusEffect(winnings, chicken.position));
    }

    public void LoseBet()
    {
        Debug.Log("You lost your bet!");
        betText[1].text = FetchBetamount.ToString();
        playerBalance -= FetchBetamount;
        balanceText.text = playerBalance.ToString();
        SetBalance();
        UpdateUI();
    }

    void UpdateUI()
    {
        if (balanceText != null)
            balanceText.text = $"Balance: {playerBalance}";

        if (betText != null)
        {
            betText[0].text = currentBet > 0 ? $"Bet: {currentBet}" : "No Bet";
        }


        if (cashoutButton != null)
        {
            cashoutButton.interactable = currentBet > 0;
            JumpButton.interactable = currentBet > 0;
        }

    }


    void ActivateNearbyLanes()
    {
        int start = Mathf.Max(0, currentLane);
        int end = Mathf.Min(lanes.Length - 1, currentLane + 3);

        for (int i = 0; i < lanes.Length; i++)
        {
            if (i >= start && i <= end)
                lanes[i].StartLane();
            else
                lanes[i].StopLane();
        }
    }

    IEnumerator SpawnBarrier(Lane lane, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        Vector3 barrierPos = lane.transform.position;
        barrierPos.y += 1.5f;

        GameObject barrier = Instantiate(barrierPrefab, barrierPos, Quaternion.identity, lane.transform);
        activeBarriers.Add(barrier);
    }

    public void Cashout()
    {
        if (currentBet <= 0) return;
         SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        YouWon.gameObject.SetActive(true);
        WinAmount.text = currentBet.ToString();
        playerBalance += currentBet;
        Debug.Log($"Cashed out: {currentBet}");
        currentBet = 0;
        AmountWon = 0;
        SetBalance();
        balanceText.text = playerBalance.ToString();
        UpdateUI();
        StartCoroutine(WaitToReload());
    }
    IEnumerator WaitToReload()
    {
        yield return new WaitForSecondsRealtime(3f);
        SceneManager.LoadScene("Gameplay");
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Car"))
        {
            JumpButton.interactable = false;
            chickenAnim.Died();
            StartCoroutine(Die());
        }

    }

    IEnumerator Die()
    {
        yield return new WaitForSecondsRealtime(4f);
        Youlose.gameObject.SetActive(true);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        gameOver = true;
        multiplierText.text = "You died! x" + multipliers[Mathf.Clamp(currentLane, 0, multipliers.Length - 1)].ToString("F2");
        foreach (var lane in lanes)
            lane.StopLane();
        Debug.Log("Game Over! Lane reached: " + currentLane);
        LoseBet();
        yield return new WaitForSecondsRealtime(2f);
        SceneManager.LoadScene("Gameplay");
    }

    void WinGame()
    {
        gameOver = true;
        playerBalance += currentBet;
        SetBalance();
        balanceText.text = playerBalance.ToString();
        multiplierText.text = "You survived! x" + multipliers[lanePositions.Length - 1].ToString("F2");
        foreach (var lane in lanes)
            lane.StopLane();
        Debug.Log("You survived all lanes!");

    }

    void UpdateMultiplierUI()
    {
        int lane = Mathf.Clamp(currentLane, 0, multipliers.Length - 1);
        multiplierText.text = "x" + multipliers[lane].ToString("F2");

        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i].multiplierText != null)
                lanes[i].multiplierText.text = "x" + multipliers[i].ToString("F2");
        }
    }

}
