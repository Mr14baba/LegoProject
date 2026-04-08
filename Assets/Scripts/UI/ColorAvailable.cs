using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorAvailable", menuName = "Scriptable Objects/ColorAvailable")]
public class ColorAvailable : ScriptableObject
{
    [Serializable]
    public class ColorUI
    {
        public string label;
        public Texture2D icon;
        public Color color;
    }

    public List<ColorUI> items;
}
