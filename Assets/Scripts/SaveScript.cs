using System;
using System.IO;
using UnityEngine;

public class SaveScript : MonoBehaviour
{
    public static SaveScript Instance {get; private set;}
    
    //Location of save files
    public readonly string sceneFolderPath  = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\LegoScenes";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private string SerializeLego(SerializableList<LegoData> SerializableLegoList)
    {
        //Serialization of the lego pieces for exportation
        SerializableLegoList = new();
        foreach(LegoEnum key in GameManager.Instance.dictTypeOfLegoPlaced.Keys)
        {
            foreach(GameObject legoToSerialize in GameManager.Instance.dictTypeOfLegoPlaced[key])
            {
                if(legoToSerialize != null)
                {
                    LegoData legoData = new()
                    {
                        name = legoToSerialize.name,
                        position = legoToSerialize.transform.position,
                        rotation = legoToSerialize.transform.rotation,
                        color = legoToSerialize.GetComponent<LegoBlock>().ActualLegoMaterial.color,
                        legoEnum = legoToSerialize.GetComponent<LegoBlock>().EnumLego,
                        //prefabName = legoToSerialize.GetComponent<MeshFilter>().sharedMesh.name,
                        parent = legoToSerialize.transform.parent?.parent.name + "|" + legoToSerialize.transform.parent?.name
                    };
                    SerializableLegoList.list.Add(legoData);
                }
            }
        }
        //Writing of the .json file
        string SerializedLegoList = JsonUtility.ToJson(SerializableLegoList);
        SerializedLegoList = SerializedLegoList.Replace("{\"name\"",Environment.NewLine + "{\"name\"");
        SerializedLegoList = SerializedLegoList.Replace("]",Environment.NewLine + "]");
        return SerializedLegoList;
    }

    public void ExportScene(SerializableList<LegoData> serializableLegoList)
    {
        if(!Directory.Exists(sceneFolderPath))
        {
            Directory.CreateDirectory(sceneFolderPath);
        }

        if (string.IsNullOrWhiteSpace(GameManager.Instance.actualFileName))
        {
            GameManager.Instance.actualFileName = "newScene";
        }
        File.WriteAllText(sceneFolderPath + "\\" + GameManager.Instance.actualFileName +".json", SerializeLego(serializableLegoList));
    }

}
