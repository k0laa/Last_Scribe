using UnityEngine;

public class WordSpawner : MonoBehaviour
{
    public GameObject wordPrefab;
    public Transform wordCanvas;

    public WordDisplay SpawnWord()
    {
        // 0 ile 360 derece arasýnda rastgele bir açý seç
        float randomAngle = Random.Range(0f, 360f);
        
        // Merkezden ne kadar uzakta doðacaklar? (Ekran dýþý olmasý için büyük bir deðer)
        float spawnRadius = 800f; 

        // Trigonometri kullanarak açýya göre X ve Y baþlangýç noktasýný buluyoruz
        Vector3 spawnPosition = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * spawnRadius,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * spawnRadius,
            0f
        );

        // WordPrefab'i Canvas'ýn içinde üret
        GameObject wordObj = Instantiate(wordPrefab, spawnPosition, Quaternion.identity, wordCanvas);
        
        // Eðer Canvas'ýn Pivot ayarlarý merkeze göre deðilse, yerel pozisyonu düzeltiyoruz
        wordObj.GetComponent<RectTransform>().localPosition = spawnPosition;

        WordDisplay wordDisplay = wordObj.GetComponent<WordDisplay>();
        return wordDisplay;
    }
}