using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LegoAvailable", menuName = "Scriptable Objects/LegoAvailable")]
public class UILegoAvailable : ScriptableObject
{
    [Serializable]
    public class LegoUI
    {
        public string label;
        public Texture2D icon;
        public LegoEnum lego;
    }

    public List<LegoUI> items;
}
