using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq; // Yeni veri analizleri için gerekli

public class WordManager : MonoBehaviour
{
    [Header("Oyun Akýþý ve Baþlangýç")]
    public GameObject startPanel;
    public bool hasStarted = false;
    private float effectiveGameTime = 0f;

    public List<Word> words;
    public WordSpawner wordSpawner;

    private bool hasActiveWord;
    private Word activeWord;

    [Header("Matematiksel Zorluk (Flow Sistemi)")]
    public TextMeshProUGUI levelText;
    public int currentLevel = 1;
    public float baseSpawnDelay = 2.5f;
    public float baseWordSpeed = 80f;
    private float currentSpawnDelay;
    private float nextWordTime = 0f;

    [Header("Geliþmiþ Can Sistemi (100 HP)")]
    public Slider healthBar;
    public float maxHealth = 100f;
    private float currentHealth;
    public float normalDamage = 10f;
    public float bossDamage = 30f;
    public float damageDistance = 50f;

    [Header("Görsel ve Ses Efektleri")]
    public GameObject explosionPrefab;
    public Animator playerAnimator;
    private float animTimer = 0f;
    private float animSuresi = 0.15f;

    public AudioSource sfxSource;
    public AudioClip dogruTusSesi;
    public AudioClip yanlisTusSesi;
    public AudioClip patlamaSesi;
    public AudioClip bossMuzigi;

    [Header("Boss Fight Sistemi")]
    public bool isBossActive = false;
    public GameObject bossPrefab;
    private int bossScoreCounter = 0;

    [Header("Oyun Sonu ve Ýstatistikler")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalStatsText;
    private bool isGameOver = false;

    // --- SÜPER TELEMETRÝ SENSÖRLERÝ ---
    private string sessionStartTime;
    private int totalWordsTyped = 0;
    private int correctKeystrokes = 0;
    private int wrongKeystrokes = 0;
    private int currentCombo = 0;
    private int maxCombo = 0;

    private Dictionary<string, int> letterErrorDict = new Dictionary<string, int>();
    private List<KeyConfusion> keyConfusionList = new List<KeyConfusion>();
    private List<string> failedWordsList = new List<string>();

    private float lastKeystrokeTime = 0f;
    private float longestIdleTime = 0f;
    private float timeToFirstKeystroke = 0f;
    private bool isFirstKeystrokePressed = false;

    private int frameCount = 0;
    private float totalDeltaTime = 0f;

    private void Start()
    {
        WordGenerator.LoadWordsFromTxt();
        words = new List<Word>();

        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        hasStarted = false;
        effectiveGameTime = 0f;
        sessionStartTime = System.DateTime.Now.ToString("HH:mm:ss"); // Saati kaydet

        startPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        UpdateLevelUI();

        Time.timeScale = 1f;
        CalculateDifficulty();
    }

    public void OyunaBasla()
    {
        hasStarted = true;
        startPanel.SetActive(false);
        nextWordTime = Time.time + 1f;
        lastKeystrokeTime = Time.time; // Idle time ölçümü için baþlangýç zamaný
    }

    private void Update()
    {
        if (!hasStarted || isGameOver) return;

        // FPS Ölçeði
        frameCount++;
        totalDeltaTime += Time.deltaTime;

        if (words.Count > 0 || isBossActive)
        {
            effectiveGameTime += Time.deltaTime;

            // Ýlk tuþa basma süresini ölç (Ekranda kelime varken ama tuþa basýlmadýysa)
            if (!isFirstKeystrokePressed)
            {
                timeToFirstKeystroke += Time.deltaTime;
            }
        }

        if (!isBossActive && Time.time >= nextWordTime)
        {
            AddWord();
            nextWordTime = Time.time + currentSpawnDelay;
        }

        for (int i = words.Count - 1; i >= 0; i--)
        {
            if (words[i].display != null && Vector3.Distance(words[i].display.transform.localPosition, Vector3.zero) < damageDistance)
            {
                TakeDamage(words[i]);
            }
        }

        if (animTimer > 0)
        {
            animTimer -= Time.deltaTime;
            if (animTimer <= 0 && playerAnimator != null) playerAnimator.SetBool("isTyping", false);
        }

        foreach (char letter in Input.inputString)
        {
            TypeLetter(letter);
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isTyping", true);
                animTimer = animSuresi;
            }
        }
    }

