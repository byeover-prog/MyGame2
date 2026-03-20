using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 요소를 이름으로 검색하고, 없으면 자동 생성하는 유틸리티입니다.
/// OutgameUpgradeToolkitPresenter2D와 FormationToolkitPresenter2D에서 공통 사용합니다.
/// </summary>
public static class MetaUiToolkitUtil2D
{
    /// <summary>
    /// 이름으로 VisualElement를 검색합니다. 없으면 새로 만들어 root에 추가합니다.
    /// </summary>
    public static VisualElement QueryOrCreate(VisualElement root, string elementName, string fallbackClass = null)
    {
        if (root == null) return new VisualElement();

        VisualElement found = root.Q<VisualElement>(elementName);
        if (found != null) return found;

        found = new VisualElement { name = elementName };
        if (!string.IsNullOrWhiteSpace(fallbackClass))
            found.AddToClassList(fallbackClass);
        root.Add(found);
        return found;
    }

    /// <summary>
    /// 이름으로 ScrollView를 검색합니다. 없으면 새로 만들어 root에 추가합니다.
    /// </summary>
    public static ScrollView QueryOrCreateScrollView(VisualElement root, string elementName, string fallbackClass = null)
    {
        if (root == null) return new ScrollView();

        ScrollView found = root.Q<ScrollView>(elementName);
        if (found != null) return found;

        found = new ScrollView { name = elementName };
        if (!string.IsNullOrWhiteSpace(fallbackClass))
            found.AddToClassList(fallbackClass);
        root.Add(found);
        return found;
    }

    /// <summary>
    /// 이름으로 Label을 검색합니다. 없으면 새로 만들어 root에 추가합니다.
    /// </summary>
    public static Label QueryOrCreateLabel(VisualElement root, string elementName, string defaultText)
    {
        if (root == null) return new Label(defaultText);

        Label found = root.Q<Label>(elementName);
        if (found != null) return found;

        found = new Label(defaultText) { name = elementName };
        root.Add(found);
        return found;
    }

    /// <summary>
    /// 이름으로 Button을 검색합니다. 없으면 새로 만들어 root에 추가합니다.
    /// </summary>
    public static Button QueryOrCreateButton(VisualElement root, string elementName, string defaultText)
    {
        if (root == null) return new Button { text = defaultText };

        Button found = root.Q<Button>(elementName);
        if (found != null) return found;

        found = new Button { name = elementName, text = defaultText };
        root.Add(found);
        return found;
    }

    /// <summary>
    /// VisualElement의 배경에 Sprite를 설정합니다.
    /// null이면 배경을 지웁니다.
    /// </summary>
    public static void SetSpriteBackground(VisualElement element, Sprite sprite)
    {
        if (element == null) return;

        if (sprite != null)
        {
            element.style.backgroundImage = new StyleBackground(sprite);
        }
        else
        {
            element.style.backgroundImage = StyleKeyword.None;
        }
    }
}