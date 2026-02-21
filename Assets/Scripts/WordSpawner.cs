using UnityEngine;

public class WordSpawner : MonoBehaviour
{
    public GameObject wordPrefab;
    public Transform wordCanvas;

    // YENÝ: Artýk fonksiyona dýþarýdan bir hýz (moveSpeed) deðeri gönderiyoruz
    public WordDisplay SpawnWord(float moveSpeed)
    {
        float randomAngle = Random.Range(0f, 360f);
        float spawnRadius = 800f;

        Vector3 spawnPosition = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * spawnRadius,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * spawnRadius,
            0f
        );

        GameObject wordObj = Instantiate(wordPrefab, spawnPosition, Quaternion.identity, wordCanvas);
        wordObj.GetComponent<RectTransform>().localPosition = spawnPosition;

        WordDisplay wordDisplay = wordObj.GetComponent<WordDisplay>();

        // Üretilen kelimenin hýzýný, manager'dan gelen matematiðe göre ayarla!
        wordDisplay.speed = moveSpeed;

        return wordDisplay;
    }
}