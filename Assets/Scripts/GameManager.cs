using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject winPanel;
    public GameObject losePanel;

    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        HidePanel(winPanel);
        HidePanel(losePanel);

        Time.timeScale = 1f;
    }

    public void LoseGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        ShowPanel(losePanel);
        DisableGameplay();
    }

    public void WinGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        ShowPanel(winPanel);
        DisableGameplay();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(true);

        // zapnutí interakce s panelem
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // odemknutí kurzoru
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HidePanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(false);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    private void DisableGameplay()
    {
        // zastavení času, hra skončila
        Time.timeScale = 0f;
    }
}
