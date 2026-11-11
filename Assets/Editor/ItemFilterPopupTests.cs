using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ItemFilterPopupTests
{
    [Test]
    public void ResourcesPopupUsesEditableFilterPanelAndGeneratesOptions()
    {
        GameObject panel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FilterPanel.prefab");
        GameObject resource = Resources.Load<GameObject>("ItemFilterPopup");
        Assert.That(panel, Is.Not.Null);
        Assert.That(resource, Is.Not.Null);
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(resource), Is.EqualTo(panel));
        Assert.That(panel.GetComponentInChildren<ScrollRect>(true), Is.Not.Null);
        Assert.That(panel.transform.Find("Option template"), Is.Not.Null);

        GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        ItemData first = ScriptableObject.CreateInstance<ItemData>();
        ItemData second = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            first.condition = "Factory New";
            first.gun = "AK-47";
            first.rarity = "Covert";
            first.type = "Rifle";
            second.condition = "Minimal Wear";
            second.gun = "AWP";
            second.rarity = "Restricted";
            second.type = "Rifle";
            ItemFilter filter = new ItemFilter();
            filter.Guns.Add("AK-47");
            int changes = 0;
            ItemFilterPopup.Open(canvas.transform, filter, new[] { first, second }, () => changes++);
            ItemFilterPopup popup = canvas.GetComponentInChildren<ItemFilterPopup>();
            Assert.That(popup, Is.Not.Null);
            Toggle[] options = popup.GetComponentsInChildren<Toggle>(true);
            Toggle ak = options.Single(option => option.name == "AK-47");
            Toggle awp = options.Single(option => option.name == "AWP");
            Assert.That(ak.isOn, Is.True);
            Assert.That(ak.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("AK-47"));
            awp.isOn = true;
            options.Single(option => option.name == "StatTrak").isOn = true;
            Assert.That(filter.Guns, Does.Contain("AWP"));
            Assert.That(filter.StatTrak, Does.Contain(true));
            Assert.That(changes, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void OptionsUseBestToWorstOrderAndKeepUnknownSelections()
    {
        GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        List<ItemData> items = new List<ItemData>();
        try
        {
            string[] conditions = { "Battle-Scarred", "Field-Tested", "Factory New", "Well-Worn", "Minimal Wear", "Z condition", "A condition" };
            string[] rarities = { "Restricted", "Special", "Mil-Spec", "Covert", "Classified", "Z rarity", "A rarity" };
            for (int i = 0; i < conditions.Length; i++)
            {
                ItemData item = ScriptableObject.CreateInstance<ItemData>();
                item.condition = conditions[i];
                item.rarity = rarities[i];
                item.type = i % 2 == 0 ? "Rifle" : "Pistol";
                item.gun = i % 2 == 0 ? "AWP" : "AK-47";
                items.Add(item);
            }

            ItemFilter filter = new ItemFilter();
            filter.Conditions.Add("Saved condition");
            filter.Rarities.Add("Saved rarity");
            ItemFilterPopup.Open(canvas.transform, filter, items, () => { });
            ItemFilterPopup popup = canvas.GetComponentInChildren<ItemFilterPopup>();
            Assert.That(popup, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(popup);

            CollectionAssert.AreEqual(
                new[] { "Factory New", "Minimal Wear", "Field-Tested", "Well-Worn", "Battle-Scarred", "A condition", "Saved condition", "Z condition" },
                ChildNames(serialized.FindProperty("conditions")));
            CollectionAssert.AreEqual(
                new[] { "Special", "Covert", "Classified", "Restricted", "Mil-Spec", "A rarity", "Saved rarity", "Z rarity" },
                ChildNames(serialized.FindProperty("rarities")));
            CollectionAssert.AreEqual(new[] { "Pistol", "Rifle" }, ChildNames(serialized.FindProperty("types")));
            CollectionAssert.AreEqual(new[] { "AK-47", "AWP" }, ChildNames(serialized.FindProperty("guns")));
            Assert.That(popup.GetComponentsInChildren<Toggle>(true).Single(toggle => toggle.name == "Saved condition").isOn, Is.True);
            Assert.That(popup.GetComponentsInChildren<Toggle>(true).Single(toggle => toggle.name == "Saved rarity").isOn, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvas);
            foreach (ItemData item in items) Object.DestroyImmediate(item);
        }
    }

    private static string[] ChildNames(SerializedProperty property)
    {
        Transform parent = (Transform)property.objectReferenceValue;
        return parent.Cast<Transform>().Select(child => child.name).ToArray();
    }

    [Test]
    public void PopupOptionsScrollWhenThereAreMoreItemsThanFit()
    {
        GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        List<ItemData> items = new List<ItemData>();
        try
        {
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(800, 600);
            for (int i = 0; i < 80; i++)
            {
                ItemData item = ScriptableObject.CreateInstance<ItemData>();
                item.gun = "Gun " + i;
                items.Add(item);
            }

            ItemFilterPopup.Open(canvas.transform, new ItemFilter(), items, () => { });
            ScrollRect scroll = canvas.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(scroll.scrollSensitivity, Is.EqualTo(5f));
            scroll.verticalNormalizedPosition = 1;
            scroll.OnScroll(new PointerEventData(eventSystem.GetComponent<EventSystem>())
            {
                scrollDelta = new Vector2(0, -6)
            });
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(30f).Within(0.01f));
            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(1));
        }
        finally
        {
            Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(eventSystem);
            foreach (ItemData item in items) Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void PageWheelIsBlockedOnlyWhilePopupIsActiveInSameScene()
    {
        Scene firstScene = EditorSceneManager.NewPreviewScene();
        Scene otherScene = EditorSceneManager.NewPreviewScene();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            GameObject pagerObject = new GameObject("Pager");
            SceneManager.MoveGameObjectToScene(pagerObject, firstScene);
            ScrollPageButtons pager = pagerObject.AddComponent<ScrollPageButtons>();
            Button next = new GameObject("Next", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            next.transform.SetParent(pagerObject.transform);
            Button previous = new GameObject("Previous", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            previous.transform.SetParent(pagerObject.transform);
            typeof(ScrollPageButtons).GetField("nextButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pager, next);
            typeof(ScrollPageButtons).GetField("prevButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pager, previous);
            int pageChanges = 0;
            next.onClick.AddListener(() => pageChanges++);
            previous.onClick.AddListener(() => pageChanges++);

            GameObject popupObject = new GameObject("Popup");
            SceneManager.MoveGameObjectToScene(popupObject, otherScene);
            ItemFilterPopup popup = popupObject.AddComponent<ItemFilterPopup>();
            Invoke(popup, "OnEnable");
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) });
            InputSystem.Update();
            Invoke(pager, "Update");
            Assert.That(pageChanges, Is.EqualTo(1));

            typeof(ScrollPageButtons).GetField("_lastScrollTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pager, -1f);
            Invoke(popup, "OnDisable");
            SceneManager.MoveGameObjectToScene(popupObject, firstScene);
            Invoke(popup, "OnEnable");
            Invoke(pager, "Update");
            Assert.That(pageChanges, Is.EqualTo(1));

            Invoke(popup, "OnDisable");
            Invoke(pager, "Update");
            Assert.That(pageChanges, Is.EqualTo(2));
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            EditorSceneManager.ClosePreviewScene(otherScene);
            EditorSceneManager.ClosePreviewScene(firstScene);
        }
    }

    [UnityTest]
    public IEnumerator PopupLifecycleBlocksAndRestoresPageWheelInPlayMode()
    {
        yield return new EnterPlayMode();
        GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject pagerObject = new GameObject("Pager");
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            ScrollPageButtons pager = pagerObject.AddComponent<ScrollPageButtons>();
            Button next = new GameObject("Next", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            next.transform.SetParent(pagerObject.transform);
            Button previous = new GameObject("Previous", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            previous.transform.SetParent(pagerObject.transform);
            typeof(ScrollPageButtons).GetField("nextButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pager, next);
            typeof(ScrollPageButtons).GetField("prevButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pager, previous);
            int pageChanges = 0;
            next.onClick.AddListener(() => pageChanges++);
            previous.onClick.AddListener(() => pageChanges++);

            ItemFilterPopup.Open(canvas.transform, new ItemFilter(), Array.Empty<ItemData>(), () => { });
            ItemFilterPopup popup = canvas.GetComponentInChildren<ItemFilterPopup>();
            Assert.That(popup, Is.Not.Null);
            Assert.That(ItemFilterPopup.IsOpenInScene(pagerObject.scene), Is.True);
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) });
            InputSystem.Update();
            Invoke(pager, "Update");
            Assert.That(pageChanges, Is.Zero);

            popup.GetComponentsInChildren<Button>().Single(button => button.name == "Close").onClick.Invoke();
            Assert.That(ItemFilterPopup.IsOpenInScene(pagerObject.scene), Is.False);
            Invoke(pager, "Update");
            Assert.That(pageChanges, Is.EqualTo(1));
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            Object.Destroy(canvas);
            Object.Destroy(pagerObject);
        }
        yield return new ExitPlayMode();
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
