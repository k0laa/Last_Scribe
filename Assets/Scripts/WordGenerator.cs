using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class WordGenerator : MonoBehaviour
{
    private static List<string> allWords = new List<string>(); // Tüm kelimelerin havuzu
    private static List<string> allParagraphs = new List<string>(); // Boss için tüm metinlerin havuzu

    // Oyun baþladýðýnda WordManager bu fonksiyonu çaðýrýp dosyalarý okutacak
    public static void LoadWordsFromTxt()
    {
        allWords.Clear();
        allParagraphs.Clear();

        string folderPath = Application.streamingAssetsPath;

        if (Directory.Exists(folderPath))
        {
            string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");

            foreach (string file in txtFiles)
            {
                // Dosyayý oku ve hayalet noktalarý (\u0307) temizle
                string text = File.ReadAllText(file, System.Text.Encoding.UTF8);
                text = text.Replace("\r", "").Replace("\n", " ").Replace("\u0307", "").Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    allParagraphs.Add(text); // Boss için bütün metni havuza at

                    // Metni kelimelere böl ve kelime havuzuna at
                    string[] words = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    allWords.AddRange(words);
                }
            }
        }

        // Eðer klasörde hiç txt yoksa (hata olmamasý için) yedek kelimeler ekle
        if (allWords.Count == 0)
        {
            allWords.AddRange(new string[] { "adalet", "katiplik", "sinav", "dosya", "mahkeme" });
            allParagraphs.Add("adalet mulkun temelidir katiplik sinavinda basarili olmak icin bol pratik yapmalisin");
        }
    }

    // Arcade moduna tekil kelime fýrlatýr
    public static string GetRandomWord()
    {
        if (allWords.Count == 0) LoadWordsFromTxt(); // Güvenlik önlemi

        int randomIndex = Random.Range(0, allWords.Count);
        return allWords[randomIndex];
    }

    // Boss için rastgele bir metinden 15 kelimelik bir parça kesip verir
    public static string GetRandomBossText(int wordCount = 15)
    {
        if (allParagraphs.Count == 0) LoadWordsFromTxt();

        // Rastgele bir metin seç
        string randomText = allParagraphs[Random.Range(0, allParagraphs.Count)];
        string[] words = randomText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Eðer seçilen metin zaten 15 kelimeden kýsaysa tamamýný gönder
        if (words.Length <= wordCount) return randomText;

        // Metnin hep baþýndan gelmemesi için rastgele bir baþlangýç noktasý seç
        int startIndex = Random.Range(0, words.Length - wordCount);

        List<string> bossWords = new List<string>();
        for (int i = 0; i < wordCount; i++)
        {
            bossWords.Add(words[startIndex + i]);
        }

        // Seçilen 15 kelimeyi aralarýnda boþluk býrakarak birleþtir ve gönder
        return string.Join(" ", bossWords);
    }
}