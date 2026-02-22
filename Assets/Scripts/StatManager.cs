using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

// 1. Tekil bir oyun seansýnýn verilerini tutan þablon
[System.Serializable]
public class GameSession
{
    public string playDate;     // Ne zaman oynandý? (Örn: 2026-02-22)
    public string gameMode;     // "Arcade" mi "Katiplik" mi?
    public int wpm;             // Yazma Hýzý
    public float accuracy;      // Doðruluk Oraný
    public int totalWords;      // Toplam Vuruþ/Kelime
    public int levelReached;    // (Sadece Arcade için) Kaçýncý seviyeye geldi?
    public int netScore;        // (Sadece Katiplik için) Net kelime
}

// 2. Tüm seanslarý liste halinde tutan ana dosya yapýsý
[System.Serializable]
public class PlayerStatsData
{
    public List<GameSession> allSessions = new List<GameSession>();
}

// 3. Veritabanýný yöneten Ana Sýnýf
public static class StatManager
{
    private static string GetFilePath()
    {
        // Bilgisayarýn gizli ve güvenli uygulama verileri klasörüne kaydeder
        return Path.Combine(Application.persistentDataPath, "KatiplikGelisimVerileri.json");
    }

    // Oyundan gelen veriyi JSON'a kaydetme
    public static void SaveSession(string mode, int wpm, float accuracy, int totalWords, int levelReached = 0, int netScore = 0)
    {
        PlayerStatsData data = LoadAllData();

        GameSession newSession = new GameSession
        {
            playDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            gameMode = mode,
            wpm = wpm,
            accuracy = accuracy,
            totalWords = totalWords,
            levelReached = levelReached,
            netScore = netScore
        };

        data.allSessions.Add(newSession);

        // Veriyi JSON metnine çevir ve kaydet
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(), json);

        Debug.Log("VERÝ KAYDEDÝLDÝ: " + GetFilePath());
    }

    // JSON'dan verileri geri okuma
    public static PlayerStatsData LoadAllData()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerStatsData>(json);
        }
        return new PlayerStatsData(); // Dosya yoksa boþ liste döndür
    }
}