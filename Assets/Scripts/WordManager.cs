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

    [Header("Dalga Ayarlarý")]
    public float wordSpawnDelay = 2.0f;
    private float nextWordTime = 0f;

    [Header("Oyuncu ve Can Sistemi")]
    public int playerHealth = 5;
    public TextMeshProUGUI healthText;
    public float damageDistance = 50f;

    [Header("Görsel Efektler")]
    public GameObject explosionPrefab;

    [Header("Oyuncu Animasyonu")]
    public Animator playerAnimator;
    private float animTimer = 0f;
    private float animSuresi = 0.15f;

    [Header("Ses Efektleri (SFX)")]
    public AudioSource sfxSource; // Ýkinci eklediðimiz boþ kaset çalar
    public AudioClip dogruTusSesi;
    public AudioClip yanlisTusSesi;
    public AudioClip patlamaSesi;
    public AudioClip bossMuzigi; // Ýsteðe baðlý, boss gelince çalacak ses

    [Header("Boss Fight Sistemi")]
    public int bossThreshold = 10;
    public bool isBossActive = false;
    public GameObject bossPrefab;
    private int bossScoreCounter = 0;

    [Header("Oyun Sonu Sistemi")]
    public int totalWordsTyped = 0;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    private bool isGameOver = false;

    private void Start()
    {
        words = new List<Word>();
        UpdateHealthUI();
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (isGameOver) return;

        if (!isBossActive && Time.time >= nextWordTime)
        {
            AddWord();
            nextWordTime = Time.time + wordSpawnDelay;
            wordSpawnDelay = Mathf.Max(0.5f, wordSpawnDelay * 0.98f);
        }

        for (int i = words.Count - 1; i >= 0; i--)
        {
            if (words[i].display != null && Vector3.Distance(words[i].display.transform.localPosition, Vector3.zero) < damageDistance)
            {
                TakeDamage(words[i]);
            }
        }

        // Animasyon Zamanlayýcýsý
        if (animTimer > 0)
        {
            animTimer -= Time.deltaTime;
            if (animTimer <= 0 && playerAnimator != null)
            {
                playerAnimator.SetBool("isTyping", false);
            }
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

    public void AddWord()
    {
        string randomWord = WordGenerator.GetRandomWord();
        WordDisplay wordDisplay = wordSpawner.SpawnWord();
        Word newWord = new Word(randomWord, wordDisplay);
        words.Add(newWord);
    }

    public void TypeLetter(char letter)
    {
        bool isCorrectHit = false; // Tuþa doðru mu bastýk kontrolü

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

        // --- YENÝ EKLENEN: SES ÇALMA MANTIÐI ---
        if (sfxSource != null)
        {
            // Eðer tuþa doðru bastýysak doðru sesi, yanlýþ bastýysak yanlýþ sesi tek seferlik (PlayOneShot) çal
            if (isCorrectHit)
                sfxSource.PlayOneShot(dogruTusSesi);
            else
                sfxSource.PlayOneShot(yanlisTusSesi);
        }

        if (hasActiveWord && activeWord.WordTyped())
        {
            Instantiate(explosionPrefab, activeWord.display.transform.position, Quaternion.identity, wordSpawner.wordCanvas);

            // Kelime patlayýnca patlama sesini çal
            if (sfxSource != null && patlamaSesi != null) sfxSource.PlayOneShot(patlamaSesi);

            hasActiveWord = false;
            words.Remove(activeWord);
            totalWordsTyped++;

            if (isBossActive)
            {
                isBossActive = false;
                bossScoreCounter = 0;
                nextWordTime = Time.time + 3f;
            }
            else
            {
                bossScoreCounter++;
                if (bossScoreCounter >= bossThreshold && !isBossActive)
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

        // Ekstra: Can gidince uyarý sesi olarak yanlýþ sesi biraz yüksek çalabiliriz
        if (sfxSource != null) sfxSource.PlayOneShot(yanlisTusSesi, 1.5f);

        UpdateHealthUI();
        hitWord.display.RemoveWord();
        words.Remove(hitWord);

        if (activeWord == hitWord) hasActiveWord = false;

        if (isBossActive)
        {
            isBossActive = false;
            bossScoreCounter = 0;
            nextWordTime = Time.time + 2f;
        }

        if (playerHealth <= 0) GameOver();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = "CAN: " + playerHealth.ToString();
    }

    public void StartBossFight()
    {
        isBossActive = true;

        // Boss gelirken o korkutucu sesi çal
        if (sfxSource != null && bossMuzigi != null) sfxSource.PlayOneShot(bossMuzigi);

        for (int i = words.Count - 1; i >= 0; i--)
        {
            if (words[i].display != null) Destroy(words[i].display.gameObject);
        }
        words.Clear();
        hasActiveWord = false;

        string bossText = "adalet mulkun temelidir katiplik sinavinda basarili olmak icin hem hizli hem de hatasiz yazmaniz gerekmektedir derin bir nefes al ve parmaklarina guven";
        Vector3 bossSpawnPos = new Vector3(800f, 0f, 0f);
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity, wordSpawner.wordCanvas);
        bossObj.GetComponent<RectTransform>().localPosition = bossSpawnPos;

        WordDisplay bossDisplay = bossObj.GetComponent<WordDisplay>();
        Word bossWord = new Word(bossText, bossDisplay);
        words.Add(bossWord);
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
        finalScoreText.text = "TOPLAM YAZILAN: " + totalWordsTyped.ToString();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("AnaMenu");
    }
}