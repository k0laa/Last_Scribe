using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO; // Dosya okuma kütüphanesi
using System.Collections.Generic;

public class KatiplikManager : MonoBehaviour
{
    [Header("A4 Kaðýdý Arayüzleri")]
    public TextMeshProUGUI solMetinText; // Sol kaðýttaki orjinal metin
    public TextMeshProUGUI sagMetinText; // Sað kaðýttaki bizim yazdýðýmýz

    [Header("Göstergeler ve Kontroller")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statsText;
    public Toggle renkYardimiToggle; // Renk yardýmý tuþu

    [Header("Sýnav Sonu Paneli")]
    public GameObject sonucPaneli;
    public TextMeshProUGUI netWpmText;
    public TextMeshProUGUI dogrulukText;

    private string originalText = "";
    private string typedText = ""; // Bizim sað kaðýda yazdýðýmýz saf metin

    private float timeRemaining = 180f;
    private bool isExamRunning = false;

    private int totalKeystrokes = 0;
    private int correctKeystrokes = 0;

    void Start()
    {
        renkYardimiToggle.onValueChanged.AddListener(delegate { UpdateDisplays(); });

        LoadSelectedTxtFile(); // ARTIK RASTGELE DEÐÝL, SEÇÝLENÝ YÜKLÜYORUZ

        sagMetinText.text = "";
        typedText = "";

        isExamRunning = true;
        UpdateDisplays();
    }

    // --- YENÝ: HAFIZADAKÝ SEÇÝLÝ DOSYAYI OKUMA ---
    void LoadSelectedTxtFile()
    {
        // Metin seçim ekranýnda kaydettiðimiz dosya yolunu al
        string secilenYol = PlayerPrefs.GetString("SecilenMetinYolu", "");

        if (secilenYol != "" && File.Exists(secilenYol))
        {
            originalText = File.ReadAllText(secilenYol, System.Text.Encoding.UTF8);
            originalText = originalText.Replace("\r", "").Trim();
        }
        else
        {
            // Eðer bir hata olursa veya direkt bu sahne açýlýrsa uyarý ver
            originalText = "Lutfen once Metin Secim ekranindan bir metin secin!";
        }
    }

    void Update()
    {
        if (!isExamRunning) return;

        // Süre kontrolü
        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();
        if (timeRemaining <= 0) { EndExam(); return; }

        // --- YENÝ: KLAVYE VE BACKSPACE YÖNETÝMÝ ---
        foreach (char c in Input.inputString)
        {
            if (c == '\b') // Backspace (Silme Tuþu)
            {
                if (typedText.Length > 0)
                {
                    typedText = typedText.Substring(0, typedText.Length - 1); // Saðdaki yazýdan 1 harf sil
                }
            }
            else if ((c == '\n') || (c == '\r'))
            {
                // Katiplik sýnavlarýnda bazen enter gerekir, þimdilik metni bozmamasý için engelliyoruz veya boþluða çeviriyoruz
                typedText += " ";
            }
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

            UpdateDisplays(); // Her tuþ basýmýnda her iki kaðýdý da güncelle

            if (typedText.Length >= originalText.Length) { EndExam(); }
        }
    }

    // --- YENÝ: ÇÝFT KAÐIT GÜNCELLEME SÝSTEMÝ ---
    void UpdateDisplays()
    {
        // 1. Sað Kaðýt: Doðrudan kullanýcýnýn yazdýklarýný gösterir (Word gibi)
        sagMetinText.text = typedText;

        // 2. Sol Kaðýt: Eðer renk yardýmý açýksa renklendirir, kapalýysa saf siyah gösterir
        string leftDisplay = "";

        if (renkYardimiToggle.isOn)
        {
            for (int i = 0; i < originalText.Length; i++)
            {
                if (i < typedText.Length)
                {
                    if (typedText[i] == originalText[i]) leftDisplay += $"<color=green>{originalText[i]}</color>";
                    else leftDisplay += $"<color=red><u>{originalText[i]}</u></color>";
                }
                else if (i == typedText.Length) leftDisplay += $"<color=#0000FF><b><u>{originalText[i]}</u></b></color>"; // Yazýlacak sýradaki harf (Mavi ve altý çizili)
                else leftDisplay += $"<color=#000000>{originalText[i]}</color>"; // Yazýlmamýþ kýsým siyah
            }
        }
        else
        {
            // Renk yardýmý kapalýysa orjinal metin düz siyah görünür (Gerçek bir kaðýt gibi)
            // Ýstersen okunabilirlik için oyuncunun nerede kaldýðýný belli eden ufacýk bir iþaret koyabiliriz ama þimdilik resmi formata uygun düz siyah.
            leftDisplay = originalText;
        }

        solMetinText.text = leftDisplay;
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
            float wpm = (totalKeystrokes / 5f) / minutesElapsed;
            float accuracy = 0f;
            if (totalKeystrokes > 0) accuracy = ((float)correctKeystrokes / totalKeystrokes) * 100f;
            statsText.text = $"WPM: {Mathf.RoundToInt(wpm)} | Doðruluk: %{Mathf.RoundToInt(accuracy)}";
        }
    }

    void EndExam()
    {
        isExamRunning = false;

        float minutesElapsed = (180f - timeRemaining) / 60f;
        float finalWpm = 0f;
        float finalAccuracy = 0f;

        if (minutesElapsed > 0f)
        {
            finalWpm = (totalKeystrokes / 5f) / minutesElapsed;
            if (totalKeystrokes > 0) finalAccuracy = ((float)correctKeystrokes / totalKeystrokes) * 100f;
        }

        sonucPaneli.SetActive(true);
        netWpmText.text = "Net Hýz: " + Mathf.RoundToInt(finalWpm) + " WPM";
        dogrulukText.text = "Doðruluk: %" + Mathf.RoundToInt(finalAccuracy);
        timeRemaining = 0;
        UpdateTimerUI();
    }

    public void RestartExam() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene("AnaMenu"); }
}