    private void CalculateDifficulty()
    {
        currentSpawnDelay = Mathf.Max(0.4f, baseSpawnDelay - (currentLevel * 0.2f) - (bossScoreCounter * 0.05f));
    }

    private void UpdateLevelUI() { if (levelText != null) levelText.text = "SEVÝYE: " + currentLevel; }

    public void AddWord()
    {
        CalculateDifficulty();
        float currentWordSpeed = Mathf.Min(400f, baseWordSpeed + (currentLevel * 15f) + (bossScoreCounter * 2f));

        string randomWord = WordGenerator.GetRandomWord();
        WordDisplay wordDisplay = wordSpawner.SpawnWord(currentWordSpeed);
        Word newWord = new Word(randomWord, wordDisplay);
        words.Add(newWord);
    }

    public void TypeLetter(char letter)
    {
        isFirstKeystrokePressed = true; // Ýlk tuþa basýldý, refleks kronometresi durdu

        // Idle (Duraksama) Süresini Hesapla
        float timeSinceLastKey = Time.time - lastKeystrokeTime;
        if (timeSinceLastKey > longestIdleTime) longestIdleTime = timeSinceLastKey;
        lastKeystrokeTime = Time.time;

        bool isCorrectHit = false;

        if (hasActiveWord)
        {
            if (activeWord.GetNextLetter() == letter)
            {
                activeWord.TypeLetter();
                isCorrectHit = true;
            }
        }
        else
        {
            foreach (Word word in words)
            {
                if (word.GetNextLetter() == letter)
                {
                    activeWord = word;
                    hasActiveWord = true;
                    word.TypeLetter();
                    isCorrectHit = true;
                    break;
                }
            }
        }

        if (isCorrectHit)
        {
            correctKeystrokes++;
            currentCombo++;
            if (currentCombo > maxCombo) maxCombo = currentCombo;
        }
        else
        {
            wrongKeystrokes++;
            currentCombo = 0;

            // --- AJAN SENSÖRÜ: Kas Hafýzasý Þaþýrmasý (A yerine S'ye mi bastý?) ---
            if (hasActiveWord)
            {
                string expectedStr = activeWord.GetNextLetter().ToString().ToUpper();
                string pressedStr = letter.ToString().ToUpper();

                // Düz Hata Çetelesi
                if (letterErrorDict.ContainsKey(expectedStr)) letterErrorDict[expectedStr]++;
                else letterErrorDict.Add(expectedStr, 1);

                // Tuþ Karýþtýrma Çetelesi (Key Confusion)
                if (pressedStr != " " && pressedStr != "") // Boþluk vb. kaydetmeye gerek yok
                {
                    KeyConfusion existingKvp = keyConfusionList.FirstOrDefault(k => k.expectedChar == expectedStr && k.pressedChar == pressedStr);
                    if (existingKvp != null) existingKvp.count++;
                    else keyConfusionList.Add(new KeyConfusion { expectedChar = expectedStr, pressedChar = pressedStr, count = 1 });
                }
            }
        }

        if (sfxSource != null)
        {
            if (isCorrectHit) sfxSource.PlayOneShot(dogruTusSesi);
            else sfxSource.PlayOneShot(yanlisTusSesi, 0.5f);
        }

        if (hasActiveWord && activeWord.WordTyped())
        {
            Instantiate(explosionPrefab, activeWord.display.transform.position, Quaternion.identity, wordSpawner.wordCanvas);
            if (sfxSource != null && patlamaSesi != null) sfxSource.PlayOneShot(patlamaSesi);

            hasActiveWord = false;
            words.Remove(activeWord);
            totalWordsTyped++;

            if (isBossActive)
            {
                isBossActive = false;
                bossScoreCounter = 0;
                currentLevel++;
                UpdateLevelUI();
                nextWordTime = Time.time + 3f;
                CalculateDifficulty();
            }
            else
            {
                bossScoreCounter++;
                int targetBossScore = 8 + (currentLevel * 2);
                if (bossScoreCounter >= targetBossScore && !isBossActive) StartBossFight();
            }
        }
    }

