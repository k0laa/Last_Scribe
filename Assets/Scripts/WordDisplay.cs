using UnityEngine;
using TMPro;

public class WordDisplay : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float speed; // UI piksel hýzýmýz (Artýk çok daha hýzlý!)

    public void SetWord(string word)
    {
        textMesh.text = word;
    }

    public void RemoveLetter()
    {
        // Doðru yazýlan harfi görselden sil ve kalanlarý göster
        textMesh.text = textMesh.text.Remove(0, 1); 
        textMesh.color = new Color32(255, 124, 0, 255);
    }

    public void RemoveWord()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        // UI nesnesini ekranýn merkezine (0,0 noktasýna) doðru hareket ettir
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.MoveTowards(rectTransform.localPosition, Vector3.zero, speed * Time.deltaTime);
    }
}