using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AnalyticsManager : MonoBehaviour
{
    [Header("Arayüz Baðlantýlarý")]
    public TMP_Dropdown modFiltresi;
    public TextMeshProUGUI ortalamaWpmText;
    public TextMeshProUGUI ortalamaDogrulukText;
    public Transform grafikAlani; // Horizontal Layout Group olan panel
    public GameObject barPrefab;  // Üreteceðimiz sütun

    public float maksimumGrafikBoyu = 400f; // Sütunlarýn çýkabileceði maksimum piksel yüksekliði

    private PlayerStatsData allData;

    void Start()
    {
        // Filtre deðiþtiðinde grafiði yeniden çiz!
        modFiltresi.onValueChanged.AddListener(delegate { GrafigiCiz(); });

        allData = StatManager.LoadAllData();
        GrafigiCiz();
    }

    public void GrafigiCiz()
    {
        // 1. Önce eski grafiði temizle
        foreach (Transform child in grafikAlani) { Destroy(child.gameObject); }

        if (allData.allSessions.Count == 0)
        {
            ortalamaWpmText.text = "Henüz Veri Yok";
            return;
        }

        // 2. Filtreye göre verileri seç
        int seciliFiltre = modFiltresi.value; // 0: Tümü, 1: Arcade, 2: Katiplik
        List<GameSession> filtrelenmisListe = new List<GameSession>();

        foreach (var session in allData.allSessions)
        {
            if (seciliFiltre == 0) filtrelenmisListe.Add(session);
            else if (seciliFiltre == 1 && session.gameMode == "Arcade") filtrelenmisListe.Add(session);
            else if (seciliFiltre == 2 && session.gameMode == "Katiplik") filtrelenmisListe.Add(session);
        }

        // 3. Ýstatistikleri Hesapla ve Grafiði Çiz
        int toplamWpm = 0;
        float toplamDogruluk = 0f;

        // Grafiði oranlamak için en yüksek WPM'i bul (En uzun sütun o olacak)
        int enYuksekWpm = 1;
        foreach (var s in filtrelenmisListe) { if (s.wpm > enYuksekWpm) enYuksekWpm = s.wpm; }

        // Sütunlarý üret (Sadece son 20 seansý göster ki ekran taþmasýn)
        int baslangicIndex = Mathf.Max(0, filtrelenmisListe.Count - 20);

        for (int i = baslangicIndex; i < filtrelenmisListe.Count; i++)
        {
            GameSession seans = filtrelenmisListe[i];

            toplamWpm += seans.wpm;
            toplamDogruluk += seans.accuracy;

            // Sütunu (Bar) yarat
            GameObject bar = Instantiate(barPrefab, grafikAlani);

            // Sütunun boyunu WPM'e göre orantýlý olarak uzat
            RectTransform barRect = bar.GetComponent<RectTransform>();
            float oran = (float)seans.wpm / enYuksekWpm;
            barRect.sizeDelta = new Vector2(50f, maksimumGrafikBoyu * oran); // Geniþlik 50px, Boy dinamik

            // Üzerine WPM deðerini yazdýr
            TextMeshProUGUI barText = bar.GetComponentInChildren<TextMeshProUGUI>();
            barText.text = seans.wpm.ToString();

            // Arcade mi Katiplik mi olduðunu rengiyle belli edebiliriz
            if (seans.gameMode == "Katiplik") bar.GetComponent<Image>().color = new Color32(200, 50, 50, 255); // Kýrmýzýmsý
            else bar.GetComponent<Image>().color = new Color32(50, 150, 250, 255); // Mavimsi
        }

        // 4. Ortalamalarý Ekrana Yaz
        if (filtrelenmisListe.Count > 0)
        {
            ortalamaWpmText.text = "Ortalama Hýz: " + (toplamWpm / filtrelenmisListe.Count) + " WPM";
            ortalamaDogrulukText.text = "Ortalama Doðruluk: %" + Mathf.RoundToInt(toplamDogruluk / filtrelenmisListe.Count);
        }
        else
        {
            ortalamaWpmText.text = "Bu Mod Ýçin Veri Yok";
            ortalamaDogrulukText.text = "";
        }
    }

    public void AnaMenuyeDon() { SceneManager.LoadScene("AnaMenu"); }
}