using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class WordManager : MonoBehaviour
{
    public List<Word> words;
    public WordSpawner wordSpawner;

    private bool hasActiveWord;
    private Word activeWord;

    [Header("Matematiksel Zorluk (Flow Sistemi)")]
    public int currentLevel = 1; // Oyuncunun þu anki dalgasý/seviyesi
    public float baseSpawnDelay = 2.5f; // Baþlangýç doðma süresi
    public float baseWordSpeed = 80f; // Baþlangýç kelime düþme hýzý (Yavaþ baþlar)

    // Anlýk olarak hesaplanacak deðerler
    private float currentSpawnDelay;
    private float nextWordTime = 0f;

    [Header("Oyuncu ve Can Sistemi")]
    public int playerHealth = 5;
    public TextMeshProUGUI healthText;
    public float damageDistance = 50f;

    [Header("Görsel Efektler")]
    public GameObject explosionPrefab;
    public Animator playerAnimator;
    private float animTimer = 0f;
    private float animSuresi = 0.15f;

    [Header("Ses Efektleri (SFX)")]
    public AudioSource sfxSource;
    public AudioClip dogruTusSesi;
    public AudioClip yanlisTusSesi;
    public AudioClip patlamaSesi;
    public AudioClip bossMuzigi;

    [Header("Boss Fight Sistemi")]
    public bool isBossActive = false;
    public GameObject bossPrefab;
    private int bossScoreCounter = 0; // Bu seviyede kesilen kelime sayýsý

    [Header("Oyun Sonu Sistemi")]
    public int totalWordsTyped = 0;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    private bool isGameOver = false;

    private void Start()
    {
        WordGenerator.LoadWordsFromTxt();
        words = new List<Word>();
        UpdateHealthUI();
        Time.timeScale = 1f;

        // Ýlk seviyenin hýzýný hesapla
        CalculateDifficulty();
    }

    private void Update()
    {
        if (isGameOver) return;

        // Eðer boss yoksa ve zamaný geldiyse kelime üret
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

    // --- PROFESYONEL ZORLUK DENKLEMÝ ---
    private void CalculateDifficulty()
    {
        // 1. Doðma Süresi (Spawn Delay):
        // Seviye arttýkça (0.2sn) daha hýzlý baþlar. Dalga içinde ilerledikçe (0.05sn) hýzlanýr.
        // Ama insan limitini aþmamasý için Asla 0.4 saniyenin altýna düþmez (Hard Cap).
        currentSpawnDelay = Mathf.Max(0.4f, baseSpawnDelay - (currentLevel * 0.2f) - (bossScoreCounter * 0.05f));
    }

    public void AddWord()
    {
        CalculateDifficulty(); // Her kelime çýkýþýnda zorluðu mikro düzeyde artýr

        // 2. Kelime Düþme Hýzý (Word Speed):
        // Temel hýz + (Seviye x 15) + (O dalgadaki skor x 2)
        // Ýnsan gözünün takibini bozmamak için maksimum hýz 400'e sabitlendi (Max Cap).
        float currentWordSpeed = Mathf.Min(400f, baseWordSpeed + (currentLevel * 15f) + (bossScoreCounter * 2f));

        string randomWord = WordGenerator.GetRandomWord();
        WordDisplay wordDisplay = wordSpawner.SpawnWord(currentWordSpeed); // Hýzý fabrikaya gönder!

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

        if (sfxSource != null)
        {
            if (isCorrectHit) sfxSource.PlayOneShot(dogruTusSesi);
            else sfxSource.PlayOneShot(yanlisTusSesi);
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
                // --- BOSS KESÝLDÝ - SEVÝYE ATLA! (TESTERE DÝÞÝ EÐRÝSÝ) ---
                isBossActive = false;
                bossScoreCounter = 0;
                currentLevel++; // Seviyeyi artýr

                // Oyuncuya nefes almasý için 3 saniye süre ver
                nextWordTime = Time.time + 3f;
                CalculateDifficulty(); // Hýzlarý yeni seviyeye göre sýfýrla/ayarla
            }
            else
            {
                bossScoreCounter++;

                // 3. Dinamik Boss Eþiði (Boss Threshold):
                // Ýlk seviyede 10 kelimede boss gelir. Seviye 2'de 12, Seviye 3'te 14 kelimede...
                int targetBossScore = 8 + (currentLevel * 2);

                if (bossScoreCounter >= targetBossScore && !isBossActive)
                {
                    StartBossFight();
                }
            }
        }
    }

    public void TakeDamage(Word hitWord)
    {
        if (isBossActive) playerHealth -= 3;
        else playerHealth--;

        if (sfxSource != null) sfxSource.PlayOneShot(yanlisTusSesi, 1.5f);

        UpdateHealthUI();
        hitWord.display.RemoveWord();
        words.Remove(hitWord);

        if (activeWord == hitWord) hasActiveWord = false;

        if (isBossActive)
        {
            // Boss bize çarptýysa seviye atlama, ayný seviyeyi tekrar dene
            isBossActive = false;
            bossScoreCounter = 0;
            CalculateDifficulty();
            nextWordTime = Time.time + 2f;
        }

        if (playerHealth <= 0) GameOver();
    }

    private void UpdateHealthUI() { if (healthText != null) healthText.text = "CAN: " + playerHealth.ToString(); }

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

        // 4. Dinamik Boss Zorluðu:
        // Boss kelime sayýsý seviyeyle uzar (Örn: Seviye 1 = 10 kelime, Seviye 2 = 12 kelime...)
        int bossWordCount = 8 + (currentLevel * 2);
        string bossText = WordGenerator.GetRandomBossText(bossWordCount);

        // Boss'un yaklaþma hýzý da seviyeyle mikro düzeyde artar
        float bossSpeed = 8f + (currentLevel * 1.5f);

        Vector3 bossSpawnPos = new Vector3(800f, 0f, 0f);
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity, wordSpawner.wordCanvas);
        bossObj.GetComponent<RectTransform>().localPosition = bossSpawnPos;

        WordDisplay bossDisplay = bossObj.GetComponent<WordDisplay>();
        bossDisplay.speed = bossSpeed; // Boss hýzýný uygula

        Word bossWord = new Word(bossText, bossDisplay);
        words.Add(bossWord);
    }

    public void GameOver() { isGameOver = true; Time.timeScale = 0f; gameOverPanel.SetActive(true); finalScoreText.text = "TOPLAM YAZILAN: " + totalWordsTyped.ToString(); }
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("AnaMenu"); }
}