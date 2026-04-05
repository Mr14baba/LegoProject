using System;
using System.Collections.Generic;
using UnityEngine;

public enum LegoEnum
{
    lego1x1Testing,
    lego1x1,
    lego1x2,
    lego2x2,
    legoPlat1x1,
    legoPlat1x2,
    legoPlat2x2,
    
}

[Serializable]
public class LegoData
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public Color color;
    public LegoEnum legoEnum;

    //can be used later to replace LegoEnum for customized lego piece
    //public string prefabName;
    public string parent;
}

[Serializable]
public class SerializableList<T>
{
    public List<T> list = new();
}