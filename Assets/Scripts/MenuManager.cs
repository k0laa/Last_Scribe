using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Arcade moduna geçiþ
    public void ArcadeModunaGit()
    {
        // Ýlk yaptýðýmýz sahnenin adýný buraya tam ve doðru girmelisin! (Genelde SampleScene veya KatiplikMacerasi olur)
        SceneManager.LoadScene("ArcadeMode"); 
    }

    // Simülasyon moduna geçiþ
    public void SimulasyonModunaGit()
    {
        SceneManager.LoadScene("KatiplikMode");
    }

    // Oyundan çýkýþ
    public void OyundanCik()
    {
        Application.Quit();
        Debug.Log("Oyundan Çýkýldý."); // Unity editöründe çalýþýrken test edebilmek için
    }
}