using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DropdownWithImage : BaseField<int>
{
    [UxmlAttribute]
    public Texture2D arrowImage;
    public class Item
    {
        public string Label;
        public Texture2D Icon;
    }
    public List<Item> items = new();
    [HideInInspector] public Texture2D mouseHoverSprite;
    private VisualElement popup;
    private Image selectedIcon;
    private Label selectedLabel;
    private bool isOpen;
    

    public DropdownWithImage() : this(null) {}

    public DropdownWithImage(string label) : base(label, new VisualElement())
    {
        AddToClassList("image-dropdown-field");

        var display = this.Q<VisualElement>(className: "unity-base-field__input");

        selectedLabel = new Label("Selected value");
        selectedLabel.AddToClassList("image-dropdown-field__selected-label");

        selectedIcon = new Image();
        selectedIcon.AddToClassList("image-dropdown-field__selected-icon");

        display.Add(selectedLabel);
        display.Add(selectedIcon);

        display.RegisterCallback<ClickEvent>(evt => TogglePopup());

        popup = new VisualElement();
        popup.AddToClassList("image-dropdown-field__popup");

        RegisterCallback<AttachToPanelEvent>(evt => panel.visualTree.RegisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown));
        RegisterCallback<DetachFromPanelEvent>(evt => panel?.visualTree.UnregisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown));
    }

    public void AddItem(Item item)
    {
        items.Add(item);
    }

    public void SelectItem(Item item)
    {
        value = items.IndexOf(item);
        selectedLabel.text = item.Label;
        selectedIcon.image = item.Icon;
        ClosePopup();
    }

    private void TogglePopup()
    {
        if (isOpen) 
        {
            ClosePopup();
        }

        else 
        {
            OpenPopup();
        }
    }

    private void OpenPopup()
    {
        popup.Clear();

        foreach (var item in items)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("image-dropdown-field__popup-row");

            Image icon = new Image();
            icon.AddToClassList("image-dropdown-field__popup-icon");
            if (item.Icon != null) icon.image = item.Icon;

            Label lbl = new Label(item.Label);
            lbl.AddToClassList("image-dropdown-field__popup-label");

            row.Add(lbl);
            row.Add(icon);

            Item captured = item;
            row.RegisterCallback<ClickEvent>(evt => SelectItem(captured));
            row.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseHoverSprite, new(16,0), CursorMode.Auto));
            row.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));

            popup.Add(row);
        }

        Add(popup);

        VisualElement display = this.Q<VisualElement>(className: "unity-base-field__input");

        popup.style.top = display.layout.yMax;
        popup.style.left = display.layout.xMin;
        popup.style.visibility = Visibility.Visible;

        isOpen = true;
    }

    private void ClosePopup()
    {
        popup.style.visibility = Visibility.Hidden;
        isOpen = false;
    }
    private void OnPointerDownOutside(PointerDownEvent evt)
    {
        if (isOpen && !this.worldBound.Contains(evt.position) && !popup.worldBound.Contains(evt.position))
        {
            ClosePopup();
        }  
    }
}