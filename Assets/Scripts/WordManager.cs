using System.Collections.Generic;
using UnityEngine;

public class WordManager : MonoBehaviour
{
    public List<Word> words; // Ekrandaki aktif kelimeler listesi
    public WordSpawner wordSpawner; // Kelimeleri üretecek script (Bunu birazdan ekleyeceðiz)

    private bool hasActiveWord; // Bir kelimeye kilitlendik mi?
    private Word activeWord;    // Üzerinde çalýþtýðýmýz aktif kelime

    private void Start()
    {
        words = new List<Word>();
        // Test için baþlangýçta 3 tane manuel kelime ekliyoruz
        AddWord("katiplik");
        AddWord("sinav");
        AddWord("adalet");
    }

    // Ýleride veritabanýndan/array'den kelime çekeceðimiz fonksiyon
    public void AddWord(string wordString)
    {
        WordDisplay wordDisplay = wordSpawner.SpawnWord();
        Word newWord = new Word(wordString, wordDisplay);
        words.Add(newWord);
    }

    private void Update()
    {
        // Klavye girdilerini dinle
        foreach (char letter in Input.inputString)
        {
            TypeLetter(letter);
        }
    }

    public void TypeLetter(char letter)
    {
        // Eðer zaten bir kelime yazmaya baþladýysak, sadece o kelimeyi kontrol et
        if (hasActiveWord)
        {
            if (activeWord.GetNextLetter() == letter)
            {
                activeWord.TypeLetter();
            }
        }
        else // Eðer aktif kilitli bir kelime yoksa, basýlan harfle baþlayan bir kelime bul
        {
            foreach (Word word in words)
            {
                if (word.GetNextLetter() == letter)
                {
                    activeWord = word;
                    hasActiveWord = true;
                    word.TypeLetter();
                    break; // Ýlk bulduðuna kilitlen ve aramayý býrak
                }
            }
        }

        // Kilitlendiðimiz kelime bittiyse kilidi aç ve listeden çýkar
        if (hasActiveWord && activeWord.WordTyped())
        {
            hasActiveWord = false;
            words.Remove(activeWord);
        }
    }
}