using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonLoopExpand : MonoBehaviour
{
    public Button PlayButton;
    public float minScale = 1f;
    public float maxScale = 1.2f;
    public float speed = 2f; // how fast it expands/shrinks

    private Vector3 originalScale;

    void Start()
    {
        originalScale = PlayButton.transform.localScale;
    }

    void Update()
    {
        float scale = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(Time.time * speed, 1f));
        PlayButton.transform.localScale = originalScale * scale;
    }
    public void StartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }
}
