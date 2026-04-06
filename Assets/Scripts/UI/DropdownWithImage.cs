using UnityEngine.UIElements;

[UxmlElement]
public partial class DropdownWithImage : BaseField<string>
{
    public DropdownWithImage() : this(null)
    {
        
    }

    public DropdownWithImage(string label) : base(label, new VisualElement())
    {
        
    }

}