using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Veri analizi için

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
    private int[] targetWordStates;

    private float timeRemaining = 180f;
    private bool isExamRunning = false;

    // Resmi Sýnav Ýstatistik Deðiþkenleri
    private int currentDogru = 0;
    private int currentYanlis = 0;
    private int currentAtlanan = 0;
    private int currentBosluk = 0;
    private int currentTargetIndex = 0;

    // --- SÜPER TELEMETRÝ SENSÖRLERÝ (ARKAPLAN AJANI) ---
    private string sessionStartTime;
    private int correctKeys = 0;
    private int wrongKeys = 0;
    private int backspaceCount = 0;
    private int currentCombo = 0;
    private int maxCombo = 0;

    private float lastKeystrokeTime = 0f;
    private float longestIdleTime = 0f;
    private float timeToFirstKeystroke = 0f;
    private bool isFirstKeystrokePressed = false;

    private int frameCount = 0;
    private float totalDeltaTime = 0f;

    private Dictionary<string, int> letterErrorDict = new Dictionary<string, int>();
    private List<KeyConfusion> keyConfusionList = new List<KeyConfusion>();

    void Start()
    {
        renkYardimiToggle.onValueChanged.AddListener(delegate { UpdateDisplays(); });
        LoadSelectedTxtFile();

        sagMetinText.text = "";
        typedText = "";

        // Sensörleri Baþlat
        sessionStartTime = System.DateTime.Now.ToString("HH:mm:ss");
        lastKeystrokeTime = Time.time;

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

        // FPS ve Geçen Süre Ölçümü
        frameCount++;
        totalDeltaTime += Time.deltaTime;

        if (!isFirstKeystrokePressed)
        {
            timeToFirstKeystroke += Time.deltaTime; // Metni okuma ve hazýrlanma süresi
        }

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (timeRemaining <= 0)
        {
            EndExam();
            return;
        }

        foreach (char c in Input.inputString)
        {
            isFirstKeystrokePressed = true;

            // Kilitlenme (Panik) Süresini Ölç
            float timeSinceLastKey = Time.time - lastKeystrokeTime;
            if (timeSinceLastKey > longestIdleTime) longestIdleTime = timeSinceLastKey;
            lastKeystrokeTime = Time.time;

            if (c == '\b')
            {
                if (typedText.Length > 0)
                {
                    typedText = typedText.Substring(0, typedText.Length - 1);
                    backspaceCount++; // Silme tuþu casusu
                }
            }
            else if (c == '\n' || c == '\r') continue;
            else
            {
                // --- HARF BAZLI HATA VE KAS HAFIZASI KONTROLÜ ---
                if (typedText.Length < originalText.Length)
                {
                    char expectedChar = originalText[typedText.Length];
                    char pressedChar = c;

                    if (pressedChar == expectedChar)
                    {
                        correctKeys++;
                        currentCombo++;
                        if (currentCombo > maxCombo) maxCombo = currentCombo;
                    }
                    else
                    {
                        wrongKeys++;
                        currentCombo = 0;

                        string expectedStr = expectedChar.ToString().ToUpper();
                        string pressedStr = pressedChar.ToString().ToUpper();

                        // Hatalý Harf Çetelesi
                        if (letterErrorDict.ContainsKey(expectedStr)) letterErrorDict[expectedStr]++;
                        else letterErrorDict.Add(expectedStr, 1);

                        // Tuþ Karýþtýrma Casusu
                        if (pressedStr != " " && pressedStr != "")
                        {
                            KeyConfusion existingKvp = keyConfusionList.FirstOrDefault(k => k.expectedChar == expectedStr && k.pressedChar == pressedStr);
                            if (existingKvp != null) existingKvp.count++;
                            else keyConfusionList.Add(new KeyConfusion { expectedChar = expectedStr, pressedChar = pressedStr, count = 1 });
                        }
                    }
                }

                typedText += c;
            }

            UpdateDisplays();

            // Eðer yazýlan kelime sayýsý hedefi geçtiyse (Metin bittiyse) sýnavý erken bitir
            if (currentDogru + currentYanlis + currentAtlanan >= targetWords.Length) EndExam();
        }
    }

    void EvaluateExam(bool isFinal)
    {
        currentDogru = 0; currentYanlis = 0; currentAtlanan = 0; currentBosluk = 0;
        targetWordStates = new int[targetWords.Length];

        string[] typedWordsRaw = typedText.Split(' ');
        List<string> islenmisKelimeler = new List<string>();
        bool oncekiBosluktu = false;

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
        if (islenmisKelimeler.Count > 0 && !typedText.EndsWith(" ")) degerlendirilecekKelime--;

        int oIndex = 0;

        for (int tIndex = 0; tIndex < degerlendirilecekKelime; tIndex++)
        {
            if (oIndex >= targetWords.Length) break;

            string tWord = islenmisKelimeler[tIndex];
            string oWord = targetWords[oIndex];

            if (tWord == oWord)
            {
                currentDogru++;
                targetWordStates[oIndex] = 1;
                oIndex++;
            }
            else
            {
                bool resynced = false;
                for (int skip = 1; skip <= 30; skip++)
                {
                    if (oIndex + skip >= targetWords.Length) break;
                    if (tWord == targetWords[oIndex + skip])
                    {
                        bool isValidJump = true;
                        if (skip > 2 && tIndex + 1 < degerlendirilecekKelime && oIndex + skip + 1 < targetWords.Length)
                        {
                            if (islenmisKelimeler[tIndex + 1] != targetWords[oIndex + skip + 1]) isValidJump = false;
                        }

                        if (isValidJump)
                        {
                            currentAtlanan += skip;
                            for (int s = 0; s < skip; s++) targetWordStates[oIndex + s] = 3;
                            oIndex += skip;
                            currentDogru++;
                            targetWordStates[oIndex] = 1;
                            oIndex++;
                            resynced = true;
                            break;
                        }
                    }
                }

                if (!resynced)
                {
                    if (tIndex + 1 < degerlendirilecekKelime && islenmisKelimeler[tIndex + 1] == oWord) currentYanlis++;
                    else
                    {
                        currentYanlis++;
                        targetWordStates[oIndex] = 2;
                        oIndex++;
                    }
                }
            }
        }
        currentTargetIndex = oIndex;
    }

    void UpdateDisplays()
    {
        EvaluateExam(false);
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
                    else if (targetWordStates[i] == 3) leftDisplay += $"<color=#888888>{targetWords[i]}</color> ";
                }
                else if (i == currentTargetIndex) leftDisplay += $"<color=#0000FF><b><u>{targetWords[i]}</u></b></color> ";
                else leftDisplay += $"<color=#000000>{targetWords[i]}</color> ";
            }
        }
        else leftDisplay = string.Join(" ", targetWords);

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

        float activeDuration = 180f - timeRemaining; // Ne kadar süre yazdý?
        if (activeDuration < 0.1f) activeDuration = 0.1f; // Sýfýra bölme hatasýný önle

        timeRemaining = 0;
        UpdateTimerUI();

        EvaluateExam(true);

        int toplamYazilanKelime = currentDogru + currentYanlis;
        int netKelime = currentDogru - currentBosluk;
        if (netKelime < 0) netKelime = 0;

        float hataOrani = 0;
        if (toplamYazilanKelime > 0) hataOrani = ((float)currentYanlis / toplamYazilanKelime) * 100f;

        bool basarili = true;
        string sebep = "";

        if (currentAtlanan >= 22) { basarili = false; sebep = "22 veya daha fazla kelime atlandýðý için sýnav geçersiz."; }
        else if (hataOrani > 25f) { basarili = false; sebep = $"Hata oraný %{Mathf.RoundToInt(hataOrani)} (Anlam bütünlüðü bozuldu)."; }
        else if (netKelime < 90) { basarili = false; sebep = "90 Net kelime barajý aþýlamadý."; }
        else { sebep = "Tebrikler, tüm kriterleri baþarýyla geçtiniz!"; }

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

        // --- DEVASA TELEMETRÝ PAKETLEME (KATÝPLÝK) ---
        List<LetterError> errorList = new List<LetterError>();
        foreach (var kvp in letterErrorDict) { errorList.Add(new LetterError { harf = kvp.Key, hataSayisi = kvp.Value }); }

        int totalStrokes = correctKeys + wrongKeys + backspaceCount;
        int avgFPS = totalDeltaTime > 0 ? Mathf.RoundToInt(frameCount / totalDeltaTime) : 60;
        float avgKeyDelay = totalStrokes > 0 ? (activeDuration / totalStrokes) : 0f;

        // Oynadýðý dosya ismini klasör yolundan (Path) temizleyip çekiyoruz
        string rawPath = PlayerPrefs.GetString("SecilenMetinYolu", "Bilinmeyen Metin");
        string cleanTextName = Path.GetFileNameWithoutExtension(rawPath);

        GameSession bitenOyunVerisi = new GameSession
        {
            playDate = System.DateTime.Now.ToString("dd.MM.yyyy"),
            startTime = sessionStartTime ?? "Bilinmiyor",
            endTime = System.DateTime.Now.ToString("HH:mm:ss"),
            durationSeconds = activeDuration,

            gameMode = "Katiplik",
            playedTextName = cleanTextName,
            quitReason = (activeDuration >= 180f) ? "Süre Bitti" : "Metin Bitti", // Neden bitti?

            totalKeystrokes = totalStrokes,
            correctKeystrokes = correctKeys,
            wrongKeystrokes = wrongKeys,
            backspaceCount = backspaceCount, // Sadece bu modda var!
            maxCombo = maxCombo,

            timeToFirstKeystroke = timeToFirstKeystroke,
            longestIdleTime = longestIdleTime,
            averageTimeBetweenKeys = avgKeyDelay,
            averageFPS = avgFPS,

            detayliHarfHatalari = errorList,
            tusKaristirmalari = keyConfusionList,
            failedWords = new List<string>(), // Katiplik'te kelime üstümüze gelmiyor

            grossWPM = activeDuration > 0 ? Mathf.RoundToInt(((correctKeys + wrongKeys) / 5f) / (activeDuration / 60f)) : 0,
            netWPM = netKelime, // Net WPM doðrudan net kelimedir
            accuracy = hataOrani > 0 ? (100f - hataOrani) : 100f,

            arcadeLevelReached = 0,
            arcadeWordsDefeated = 0,

            katiplikAtlananKelime = currentAtlanan,
            katiplikBoslukHatasi = currentBosluk,
            isKatiplikPassed = basarili
        };

        StatManager.SaveSession(bitenOyunVerisi);
    }

    public void RestartExam() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene("AnaMenu"); }
}