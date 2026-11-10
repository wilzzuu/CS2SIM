using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
        if (sceneName == "ExitApplication")
        {
            Application.Quit();
        }

        SceneManager.LoadScene(sceneName);
    }
}

public enum SceneNameList
{
    ExitApplication,
    MainMenu,
    Settings,
    Investing,
    Notifications,
    Rewards,
    Achievements,
    Tower,
    HighLow,
    Clicker,
    Plinko,
    Crash,
    Upgrader,
    Roulette,
    CoinFlip,
    Contracts,
    CaseBattle,
    CaseOpening,
    Trading,
    GameSelector,
    Market,
    Collection,
    Inventory,
    TradeUp
}