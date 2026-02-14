using System.Collections.Generic;
using UnityEngine;

public class WordManager : MonoBehaviour
{
    public List<Word> words;
    public WordSpawner wordSpawner;

    private bool hasActiveWord;
    private Word activeWord;

    [Header("Dalga Ayarlarý")]
    public float wordSpawnDelay = 2.0f; // Baþlangýçta 2 saniyede 1 kelime gelir
    private float nextWordTime = 0f;

    private void Start()
    {
        words = new List<Word>();
    }

    private void Update()
    {
        // Zamaný geldiyse yeni kelime üret (Dalga Sistemi)
        if (Time.time >= nextWordTime)
        {
            AddWord();
            nextWordTime = Time.time + wordSpawnDelay;

            // Profesyonel Dokunuþ: Her kelimede oyun MÝKRO seviyede hýzlanýr (Zorluk eðrisi)
            // Süreyi %2 kýsaltýyoruz ama 0.5 saniyenin altýna inmesine izin vermiyoruz
            wordSpawnDelay = Mathf.Max(0.5f, wordSpawnDelay * 0.98f);
        }

        // Klavye dinleyici
        foreach (char letter in Input.inputString)
        {
            TypeLetter(letter);
        }
    }

    public void AddWord()
    {
        // WordGenerator'dan rastgele kelime çek
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

        if (hasActiveWord && activeWord.WordTyped())
        {
            hasActiveWord = false;
            words.Remove(activeWord);
        }
    }
}