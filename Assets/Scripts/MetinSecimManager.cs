using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class MetinSecimManager : MonoBehaviour
{
    [Header("Sol Taraf: Liste")]
    public Transform listeIcerikAlani;
    public GameObject butonPrefab;

    [Header("Sağ Taraf: Detaylar")]
    public TextMeshProUGUI baslikText;
    public TextMeshProUGUI onizlemeText;
    public Toggle favoriToggle;
    public Button baslaButonu;

    private string seciliDosyaYolu = "";
    private string seciliDosyaAdi = "";

    // --- YENİ DEĞİŞKENLER ---
    private string suAnkiTamMetin = ""; // Metnin tamamını hafızada tutmak için
    private string[] tumDosyalar; // Rastgele seçim yapabilmek için tüm dosyaların listesi
    private bool arayuzGuncelleniyor = false; // BUG FİX: Kısır döngüyü kırmak için şalter

    void Start()
    {
        baslikText.text = "Sol listeden bir metin secin.";
        onizlemeText.text = "";
        baslaButonu.interactable = false;

        favoriToggle.onValueChanged.AddListener(delegate { FavoriDurumunuKaydet(); });

        ListeleMetinleri();
    }

    void ListeleMetinleri()
    {
        // Listeyi yenilemeden önce içindeki eski butonları temizle (Üst üste binmemesi için)
        foreach (Transform child in listeIcerikAlani) { Destroy(child.gameObject); }

        string folderPath = Application.streamingAssetsPath;
        if (!Directory.Exists(folderPath)) return;

        tumDosyalar = Directory.GetFiles(folderPath, "*.txt");

        foreach (string file in tumDosyalar)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            GameObject yeniButon = Instantiate(butonPrefab, listeIcerikAlani);
            TextMeshProUGUI butonYazisi = yeniButon.GetComponentInChildren<TextMeshProUGUI>();
            butonYazisi.text = fileName;

            if (PlayerPrefs.GetInt("FAV_" + fileName, 0) == 1)
            {
                butonYazisi.text = "★ " + fileName;
                butonYazisi.color = new Color(1f, 0.8f, 0f); // Yıldızlı olanları sarı/altın rengi yap
            }

            // Kapanım (Closure) sorunu yaşamamak için değişkenleri kopyalıyoruz
            string tiklananDosyaYolu = file;
            string tiklananDosyaAdi = fileName;
            yeniButon.GetComponent<Button>().onClick.AddListener(() => MetinSecildi(tiklananDosyaYolu, tiklananDosyaAdi));
        }
    }

    void MetinSecildi(string dosyaYolu, string dosyaAdi)
    {
        seciliDosyaYolu = dosyaYolu;
        seciliDosyaAdi = dosyaAdi;
        baslikText.text = dosyaAdi;

        suAnkiTamMetin = File.ReadAllText(dosyaYolu, System.Text.Encoding.UTF8);

        // Önizleme: Şimdilik sadece ilk 200 karakteri göster
        onizlemeText.text = suAnkiTamMetin;

        // --- BUG FİX UYGULAMASI ---
        arayuzGuncelleniyor = true; // Şalteri kapat (Kaydetme işlemi tetiklenmesin)
        favoriToggle.isOn = PlayerPrefs.GetInt("FAV_" + dosyaAdi, 0) == 1;
        arayuzGuncelleniyor = false; // Şalteri geri aç

        baslaButonu.interactable = true;
    }

    void FavoriDurumunuKaydet()
    {
        // Eğer işlemi kod yapıyorsa veya metin seçilmemişse iptal et!
        if (arayuzGuncelleniyor || seciliDosyaAdi == "") return;

        int favDegeri = favoriToggle.isOn ? 1 : 0;
        PlayerPrefs.SetInt("FAV_" + seciliDosyaAdi, favDegeri);

        // Sahneyi resetlemek yerine sadece listeyi baştan çizdiriyoruz
        ListeleMetinleri();
    }

    // --- YENİ EKLENEN 1: RASTGELE METİN SEÇ ---
    public void RastgeleSec()
    {
        if (tumDosyalar != null && tumDosyalar.Length > 0)
        {
            int rastgeleIndex = Random.Range(0, tumDosyalar.Length);
            MetinSecildi(tumDosyalar[rastgeleIndex], Path.GetFileNameWithoutExtension(tumDosyalar[rastgeleIndex]));
        }
    }

    // --- YENİ EKLENEN 2: TÜM METNİ GÖSTER ---
    public void TumMetniGoster()
    {
        if (suAnkiTamMetin != "")
        {
            onizlemeText.text = suAnkiTamMetin;
        }
    }

    public void SinavaBasla()
    {
        PlayerPrefs.SetString("SecilenMetinYolu", seciliDosyaYolu);
        SceneManager.LoadScene("KatiplikMode");
    }

    public void GeriDon() { SceneManager.LoadScene("AnaMenu"); }
}