using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

// 1. HARF BAZLI HATA TUTUCU
[System.Serializable]
public class LetterError
{
    public string harf;
    public int hataSayisi;
}

// 2. YENÝ: KAS HAFIZASI (Hangi harf yerine neye bastý?)
[System.Serializable]
public class KeyConfusion
{
    public string expectedChar;
    public string pressedChar;
    public int count;
}

// 3. HER BÝLGÝ KIRINTISINI TUTAN DEVASA ÞABLON
[System.Serializable]
public class GameSession
{
    public int sessionID;
    public string playDate;
    public string startTime;
    public string endTime;
    public float durationSeconds;

    public string gameMode;
    public string playedTextName;
    public string quitReason;         // Öldü mü, pes mi etti?

    // --- KLAVYE ANALÝTÝÐÝ ---
    public int totalKeystrokes;
    public int correctKeystrokes;
    public int wrongKeystrokes;
    public int backspaceCount;
    public int maxCombo;

    // --- YENÝ: PSÝKOLOJÝK VE REFLEKS ANALÝTÝÐÝ ---
    public float timeToFirstKeystroke;// Ekrana kelime geldikten sonra ilk tuþa basma süresi (Refleks hýzý)
    public float longestIdleTime;     // Hiçbir tuþa basmadan ekrana baktýðý maksimum süre (Kilitlenme süresi)
    public float averageTimeBetweenKeys; // Tuþlar arasý ortalama basma hýzý (Saniye)
    public int averageFPS;            // Oyun sýrasýnda bilgisayar kasýyor muydu?

    public List<LetterError> detayliHarfHatalari = new List<LetterError>();
    public List<KeyConfusion> tusKaristirmalari = new List<KeyConfusion>(); // Örn: K yerine L'ye bastý
    public List<string> failedWords = new List<string>(); // Vuramadýðý (canýný yakan) kelimeler

    // --- PERFORMANS ANALÝTÝÐÝ ---
    public int grossWPM;
    public int netWPM;
    public float accuracy;

    // --- MODA ÖZEL VERÝLER ---
    public int arcadeLevelReached;
    public int arcadeWordsDefeated;

    public int katiplikAtlananKelime;
    public int katiplikBoslukHatasi;
    public bool isKatiplikPassed;
}

[System.Serializable]
public class PlayerStatsData
{
    public List<GameSession> allSessions = new List<GameSession>();
}

public static class StatManager
{
    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "KatiplikTelemetriVerileri.json");
    }

    public static void SaveSession(GameSession sessionData)
    {
        PlayerStatsData data = LoadAllData();
        sessionData.sessionID = data.allSessions.Count + 1;

        data.allSessions.Add(sessionData);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(), json);

        Debug.Log("SÜPER TELEMETRÝ KAYDEDÝLDÝ! Kayýt No: " + sessionData.sessionID + "\nDosya: " + GetFilePath());
    }

    public static PlayerStatsData LoadAllData()
    {
        string path = GetFilePath();
        if (File.Exists(path)) return JsonUtility.FromJson<PlayerStatsData>(File.ReadAllText(path));
        return new PlayerStatsData();
    }
}