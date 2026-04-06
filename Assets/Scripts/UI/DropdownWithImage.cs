using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class DropdownWithImage : BaseField<string>
{
    public class Item
    {
        public string iconLabel;
        public Texture2D iconTexture;
    }

    private List<Item> itemsList = new();
    private VisualElement popup;
    private Label selectedIconLabel;
    private Image selectedIconTexture;
    private bool isOpen;
    
    public DropdownWithImage() : this(null)
    {
        
    }

    public DropdownWithImage(string label) : base(label, new VisualElement())
    {
        AddToClassList("image-dropdown-field");
        var display = this.Q<VisualElement>(className: "unity-base-field__input");
        display.style.flexDirection = FlexDirection.Row;
        display.style.alignItems = Align.Center;
        display.style.paddingLeft = 6;
        display.style.paddingRight = 6;

        selectedIconLabel = new Label("Red") {style = {width = 20, height = 20, marginRight = 6}};
    }

}