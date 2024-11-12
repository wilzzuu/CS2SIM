using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
    public void LoadInventoryScene()
    {
        SceneManager.LoadScene(1);
    }

    public void LoadCollectionScene()
    {
        SceneManager.LoadScene(2);
    }

    public void LoadMarketScene()
    {
        SceneManager.LoadScene(3);
    }

    public void LoadTradingScene()
    {
        SceneManager.LoadScene(4);
    }

    public void LoadCaseOpeningScene()
    {
        SceneManager.LoadScene(5);
    }

    public void LoadContractsScene()
    {
        SceneManager.LoadScene(6);
    }

    public void LoadCoinFlipScene()
    {
        SceneManager.LoadScene(7);
    }

    public void LoadRouletteScene()
    {
        SceneManager.LoadScene(8);
    }

    public void LoadUpgraderScene()
    {
        SceneManager.LoadScene(9);
    }

    public void LoadCrashScene()
    {
        SceneManager.LoadScene(10);
    }

    public void LoadPlinkoScene()
    {
        SceneManager.LoadScene(11);
    }

    public void LoadClickerScene()
    {
        SceneManager.LoadScene(12);
    }

    public void LoadHighLowScene()
    {
        SceneManager.LoadScene(13);
    }

    public void LoadTowerScene()
    {
        SceneManager.LoadScene(14);
    }

    public void LoadSettingsScene()
    {
        SceneManager.LoadScene(15);
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
