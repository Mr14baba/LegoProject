using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImportScript : MonoBehaviour
{
    public static ImportScript Instance {get; private set;}

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
        //Modify the string to make it in a Serializable list of LegoData we can read

        string SerializedLegoList = File.ReadAllText(fileToLoad);
        SerializedLegoList = SerializedLegoList.Replace(Environment.NewLine, "");
        SerializableList<LegoData> legoList = JsonUtility.FromJson<SerializableList<LegoData>>(SerializedLegoList);
        InstantiateScene(legoList.list);
        fileToLoad = null;
    }

    private void InstantiateScene(List<LegoData> legoDataList)
    {
        List<GameObject> newLegosList = new();

        Dictionary<GameObject, string> GoToParent = new();

        Dictionary<GameObject, Vector3> dictionaryForCoroutine = new();      

        //Start instantiation

        foreach (LegoData legoData in legoDataList)
        {
            GameObject newLego = Instantiate(GameManager.Instance.usableLegoList[(int)legoData.legoEnum]);

            //Set informations for the LegoBlock Component

            newLego.GetComponent<LegoBlock>().EnumLego = legoData.legoEnum;
            newLego.GetComponent<LegoBlock>().id = legoData.id;

            //Set Color in LegoBlock to have the hovering material when they are placed

            newLego.GetComponent<Renderer>().material.color = legoData.color;
            newLego.GetComponent<LegoBlock>().SetMaterial(newLego.GetComponent<Renderer>().material);
            newLego.GetComponent<LegoBlock>().SetHoveringMaterial(GameManager.Instance.addHoveringMaterial);

            //Set all other infos of the GameObject Lego

            newLego.name = legoData.name;
            newLego.transform.position = legoData.position;
            newLego.transform.rotation = legoData.rotation;

            //Add the lego to the list of instanciated legos, as well as other list to be used later

            GameManager.Instance.AddNewLego(newLego, false);
            newLegosList.Add(newLego);
            dictionaryForCoroutine.Add(newLego, newLego.transform.position);

            //check if Lego have parent, YES = added to list of lego with parent, NO = nothing

            if (legoData.parent != "|")
            {
                GoToParent.Add(newLego, legoData.parent);
            }
        }

        //Add a parent of the Lego
        //To find the parent, we look into a list of all new Legos to find one with the same ID specified in LegoBlock component
        //Then, we look for the name of the clip its attached to

        foreach(GameObject go in GoToParent.Keys)
        {
            go.transform.parent = newLegosList.Find(obj => obj.GetComponent<LegoBlock>().id == uint.Parse(GoToParent[go].Split("|")[0])).transform.Find(GoToParent[go].Split("|")[1]);
        }

        GameObject.FindWithTag("Player").GetComponent<PlayerController>().StartImportationCoroutine(dictionaryForCoroutine);
    }
}