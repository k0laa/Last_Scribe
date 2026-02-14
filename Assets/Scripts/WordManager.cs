using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro (Can yazýsý) için kütüphane

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
    public int playerHealth = 5; // Oyuncunun 5 caný var
    public TextMeshProUGUI healthText; // Ekrandaki Can yazýsý
    public float damageDistance = 50f; // Kelime merkeze ne kadar yaklaþýrsa çarpýþma sayýlacak?

    [Header("Görsel Efektler (Juice)")]
    public GameObject explosionPrefab; // Hazýrladýðýmýz patlama efekti

    private void Start()
    {
        words = new List<Word>();
        UpdateHealthUI(); // Oyun baþlarken caný ekrana yazdýr
    }

    private void Update()
    {
        // Zamanlayýcý (Kelime Üretimi)
        if (Time.time >= nextWordTime)
        {
            AddWord();
            nextWordTime = Time.time + wordSpawnDelay;
            wordSpawnDelay = Mathf.Max(0.5f, wordSpawnDelay * 0.98f);
        }

        // --- YENÝ: ÇARPIÞMA KONTROLÜ ---
        // Listeyi sondan baþa doðru tarýyoruz (Çünkü listeden kelime sileceðiz, hata vermemesi için)
        for (int i = words.Count - 1; i >= 0; i--)
        {
            // Eðer kelimenin merkeze (Vector3.zero) uzaklýðý bizim belirlediðimiz hasar mesafesinden kýsaysa:
            if (words[i].display != null && Vector3.Distance(words[i].display.transform.localPosition, Vector3.zero) < damageDistance)
            {
                TakeDamage(words[i]); // Hasar al!
            }
        }

        // Klavye Girdisi
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
            if (activeWord.GetNextLetter() == letter)
            {
                activeWord.TypeLetter();
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
                    break;
                }
            }
        }

        // --- YENÝ: KELÝME BÝTÝNCE PATLAMA ---
        if (hasActiveWord && activeWord.WordTyped())
        {
            // Kelimenin olduðu pozisyonda patlama efektini yarat!
            Instantiate(explosionPrefab, activeWord.display.transform.position, Quaternion.identity, wordSpawner.wordCanvas);

            hasActiveWord = false;
            words.Remove(activeWord);
        }
    }

    // --- YENÝ: HASAR ALMA SÝSTEMÝ ---
    public void TakeDamage(Word hitWord)
    {
        playerHealth--; // Caný 1 azalt
        UpdateHealthUI(); // Ekranda güncelle

        // Çarpan kelimeyi yok et ve listeden sil
        hitWord.display.RemoveWord();
        words.Remove(hitWord);

        // Eðer yazdýðýmýz kelime bize çarptýysa, klavye kilidini aç ki baþkasýna geçebilelim
        if (activeWord == hitWord)
        {
            hasActiveWord = false;
        }

        // Can sýfýrlandýysa
        if (playerHealth <= 0)
        {
            Debug.Log("OYUN BÝTTÝ!");
            // Ýleride buraya oyun bitiþ ekraný (Game Over) ekleyeceðiz
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "CAN: " + playerHealth.ToString();
        }
    }
}