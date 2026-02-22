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
    public Transform grafikAlani;
    public GameObject barPrefab;

    public float maksimumGrafikBoyu = 400f;

    private PlayerStatsData allData;

    void Start()
    {
        modFiltresi.onValueChanged.AddListener(delegate { GrafigiCiz(); });
        allData = StatManager.LoadAllData();
        GrafigiCiz();
    }

    public void GrafigiCiz()
    {
        foreach (Transform child in grafikAlani) { Destroy(child.gameObject); }

        if (allData.allSessions.Count == 0)
        {
            ortalamaWpmText.text = "Henüz Veri Yok";
            return;
        }

        int seciliFiltre = modFiltresi.value;
        List<GameSession> filtrelenmisListe = new List<GameSession>();

        foreach (var session in allData.allSessions)
        {
            if (seciliFiltre == 0) filtrelenmisListe.Add(session);
            else if (seciliFiltre == 1 && session.gameMode == "Arcade") filtrelenmisListe.Add(session);
            else if (seciliFiltre == 2 && session.gameMode == "Katiplik") filtrelenmisListe.Add(session);
        }

        int toplamWpm = 0;
        float toplamDogruluk = 0f;

        int enYuksekWpm = 1;
        // YENÝ SÝSTEM: Artýk wpm deðil, netWPM deðerini okuyoruz
        foreach (var s in filtrelenmisListe) { if (s.netWPM > enYuksekWpm) enYuksekWpm = s.netWPM; }

        int baslangicIndex = Mathf.Max(0, filtrelenmisListe.Count - 20);

        for (int i = baslangicIndex; i < filtrelenmisListe.Count; i++)
        {
            GameSession seans = filtrelenmisListe[i];

            toplamWpm += seans.netWPM; // YENÝ
            toplamDogruluk += seans.accuracy;

            GameObject bar = Instantiate(barPrefab, grafikAlani);

            RectTransform barRect = bar.GetComponent<RectTransform>();
            float oran = (float)seans.netWPM / enYuksekWpm; // YENÝ
            barRect.sizeDelta = new Vector2(50f, maksimumGrafikBoyu * oran);

            TextMeshProUGUI barText = bar.GetComponentInChildren<TextMeshProUGUI>();
            barText.text = seans.netWPM.ToString(); // YENÝ

            if (seans.gameMode == "Katiplik") bar.GetComponent<Image>().color = new Color32(200, 50, 50, 255);
            else bar.GetComponent<Image>().color = new Color32(50, 150, 250, 255);
        }

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