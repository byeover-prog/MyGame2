using UnityEngine;
using UnityEngine.UIElements;

public static class MetaUiToolkitUtil2D
{
    public static VisualElement QueryOrCreate(VisualElement root, string name, string className = null)
    {
        if (root == null) return null;

        VisualElement found = root.Q<VisualElement>(name);
        if (found != null) return found;

        found = new VisualElement { name = name };
        if (!string.IsNullOrWhiteSpace(className))
            found.AddToClassList(className);

        root.Add(found);
        return found;
    }

    public static ScrollView QueryOrCreateScrollView(VisualElement root, string name, string className = null)
    {
        if (root == null) return null;

        ScrollView found = root.Q<ScrollView>(name);
        if (found != null) return found;

        found = new ScrollView { name = name };
        if (!string.IsNullOrWhiteSpace(className))
            found.AddToClassList(className);

        root.Add(found);
        return found;
    }

    public static Label QueryOrCreateLabel(VisualElement root, string name, string defaultText = "")
    {
        if (root == null) return null;

        Label found = root.Q<Label>(name);
        if (found != null) return found;

        found = new Label(defaultText) { name = name };
        root.Add(found);
        return found;
    }

    public static Button QueryOrCreateButton(VisualElement root, string name, string text)
    {
        if (root == null) return null;

        Button found = root.Q<Button>(name);
        if (found != null) return found;

        found = new Button { name = name, text = text };
        root.Add(found);
        return found;
    }

    public static void SetSpriteBackground(VisualElement target, Sprite sprite)
    {
        if (target == null) return;

        if (sprite == null)
        {
            target.style.backgroundImage = StyleKeyword.None;
            return;
        }

        // 배경 정렬/크기 지정은 USS에서 처리하고, 여기서는 스프라이트만 연결합니다.
        target.style.backgroundImage = new StyleBackground(sprite);
    }
}
