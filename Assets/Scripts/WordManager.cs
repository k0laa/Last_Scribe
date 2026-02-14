using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // YENÝ: Sahneyi yeniden baþlatmak için gerekli kütüphane

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

    [Header("Boss Fight Sistemi")]
    public int bossThreshold = 10;
    public bool isBossActive = false;
    public GameObject bossPrefab;

    // Boss için olan skor ayrý, toplam skor ayrý.
    private int bossScoreCounter = 0;

    [Header("Oyun Sonu (Game Over) Sistemi")]
    public int totalWordsTyped = 0; // Ablanýn oyun boyunca yazdýðý toplam kelime
    public GameObject gameOverPanel; // Kararan ekran panelimiz
    public TextMeshProUGUI finalScoreText; // Oyun sonu skoru
    private bool isGameOver = false; // Oyun bitti mi kontrolü

    private void Start()
    {
        words = new List<Word>();
        UpdateHealthUI();
        Time.timeScale = 1f; // Oyun baþladýðýnda zamanýn normal aktýðýndan emin ol
    }

    private void Update()
    {
        if (isGameOver) return; // Oyun bittiyse hiçbir þey yapma!

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

        foreach (char letter in Input.inputString)
        {
            TypeLetter(letter);
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
        if (hasActiveWord)
        {
            if (activeWord.GetNextLetter() == letter) activeWord.TypeLetter();
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
                    break;
                }
            }
        }

        if (hasActiveWord && activeWord.WordTyped())
        {
            Instantiate(explosionPrefab, activeWord.display.transform.position, Quaternion.identity, wordSpawner.wordCanvas);
            hasActiveWord = false;
            words.Remove(activeWord);

            totalWordsTyped++; // YENÝ: Toplam yazýlan kelimeyi 1 artýr

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

        if (playerHealth <= 0)
        {
            GameOver(); // YENÝ: Can sýfýrlanýnca bu fonksiyonu çaðýr
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = "CAN: " + playerHealth.ToString();
    }

    public void StartBossFight()
    {
        isBossActive = true;
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

    // --- YENÝ: OYUN BÝTÝÞ FONKSÝYONLARI ---
    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Zamaný tamamen durdur (Kelimeler donar)
        gameOverPanel.SetActive(true); // Sakladýðýmýz paneli görünür yap
        finalScoreText.text = "TOPLAM YAZILAN: " + totalWordsTyped.ToString(); // Skoru ekrana bas
    }

    // Butona basýnca çalýþacak fonksiyon
    public void RestartGame()
    {
        // Zamaný tekrar normale döndür ve þu anki sahneyi baþtan yükle
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Zamaný normale döndürmeyi unutmuyoruz!
        SceneManager.LoadScene("AnaMenu"); // Ana menü sahnesinin adýný birebir ayný yazmalýsýn
    }
}