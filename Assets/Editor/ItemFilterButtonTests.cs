using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ItemFilterButtonTestScreen : MonoBehaviour, IItemFilterScreen
{
    public int OpenCount { get; private set; }

    public void OpenFilters()
    {
        OpenCount++;
    }
}

public class ItemFilterButtonTests
{
    private Scene _buttonScene;
    private Scene _otherScene;

    [SetUp]
    public void SetUp()
    {
        _buttonScene = EditorSceneManager.NewPreviewScene();
        _otherScene = EditorSceneManager.NewPreviewScene();
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.ClosePreviewScene(_otherScene);
        EditorSceneManager.ClosePreviewScene(_buttonScene);
    }

    [Test]
    public void ClickFindsActiveScreenInButtonsSceneAndIgnoresOtherScenes()
    {
        ItemFilterButtonTestScreen screen = CreateScreen(_buttonScene);
        ItemFilterButtonTestScreen otherScreen = CreateScreen(_otherScene);
        ItemFilterButtonTestScreen inactiveScreen = CreateScreen(_buttonScene);
        inactiveScreen.gameObject.SetActive(false);
        ItemFilterButton bridge = CreateButton();
        SetListenerEnabled(bridge, true);

        bridge.GetComponent<Button>().onClick.Invoke();

        Assert.That(screen.OpenCount, Is.EqualTo(1));
        Assert.That(otherScreen.OpenCount, Is.Zero);
        Assert.That(inactiveScreen.OpenCount, Is.Zero);

        SetListenerEnabled(bridge, false);
        bridge.GetComponent<Button>().onClick.Invoke();
        Assert.That(screen.OpenCount, Is.EqualTo(1));

        SetListenerEnabled(bridge, true);
        bridge.GetComponent<Button>().onClick.Invoke();
        Assert.That(screen.OpenCount, Is.EqualTo(2));
    }

    [Test]
    public void MissingScreenIsReported()
    {
        ItemFilterButton bridge = CreateButton();
        CreateScreen(_otherScene);
        LogAssert.Expect(LogType.Error, "Cannot open item filters: no active filter screen exists in this scene.");
        bridge.OpenFilters();
    }

    [Test]
    public void MultipleActiveScreensAreReportedWithoutOpeningEither()
    {
        ItemFilterButtonTestScreen first = CreateScreen(_buttonScene);
        ItemFilterButtonTestScreen second = CreateScreen(_buttonScene);
        ItemFilterButton bridge = CreateButton();
        LogAssert.Expect(LogType.Error, "Cannot open item filters: more than one active filter screen exists in this scene.");
        bridge.OpenFilters();
        Assert.That(first.OpenCount, Is.Zero);
        Assert.That(second.OpenCount, Is.Zero);
    }

    private ItemFilterButton CreateButton()
    {
        GameObject button = new GameObject("Filter Button", typeof(RectTransform), typeof(Button));
        SceneManager.MoveGameObjectToScene(button, _buttonScene);
        return button.AddComponent<ItemFilterButton>();
    }

    private static ItemFilterButtonTestScreen CreateScreen(Scene scene)
    {
        GameObject screen = new GameObject("Screen");
        SceneManager.MoveGameObjectToScene(screen, scene);
        return screen.AddComponent<ItemFilterButtonTestScreen>();
    }

    private static void SetListenerEnabled(ItemFilterButton button, bool enabled)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo disable = typeof(ItemFilterButton).GetMethod("OnDisable", flags);
        disable.Invoke(button, null);
        if (enabled)
        {
            typeof(ItemFilterButton).GetMethod("OnEnable", flags).Invoke(button, null);
        }
    }
}
