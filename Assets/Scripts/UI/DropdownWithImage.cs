using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DropdownWithImage : BaseField<string>
{
    [UxmlAttribute]
    public Texture2D arrowIcon
    {
        get => arrow.image as Texture2D;
        set => arrow.image = value;
    }

    [UxmlAttribute]
    public ColorAvailable ColorAvailableObject;
    private Image arrow;
    private VisualElement popup;
    private Label selectedIconLabel;
    private Image selectedIconTexture;
    private bool isOpen;
    
    public DropdownWithImage() : this(null)
    {
        
    }

    private void ClosePopup()
    {
        popup.style.display = DisplayStyle.None;
        isOpen = false;
    }

    private void SelectItem(ColorAvailable.Item item)
    {
        value = item.label;
        selectedIconLabel.text = item.label;
        selectedIconTexture.image = item.icon;
        ClosePopup();
    }

    private void OpenPopup()
    {
        popup.Clear();

        foreach(var item in ColorAvailableObject.items)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;

            var icon = new Image {style = {width = 20, height = 20, marginRight = 8}};
            if(item.icon != null)
            {
                icon.image = item.icon;
            }

            var label = new Label(item.label);

            row.Add(icon);
            row.Add(label);

            //Background color des items quand ils sont survolés
            row.RegisterCallback<PointerEnterEvent>(evt => row.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f)));
            row.RegisterCallback<PointerLeaveEvent>(evt => row.style.backgroundColor = StyleKeyword.None);

            var captured = item;
            row.RegisterCallback<ClickEvent>(evt => SelectItem(captured));
            popup.Add(row);
        }

        panel.visualTree.Add(popup);
        isOpen = true;
    }

    private void TogglePopup()
    {
        if(isOpen) 
        {
            ClosePopup();
        }
        else
        {
            OpenPopup();
        }

    }

    private void OnPointerDownOutside(PointerDownEvent evt)
    {
        if (isOpen && !worldBound.Contains(evt.position) && !popup.worldBound.Contains(evt.position))
            ClosePopup();
    }

    public DropdownWithImage(string label) : base(label, new VisualElement())
    {
        AddToClassList("image-dropdown-field");
        var display = this.Q<VisualElement>(className: "unity-base-field__input");
        display.style.flexDirection = FlexDirection.Row;
        display.style.alignItems = Align.Center;
        
        display.style.paddingLeft = 6;
        display.style.paddingRight = 6;

        selectedIconLabel = new Label("Red") {style = {flexGrow = 1}};
        selectedIconTexture = new Image {style = {width = 20, height = 20, marginRight = 6}};
        arrow = new Image();
        arrow.style.width = 32;
        arrow.style.height = 32;

        display.Add(selectedIconLabel);
        display.Add(selectedIconTexture);
        display.Add(arrow);

        display.RegisterCallback<ClickEvent>(evt => TogglePopup());

        popup = new VisualElement();
        popup.style.position = Position.Absolute;
        //Background color du dropdown
        popup.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
        popup.style.borderTopLeftRadius = 4;
        popup.style.borderTopRightRadius = 4;
        popup.style.borderBottomLeftRadius = 4;
        popup.style.borderBottomRightRadius = 4;
        popup.style.minWidth = 160;
        

        RegisterCallback<AttachToPanelEvent>(evt => panel.visualTree.RegisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown));
        RegisterCallback<DetachFromPanelEvent>(evt => panel?.visualTree.UnregisterCallback<PointerDownEvent>(OnPointerDownOutside, TrickleDown.TrickleDown));
    }
}