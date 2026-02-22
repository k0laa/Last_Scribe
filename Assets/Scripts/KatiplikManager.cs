using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class KatiplikManager : MonoBehaviour
{
    [Header("A4 Kaðýdý Arayüzleri")]
    public TextMeshProUGUI solMetinText;
    public TextMeshProUGUI sagMetinText;

    [Header("Göstergeler ve Kontroller")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statsText;
    public Toggle renkYardimiToggle;

    [Header("Sýnav Sonu Paneli")]
    public GameObject sonucPaneli;
    public TextMeshProUGUI brutText;
    public TextMeshProUGUI dogruText;
    public TextMeshProUGUI yanlisText;
    public TextMeshProUGUI atlananText;
    public TextMeshProUGUI boslukHataText;
    public TextMeshProUGUI netText;
    public TextMeshProUGUI durumText;
    public TextMeshProUGUI sebepText;

    private string originalText = "";
    private string typedText = "";

    private string[] targetWords;
    private int[] targetWordStates; // 0:Bekliyor, 1:Doðru, 2:Yanlýþ, 3:Atlandý

    private float timeRemaining = 180f;
    private bool isExamRunning = false;

    // Anlýk Ýstatistik Deðiþkenleri
    private int currentDogru = 0;
    private int currentYanlis = 0;
    private int currentAtlanan = 0;
    private int currentBosluk = 0;
    private int currentTargetIndex = 0;

    void Start()
    {
        renkYardimiToggle.onValueChanged.AddListener(delegate { UpdateDisplays(); });
        LoadSelectedTxtFile();

        sagMetinText.text = "";
        typedText = "";

        isExamRunning = true;
        UpdateDisplays();
    }

    void LoadSelectedTxtFile()
    {
        string secilenYol = PlayerPrefs.GetString("SecilenMetinYolu", "");
        if (secilenYol != "" && File.Exists(secilenYol))
        {
            originalText = File.ReadAllText(secilenYol, System.Text.Encoding.UTF8);
            originalText = originalText.Replace("\r", "").Replace("\n", " ").Trim();
            targetWords = originalText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            originalText = "lutfen metin secim ekranindan bir metin secin";
            targetWords = originalText.Split(' ');
        }
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
            else if (c == '\n' || c == '\r') continue;
            else typedText += c;

            UpdateDisplays();

            // Metin sonuna ulaþýldýysa bitir
            if (currentDogru + currentYanlis + currentAtlanan >= targetWords.Length) EndExam();
        }
    }

    // --- YENÝ ADALET BAKANLIÐI DEÐERLENDÝRME MOTORU ---
    // isFinal=false ise anlýk ekraný boyamak için, isFinal=true ise oyun bittiðinde net hesap için çalýþýr.
    void EvaluateExam(bool isFinal)
    {
        currentDogru = 0; currentYanlis = 0; currentAtlanan = 0; currentBosluk = 0;
        targetWordStates = new int[targetWords.Length];

        string[] typedWordsRaw = typedText.Split(' ');
        List<string> islenmisKelimeler = new List<string>();
        bool oncekiBosluktu = false;

        // 1. Fazla Boþluk Tespiti
        for (int i = 0; i < typedWordsRaw.Length; i++)
        {
            if (typedWordsRaw[i] == "")
            {
                if (!oncekiBosluktu && i != 0 && i != typedWordsRaw.Length - 1) currentBosluk++;
                oncekiBosluktu = true;
            }
            else
            {
                islenmisKelimeler.Add(typedWordsRaw[i]);
                oncekiBosluktu = false;
            }
        }

        int degerlendirilecekKelime = islenmisKelimeler.Count;

        // Kural: Bitmemiþ veya yazýlmakta olan son kelime hata sayýlmaz
        if (islenmisKelimeler.Count > 0 && !typedText.EndsWith(" ")) degerlendirilecekKelime--;

        int oIndex = 0; // Orijinal metin indexi

        // 2. Senkronizasyon (Lookahead) ve Karþýlaþtýrma Algoritmasý
        for (int tIndex = 0; tIndex < degerlendirilecekKelime; tIndex++)
        {
            if (oIndex >= targetWords.Length) break;

            string tWord = islenmisKelimeler[tIndex];
            string oWord = targetWords[oIndex];

            if (tWord == oWord) // Birebir Eþleþme
            {
                currentDogru++;
                targetWordStates[oIndex] = 1; // Yeþil
                oIndex++;
            }
            else // Hata veya Atlama durumu
            {
                bool resynced = false;

                // Ýleriye dönük 30 kelime ara (Aday büyük bir cümle atlamýþ olabilir mi?)
                for (int skip = 1; skip <= 30; skip++)
                {
                    if (oIndex + skip >= targetWords.Length) break;

                    if (tWord == targetWords[oIndex + skip])
                    {
                        bool isValidJump = true;
                        // Hatalý/Tesadüfi atlamalarý önlemek için 2 kelimeden büyük atlamalarda sonraki kelimeyi de teyit et
                        if (skip > 2 && tIndex + 1 < degerlendirilecekKelime && oIndex + skip + 1 < targetWords.Length)
                        {
                            if (islenmisKelimeler[tIndex + 1] != targetWords[oIndex + skip + 1]) isValidJump = false;
                        }

                        if (isValidJump)
                        {
                            currentAtlanan += skip;
                            // Atlanan kýsýmlarý gri renge boya
                            for (int s = 0; s < skip; s++) targetWordStates[oIndex + s] = 3;

                            oIndex += skip;
                            currentDogru++;
                            targetWordStates[oIndex] = 1; // Yakalanan yeni kelime yeþil
                            oIndex++;
                            resynced = true;
                            break;
                        }
                    }
                }

                if (!resynced)
                {
                    // Aday metinde olmayan ekstra bir kelime yazmýþ olabilir mi? (Örn: adalet kurum bakanlýðý)
                    if (tIndex + 1 < degerlendirilecekKelime && islenmisKelimeler[tIndex + 1] == oWord)
                    {
                        currentYanlis++; // Ekstra yazdýðý kelime yanlýþtýr.
                        // Orijinal index'i ilerletmiyoruz, sonraki döngüde asýl kelimeyi bulacak.
                    }
                    else
                    {
                        // Klasik yazým hatasý
                        currentYanlis++;
                        targetWordStates[oIndex] = 2; // Kýrmýzý
                        oIndex++;
                    }
                }
            }
        }

        currentTargetIndex = oIndex;
    }

    void UpdateDisplays()
    {
        EvaluateExam(false); // Anlýk deðerlendirmeyi çalýþtýr
        sagMetinText.text = typedText;

        string leftDisplay = "";

        if (renkYardimiToggle.isOn)
        {
            for (int i = 0; i < targetWords.Length; i++)
            {
                if (i < currentTargetIndex)
                {
                    if (targetWordStates[i] == 1) leftDisplay += $"<color=green>{targetWords[i]}</color> ";
                    else if (targetWordStates[i] == 2) leftDisplay += $"<color=red><u>{targetWords[i]}</u></color> ";
                    else if (targetWordStates[i] == 3) leftDisplay += $"<color=#888888>{targetWords[i]}</color> "; // Atlananlar Gri
                }
                else if (i == currentTargetIndex)
                {
                    leftDisplay += $"<color=#0000FF><b><u>{targetWords[i]}</u></b></color> ";
                }
                else leftDisplay += $"<color=#000000>{targetWords[i]}</color> ";
            }
        }
        else
        {
            leftDisplay = string.Join(" ", targetWords);
        }

        solMetinText.text = leftDisplay.Trim();

        float minutesElapsed = (180f - timeRemaining) / 60f;
        if (minutesElapsed > 0.05f)
        {
            int wpm = Mathf.RoundToInt((currentDogru / 5f) / minutesElapsed);
            statsText.text = $"Anlýk Hýz: {wpm} WPM";
        }
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndExam()
    {
        isExamRunning = false;
        timeRemaining = 0;
        UpdateTimerUI();

        EvaluateExam(true); // Kesin sonuçlar için son kez çalýþtýr

        int toplamYazilanKelime = currentDogru + currentYanlis;
        int netKelime = currentDogru - currentBosluk;
        if (netKelime < 0) netKelime = 0;

        float hataOrani = 0;
        if (toplamYazilanKelime > 0) hataOrani = ((float)currentYanlis / toplamYazilanKelime) * 100f;

        bool basarili = true;
        string sebep = "";

        if (currentAtlanan >= 22)
        {
            basarili = false;
            sebep = "22 veya daha fazla kelime atlandýðý için sýnav geçersiz.";
        }
        else if (hataOrani > 25f)
        {
            basarili = false;
            sebep = $"Hata oraný %{Mathf.RoundToInt(hataOrani)} (Anlam bütünlüðü bozuldu - %25 sýnýrý aþýldý).";
        }
        else if (netKelime < 90)
        {
            basarili = false;
            sebep = "90 Net kelime barajý aþýlamadý.";
        }
        else
        {
            sebep = "Tebrikler, tüm kriterleri baþarýyla geçtiniz!";
        }

        sonucPaneli.SetActive(true);
        brutText.text = "Toplam Yazýlan: " + toplamYazilanKelime;
        dogruText.text = "Doðru Kelime: " + currentDogru;
        yanlisText.text = "Yanlýþ Kelime: " + currentYanlis;
        atlananText.text = "Atlanan Kelime: " + currentAtlanan;
        boslukHataText.text = "Fazla Boþluk Hatasý: " + currentBosluk;

        netText.text = "NET KELÝME: " + netKelime;
        netText.color = basarili ? Color.green : Color.red;

        durumText.text = basarili ? "DURUM: BAÞARILI" : "DURUM: BAÞARISIZ";
        durumText.color = basarili ? Color.green : Color.red;
        sebepText.text = sebep;


        // --- VERÝTABANINA KAYIT SÝSTEMÝ ---
        // Sýnavýn kaç dakika sürdüðünü bul (Erken bittiyse diye)
        float minutesElapsed = (180f - timeRemaining) / 60f;
        int hesaplananWpm = 0;
        if (minutesElapsed > 0.05f) hesaplananWpm = Mathf.RoundToInt(netKelime / minutesElapsed);

        // Doðruluk oranýný hesapla (Doðru Kelime / Toplam Yazýlan)
        float hesaplananDogruluk = 0f;
        if (toplamYazilanKelime > 0) hesaplananDogruluk = ((float)currentDogru / toplamYazilanKelime) * 100f;

        // Verileri JSON dosyasýna gönder
        StatManager.SaveSession("Katiplik", hesaplananWpm, hesaplananDogruluk, toplamYazilanKelime, 0, netKelime);
    }

    public void RestartExam() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene("AnaMenu"); }
}