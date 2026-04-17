using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImportScript : MonoBehaviour
{
    public static ImportScript Instance {get; private set;}
    [HideInInspector] public Coroutine InstantiateSceneCoroutine;

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
    

    public void ImportScene(string fileToLoad)
    {
        string SerializedLegoList = File.ReadAllText(fileToLoad);
        SerializedLegoList = SerializedLegoList.Replace(Environment.NewLine, "");
        SerializableList<LegoData> legoList = JsonUtility.FromJson<SerializableList<LegoData>>(SerializedLegoList);
        InstantiateSceneCoroutine = StartCoroutine(InstantiateScene(legoList.list));
        fileToLoad = null;
    }

    private IEnumerator InstantiateScene(List<LegoData> legoDataList)
    {
        var dictTypeOfLegoPlaced = GameManager.Instance.dictTypeOfLegoPlaced;

        Dictionary<GameObject, string> GoToParent = new();
        //Scene cleanup
        foreach(LegoEnum key in dictTypeOfLegoPlaced.Keys)
        {
            foreach(GameObject legoToRemove in dictTypeOfLegoPlaced[key])
            {
                Destroy(legoToRemove);
            }
        }

        yield return new WaitForEndOfFrame();        
        
        dictTypeOfLegoPlaced = new();

        //Start instantiation
        foreach (LegoData legoData in legoDataList)
        {
            GameObject newLego = Instantiate(GameManager.Instance.usableLegoList[(int)legoData.legoEnum]);
            newLego.name = legoData.name;
            newLego.transform.position = legoData.position;
            newLego.transform.rotation = legoData.rotation;
            newLego.GetComponent<Renderer>().material.color = legoData.color;
            newLego.GetComponent<Collider>().enabled = true;
            for(int i = 0; i < newLego.transform.childCount; i++)
            {
                newLego.transform.GetChild(i).GetComponent<Collider>().enabled = true;
            }
            GameManager.Instance.AddNewLego(newLego, false);
            if (legoData.parent != "|")
            {
                GoToParent.Add(newLego, legoData.parent);
            }
        }
        foreach(GameObject go in GoToParent.Keys)
        {
            go.transform.parent = GameObject.Find(GoToParent[go].Split("|")[0]).transform.Find(GoToParent[go].Split("|")[1]);
        }
    }
}
