using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Slider için gerekli kütüphane
using UnityEngine.SceneManagement;

public class WordManager : MonoBehaviour
{
    [Header("Oyun Akýþý ve Baþlangýç")]
    public GameObject startPanel; // Baþlama ekraný
    public bool hasStarted = false; // Oyun baþladý mý?
    private float gameStartTime = 0f; // WPM hesaplamak için süreyi tutacaðýz

    public List<Word> words;
    public WordSpawner wordSpawner;

    private bool hasActiveWord;
    private Word activeWord;

    [Header("Matematiksel Zorluk (Flow Sistemi)")]
    public TextMeshProUGUI levelText; // Seviye yazýsý
    public int currentLevel = 1;
    public float baseSpawnDelay = 2.5f;
    public float baseWordSpeed = 80f;

    private float currentSpawnDelay;
    private float nextWordTime = 0f;

    [Header("Can Sistemi")]
    public Slider healthBar; // Can çubuðu
    public float maxHealth = 100f;
    private float currentHealth;
    public float normalDamage = 10f; // Normal kelime 10 can götürür
    public float bossDamage = 30f; // Boss 30 can götürür
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
    public TextMeshProUGUI finalStatsText; // Detaylý oyun sonu yazýsý
    private bool isGameOver = false;

    // Detaylý Ýstatistik Deðiþkenleri
    private int totalWordsTyped = 0;
    private int correctKeystrokes = 0;
    private int wrongKeystrokes = 0;

    private void Start()
    {
        WordGenerator.LoadWordsFromTxt();
        words = new List<Word>();

        // Can barýný ayarla
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        // Oyun baþlangýç ayarlarý
        hasStarted = false;
        startPanel.SetActive(true); // Baþla panelini göster
        gameOverPanel.SetActive(false);
        UpdateLevelUI();

        Time.timeScale = 1f;
        CalculateDifficulty();
    }

    // BUTONA BASILDIÐINDA ÇALIÞACAK FONKSÝYON
    public void OyunaBasla()
    {
        hasStarted = true;
        startPanel.SetActive(false); // Paneli gizle
        gameStartTime = Time.time; // Süreyi baþlat
        nextWordTime = Time.time + 1f; // 1 saniye sonra ilk kelime gelsin
    }

    private void Update()
    {
        // Eðer oyun baþlamadýysa veya bittiyse klavyeyi okuma, kelime üretme!
        if (!hasStarted || isGameOver) return;

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
        currentSpawnDelay = Mathf.Max(0.4f, baseSpawnDelay - (currentLevel * 0.1f) - (bossScoreCounter * 0.03f));
    }

    private void UpdateLevelUI()
    {
        if (levelText != null) levelText.text = "SEVÝYE: " + currentLevel;
    }

    public void AddWord()
    {
        CalculateDifficulty();
        float currentWordSpeed = Mathf.Min(400f, baseWordSpeed + (currentLevel * 5f) + (bossScoreCounter * 1.5f));

        string randomWord = WordGenerator.GetRandomWord();
        WordDisplay wordDisplay = wordSpawner.SpawnWord(currentWordSpeed);
        Word newWord = new Word(randomWord, wordDisplay);
        words.Add(newWord);
    }

    public void TypeLetter(char letter)
    {
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

        // Ýstatistik için tuþ vuruþlarýný kaydet
        if (isCorrectHit) correctKeystrokes++;
        else wrongKeystrokes++;

        if (sfxSource != null)
        {
            if (isCorrectHit) sfxSource.PlayOneShot(dogruTusSesi);
            else sfxSource.PlayOneShot(yanlisTusSesi, 0.7f); // Yanlýþ sesi çok az kýstým
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
                UpdateLevelUI(); // Ekranda seviyeyi güncelle

                nextWordTime = Time.time + 3f;
                CalculateDifficulty();
            }
            else
            {
                bossScoreCounter++;
                int targetBossScore = 8 + (currentLevel * 4);
                if (bossScoreCounter >= targetBossScore && !isBossActive) StartBossFight();
            }
        }
    }

    public void TakeDamage(Word hitWord)
    {
        if (isBossActive) currentHealth -= bossDamage;
        else currentHealth -= normalDamage;

        if (sfxSource != null) sfxSource.PlayOneShot(yanlisTusSesi, 1.5f);

        // Can barýný (Slider) güncelle
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
            currentHealth = 0; // Eksiye düþmesini engelle
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

        int bossWordCount = Mathf.Min(18, 8 + (currentLevel * 2));
        string bossText = WordGenerator.GetRandomBossText(bossWordCount);
        float bossSpeed = 13f + (currentLevel * 2.5f);

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

        // DETAYLI ÝSTATÝSTÝK HESAPLAMALARI
        float timeElapsedMinutes = (Time.time - gameStartTime) / 60f;
        int wpm = 0;
        if (timeElapsedMinutes > 0.05f)
        {
            wpm = Mathf.RoundToInt((totalWordsTyped) / timeElapsedMinutes);
        }

        float accuracy = 0f;
        int totalStrokes = correctKeystrokes + wrongKeystrokes;
        if (totalStrokes > 0)
        {
            accuracy = ((float)correctKeystrokes / totalStrokes) * 100f;
        }

        // Çok satýrlý þýk rapor çýktýsý
        finalStatsText.text =
            $"ULAÞILAN SEVÝYE: {currentLevel}\n" +
            $"PATLATILAN KELÝME: {totalWordsTyped}\n\n" +
            $"HIZ (WPM): {wpm}\n" +
            $"DOÐRULUK: %{Mathf.RoundToInt(accuracy)}";
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("AnaMenu"); }
}