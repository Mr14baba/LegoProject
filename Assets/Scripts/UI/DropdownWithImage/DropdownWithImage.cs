using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DropdownWithImage : BaseField<string>
{

    [UxmlAttribute]
    public Texture2D arrowImage;
    public class Item
    {
        public string Label;
        public Texture2D Icon;
    }

    private List<Item> items = new();
    private VisualElement popup;
    private Image selectedIcon;
    private Label selectedLabel;
    private bool isOpen;
    

    public DropdownWithImage() : this(null) {}

    public DropdownWithImage(string label) : base(label, new VisualElement())
    {
        AddToClassList("image-dropdown-field");

        // --- Zone d'affichage de la valeur sélectionnée ---
        var display = this.Q<VisualElement>(className: "unity-base-field__input");

        selectedIcon = new Image();
        selectedIcon.AddToClassList("image-dropdown-field__selected-icon");

        selectedLabel = new Label("Select...");
        selectedLabel.AddToClassList("image-dropdown-field__selected-label");

        display.Add(selectedIcon);
        display.Add(selectedLabel);

        display.RegisterCallback<ClickEvent>(_ => TogglePopup());

        // --- Popup ---
        popup = new VisualElement();
        popup.AddToClassList("image-dropdown-field__popup");

        RegisterCallback<AttachToPanelEvent>(evt => 
        {
            panel.visualTree.RegisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown); 
        });
        RegisterCallback<DetachFromPanelEvent>(evt => panel?.visualTree.UnregisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown));
    }

    // --- API publique ---
    public void AddItem(Item item)
    {
        items.Add(item);
    }

    // --- Logique du popup ---
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
            var row = new VisualElement();
            //row.styleSheets.Add(styleSheets[0]);
            row.AddToClassList("image-dropdown-field__popup-row");

            var icon = new Image();
            icon.AddToClassList("image-dropdown-field__popup-icon");
            if (item.Icon != null) icon.image = item.Icon;

            var lbl = new Label(item.Label);
            lbl.AddToClassList("image-dropdown-field__popup-label");

            row.Add(icon);
            row.Add(lbl);

            var captured = item;
            row.RegisterCallback<ClickEvent>(_ => SelectItem(captured));

            popup.Add(row);
        }

        Add(popup);

        var display = this.Q<VisualElement>(className: "unity-base-field__input");

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

    private void SelectItem(Item item)
    {
        value = item.Label;
        selectedLabel.text = item.Label;
        selectedIcon.image = item.Icon;
        ClosePopup();
    }

    private void OnPointerDownOutside(PointerDownEvent evt)
    {
        if (isOpen && !this.worldBound.Contains(evt.position) && !popup.worldBound.Contains(evt.position))
            ClosePopup();
    }
}