using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorAvailable", menuName = "Scriptable Objects/ColorAvailable")]
public class ColorAvailable : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string label;
        public Texture2D icon;
    }

    public List<Item> items;
}
