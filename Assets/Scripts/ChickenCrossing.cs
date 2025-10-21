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

    [Header("Bet System")]
    public int playerBalance = 1000;   // starting balance
    public int currentBet = 0;
    public Text balanceText;
    public Text betText;
    public Button cashoutButton;
    public Button JumpButton;
    public Button[] Diffbuttons;
    public Text DiffText;
    private const string BalanceKey = "BalanceKey";

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

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            Jump();
            StartCoroutine(JumpCooldown());
        }
    }

    public void SetDiff(string tag)
    {
        if (tag == "Easy")
        {
            currentDifficulty = Difficulty.Easy;
            DiffText.text = "You difficulty : Easy";
        }
        if (tag == "Medium")
        {
            currentDifficulty = Difficulty.Medium;
            DiffText.text = "You difficulty : Medium";
        }
        if (tag == "Hard")
        {
            currentDifficulty = Difficulty.Hard;
            DiffText.text = "You difficulty : Hard";
        }
        if (tag == "Impossible")
        {
            currentDifficulty = Difficulty.Impossible;
            DiffText.text = "You difficulty : Impossible";
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
        for (int i = 0; i < lanePositions.Length; i++)
            multipliers[i] = 1f + 0.15f * (i + 1);
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
    public void Jump()
    {
        currentLane++;

        if (currentLane >= lanePositions.Length)
        {
            WinGame();
            return;
        }
        if (currentLane <= 1)
        {
             Transform bottomPoint = lanePositions[currentLane].Find("BottomPoint");
             chicken.position = bottomPoint != null ? bottomPoint.position : lanePositions[currentLane].position;
        }
       

        if (barrierPrefab != null && currentLane < lanes.Length)
            StartCoroutine(SpawnBarrier(lanes[currentLane], 0.5f));

        lanes[currentLane].StopLane();

        for (int i = currentLane + 1; i <= Mathf.Min(lanes.Length - 1, currentLane + 3); i++)
            lanes[i].StartLane();
        if (currentLane > 1)
        {
            Vector3 pos = scrollRect.content.localPosition;
            pos.y += 311.2f;
            scrollRect.content.localPosition = pos;
        }

        UpdateLaneStates();
        UpdateMultiplierUI();
        StartCoroutine(JumpCooldown());
        for (int i = 0; i < Diffbuttons.Length; i++)
        {
            Diffbuttons[i].interactable = false;
        }
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
                        betText.text = "Cash out $" + currentBet.ToString() + " USD";
                        StartCoroutine(ShowBonusEffect(bonus, lanes[i].transform.position));
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Couldn't parse multiplier text: {lanes[i].multiplierText.text}");
                    }
                }
            }
            else if (i < currentLane)
            {
                if (lanes[i].multiplierText != null)
                {
                    lanes[i].multiplierText.gameObject.SetActive(true);
                    lanes[i].multiplierText.color = Color.white;
                }
            }
            else
            {
                if (lanes[i].multiplierText != null)
                {
                    lanes[i].multiplierText.gameObject.SetActive(true);
                    lanes[i].multiplierText.color = Color.yellow;
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

    public void PlaceBet(int amount)
    {
        if (amount <= 0) return;
        if (amount > playerBalance)
        {
            Debug.Log("Not enough balance!");
            return;
        }

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
        currentBet = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (balanceText != null)
            balanceText.text = $"Balance: {playerBalance}";

        if (betText != null)
            betText.text = currentBet > 0 ? $"Bet: {currentBet}" : "No Bet";

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
        GameObject barrier = Instantiate(barrierPrefab, barrierPos, Quaternion.identity, lane.transform);
        activeBarriers.Add(barrier);
    }
    public void Cashout()
    {
        if (currentBet <= 0) return;

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
        yield return new WaitForSecondsRealtime(1.5f);
        SceneManager.LoadScene("Gameplay");
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Car"))
            Die();
    }

    void Die()
    {
        gameOver = true;
        multiplierText.text = "You died! x" + multipliers[Mathf.Clamp(currentLane, 0, multipliers.Length - 1)].ToString("F2");
        foreach (var lane in lanes)
            lane.StopLane();
        Debug.Log("Game Over! Lane reached: " + currentLane);
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

        // ✅ also refresh each lane text (in case something overwrote it)
        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i].multiplierText != null)
                lanes[i].multiplierText.text = "x" + multipliers[i].ToString("F2");
        }
    }

}
