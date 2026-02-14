using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Sahneler arasý geçiþ için eklendi

public class KatiplikManager : MonoBehaviour
{
    [Header("Arayüz Baðlantýlarý")]
    public TextMeshProUGUI targetTextDisplay;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statsText;

    [Header("Sýnav Sonu Ekraný")]
    public GameObject sonucPaneli;
    public TextMeshProUGUI netWpmText;
    public TextMeshProUGUI dogrulukText;

    [Header("Sýnav Ayarlarý")]
    private string originalText = "adalet bakanligi tarafindan duzenlenen zabit katipligi sinavinda basarili olmak icin uc dakika icinde en az doksan kelime yazmak gerekmektedir bu yuzden bol bol pratik yapilmalidir ve hatalar en aza indirilmelidir";

    private string typedText = "";
    private float timeRemaining = 180f; // 3 Dakika
    private bool isExamRunning = false;

    private int totalKeystrokes = 0;
    private int correctKeystrokes = 0;

    // Sonuçlar için aklýnda tutacaðý sayýlar
    private float finalWpm = 0f;
    private float finalAccuracy = 0f;

    void Start()
    {
        isExamRunning = true;
        UpdateDisplay();
    }
    void Update()
    {
        if (!isExamRunning) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0)
        {
            EndExam();
            return;
        }

        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (typedText.Length > 0) typedText = typedText.Substring(0, typedText.Length - 1);
            }
            else if ((c == '\n') || (c == '\r')) continue;
            else
            {
                if (typedText.Length < originalText.Length)
                {
                    typedText += c;
                    totalKeystrokes++;

                    if (typedText[typedText.Length - 1] == originalText[typedText.Length - 1])
                    {
                        correctKeystrokes++;
                    }
                }
            }
            UpdateDisplay();
        }

        // --- YENÝ EKLENEN KISIM: Metin tamamen bittiyse sýnavý bitir! ---
        if (typedText.Length >= originalText.Length)
        {
            EndExam();
        }
    }

    void UpdateDisplay()
    {
        string displayText = "";
        for (int i = 0; i < originalText.Length; i++)
        {
            if (i < typedText.Length)
            {
                if (typedText[i] == originalText[i]) displayText += $"<color=green>{originalText[i]}</color>";
                else displayText += $"<color=red><u>{originalText[i]}</u></color>";
            }
            else if (i == typedText.Length) displayText += $"<color=yellow><b>{originalText[i]}</b></color>";
            else displayText += $"<color=#888888>{originalText[i]}</color>";
        }
        targetTextDisplay.text = displayText;
        CalculateStats();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void CalculateStats()
    {
        float minutesElapsed = (180f - timeRemaining) / 60f;
        if (minutesElapsed > 0.05f)
        {
            finalWpm = (totalKeystrokes / 5f) / minutesElapsed;
            if (totalKeystrokes > 0) finalAccuracy = ((float)correctKeystrokes / totalKeystrokes) * 100f;

            statsText.text = $"WPM: {Mathf.RoundToInt(finalWpm)} | Doðruluk: %{Mathf.RoundToInt(finalAccuracy)}";
        }
    }

    void EndExam()
    {
        isExamRunning = false;
        timeRemaining = 0;
        UpdateTimerUI();

        // Paneli aç ve sonuçlarý yazdýr
        sonucPaneli.SetActive(true);
        netWpmText.text = "Net Hýz: " + Mathf.RoundToInt(finalWpm) + " WPM";
        dogrulukText.text = "Doðruluk: %" + Mathf.RoundToInt(finalAccuracy);
    }

    // Butonlar için fonksiyonlar
    public void RestartExam()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("AnaMenu"); // Birazdan bu sahneyi oluþturacaðýz
    }
}