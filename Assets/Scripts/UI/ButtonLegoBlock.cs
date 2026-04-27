using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LegoBlockButton : Button
{

    private UILegoAvailable _legoAvailable;
    private int _legoIndex;

    [UxmlAttribute]
    public UILegoAvailable legoAvailable
    {
        get => _legoAvailable;
        set 
        {
            _legoAvailable = value;
            UpdateBackground();
        }
    }

    [UxmlAttribute]
    public int legoIndex
    {
        get => _legoIndex;
        set 
        {
            _legoIndex = value;
            UpdateBackground();
        }
    }

    public LegoBlockButton() : base()
    {
        AddToClassList("unity-lego-button");
    }

    private void UpdateBackground()
    {
        if (legoAvailable == null || legoIndex < 0 || legoIndex > legoAvailable.items.Count)
        {
            return;
        }
        else
        {
            iconImage = _legoAvailable.items[_legoIndex].icon;
        }
    }
}


