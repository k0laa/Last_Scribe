using UnityEngine;

public class WordSpawner : MonoBehaviour
{
    public GameObject wordPrefab;
    public Transform wordCanvas; // Kelimelerin içine doðacaðý Canvas

    public WordDisplay SpawnWord()
    {
        // Rastgele bir yatay pozisyon belirle (Ekran geniþliðine göre daha sonra ince ayar yapacaðýz)
        Vector3 randomPosition = new Vector3(Random.Range(-300f, 300f), 400f, 0f);

        // Prefab'i üret
        GameObject wordObj = Instantiate(wordPrefab, randomPosition, Quaternion.identity, wordCanvas);

        WordDisplay wordDisplay = wordObj.GetComponent<WordDisplay>();
        return wordDisplay;
    }
}