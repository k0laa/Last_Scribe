using UnityEngine;

public class WordGenerator : MonoBehaviour
{
    // Katiplik sýnavýna uygun resmi ve hukuki kelime havuzu
    private static string[] wordList = {
        "adalet", "mahkeme", "tutanak", "savunma", "karar",
        "yargi", "kanun", "hukuk", "davaci", "sanik",
        "tanik", "dosya", "kalem", "zabit", "katiplik",
        "durusma", "itiraz", "hukum", "cagri", "belge"
    };

    public static string GetRandomWord()
    {
        int randomIndex = Random.Range(0, wordList.Length);
        return wordList[randomIndex];
    }
}