using UnityEngine;

[System.Serializable]
public class Word
{
    public string word;
    private int typeIndex;
    private WordDisplay display; // Ekranda hangi UI nesnesine baðlý olduðunu bilir

    public Word(string _word, WordDisplay _display)
    {
        word = _word;
        typeIndex = 0;
        display = _display;
        display.SetWord(word);
    }

    // Sýradaki harfi döndürür
    public char GetNextLetter()
    {
        return word[typeIndex];
    }

    // Doðru harfe basýldýðýnda çalýþýr
    public void TypeLetter()
    {
        typeIndex++;
        display.RemoveLetter(); // Görselden harfi sil/boya
    }

    // Kelimenin tamamý yazýldý mý?
    public bool WordTyped()
    {
        bool isTyped = (typeIndex >= word.Length);
        if (isTyped)
        {
            display.RemoveWord(); // Ekranda yok et
        }
        return isTyped;
    }
}