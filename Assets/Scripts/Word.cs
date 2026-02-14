using UnityEngine;

[System.Serializable]
public class Word
{
    public string word;
    private int typeIndex;

    // ÝÞTE BURAYI DEÐÝÞTÝRDÝK (public yaptýk)
    public WordDisplay display;

    public Word(string _word, WordDisplay _display)
    {
        word = _word;
        typeIndex = 0;
        display = _display;
        display.SetWord(word);
    }

    public char GetNextLetter()
    {
        return word[typeIndex];
    }

    public void TypeLetter()
    {
        typeIndex++;
        display.RemoveLetter();
    }

    public bool WordTyped()
    {
        bool isTyped = (typeIndex >= word.Length);
        if (isTyped)
        {
            display.RemoveWord();
        }
        return isTyped;
    }
}