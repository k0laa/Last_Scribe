using UnityEngine;
using TMPro;

public class WordDisplay : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float fallSpeed = 1f;

    public void SetWord(string word)
    {
        textMesh.text = word;
    }

    public void RemoveLetter()
    {
        textMesh.text = textMesh.text.Remove(0, 1);
        textMesh.color = Color.red; // Geçici olarak yazýlaný belli etmek için kýrmýzý yapalým, sonra RichText (HTML) ile daha profesyonel renklendireceðiz.
    }

    public void RemoveWord()
    {
        // Burada ileride patlama efekti (Particle System) tetikleyeceðiz
        Destroy(gameObject);
    }

    private void Update()
    {
        // Kelimenin merkeze (veya aþaðýya) doðru hareket etmesi
        transform.Translate(0f, -fallSpeed * Time.deltaTime, 0f);
    }
}