    public void TakeDamage(Word hitWord)
    {
        // --- SENSÖR: Bizi vuran kelimeleri kara listeye al ---
        failedWordsList.Add(hitWord.word);

        if (isBossActive) currentHealth -= bossDamage;
        else currentHealth -= normalDamage;

        if (sfxSource != null) sfxSource.PlayOneShot(yanlisTusSesi, 1.5f);
        if (healthBar != null) healthBar.value = currentHealth;

        hitWord.display.RemoveWord();
        words.Remove(hitWord);

        if (activeWord == hitWord) hasActiveWord = false;

        if (isBossActive)
        {
            isBossActive = false;
            bossScoreCounter = 0;
            CalculateDifficulty();
            nextWordTime = Time.time + 2f;
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            GameOver();
        }
    }

    public void StartBossFight()
    {
        isBossActive = true;
        if (sfxSource != null && bossMuzigi != null) sfxSource.PlayOneShot(bossMuzigi);

        for (int i = words.Count - 1; i >= 0; i--)
        {
            if (words[i].display != null) Destroy(words[i].display.gameObject);
        }
        words.Clear();
        hasActiveWord = false;

        int bossWordCount = 8 + (currentLevel * 2);
        string bossText = WordGenerator.GetRandomBossText(bossWordCount);
        float bossSpeed = 8f + (currentLevel * 1.5f);

        Vector3 bossSpawnPos = new Vector3(800f, 0f, 0f);
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity, wordSpawner.wordCanvas);
        bossObj.GetComponent<RectTransform>().localPosition = bossSpawnPos;

        WordDisplay bossDisplay = bossObj.GetComponent<WordDisplay>();
        bossDisplay.speed = bossSpeed;

        Word bossWord = new Word(bossText, bossDisplay);
        words.Add(bossWord);
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        // Ýstatistikleri Hesapla
        float activeTimeMinutes = effectiveGameTime / 60f;
        int wpm = activeTimeMinutes > 0.01f ? Mathf.RoundToInt((correctKeystrokes / 5f) / activeTimeMinutes) : 0;

        int totalStrokes = correctKeystrokes + wrongKeystrokes;
        float accuracy = totalStrokes > 0 ? ((float)correctKeystrokes / totalStrokes) * 100f : 0f;
        int avgFPS = totalDeltaTime > 0 ? Mathf.RoundToInt(frameCount / totalDeltaTime) : 60;
        float avgKeyDelay = totalStrokes > 0 ? (effectiveGameTime / totalStrokes) : 0f;

        finalStatsText.text =
            $"ULAÞILAN SEVÝYE: {currentLevel}\n" +
            $"PATLATILAN KELÝME: {totalWordsTyped}\n\n" +
            $"NET HIZ (WPM): {wpm}\n" +
            $"DOÐRULUK: %{Mathf.RoundToInt(accuracy)}";

        // Harf Sözlüðünü JSON Formatýna Çevir
        List<LetterError> errorList = new List<LetterError>();
        foreach (var kvp in letterErrorDict) { errorList.Add(new LetterError { harf = kvp.Key, hataSayisi = kvp.Value }); }

        // DEVASA KARGOYU PAKETLE
        GameSession bitenOyunVerisi = new GameSession
        {
            playDate = System.DateTime.Now.ToString("dd.MM.yyyy"),
            startTime = sessionStartTime ?? "Bilinmiyor",
            endTime = System.DateTime.Now.ToString("HH:mm:ss"),
            durationSeconds = effectiveGameTime,

            gameMode = "Arcade",
            playedTextName = "Rastgele Kelimeler Dalgalarý",
            quitReason = "Can Bitti (Health Depleted)",

            totalKeystrokes = totalStrokes,
            correctKeystrokes = correctKeystrokes,
            wrongKeystrokes = wrongKeystrokes,
            backspaceCount = 0, // Arcade'de yok
            maxCombo = maxCombo,

            timeToFirstKeystroke = timeToFirstKeystroke,
            longestIdleTime = longestIdleTime,
            averageTimeBetweenKeys = avgKeyDelay,
            averageFPS = avgFPS,

            detayliHarfHatalari = errorList,
            tusKaristirmalari = keyConfusionList,
            failedWords = failedWordsList,

            grossWPM = activeTimeMinutes > 0 ? Mathf.RoundToInt((totalStrokes / 5f) / activeTimeMinutes) : 0,
            netWPM = wpm,
            accuracy = accuracy,

            arcadeLevelReached = currentLevel,
            arcadeWordsDefeated = totalWordsTyped
        };

        // GÖNDER!
        StatManager.SaveSession(bitenOyunVerisi);
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("AnaMenu"); }
}