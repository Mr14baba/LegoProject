using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OpenScript : MonoBehaviour
{
    public static OpenScript Instance {get; private set;}
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
    

    public void OpenScene(string fileToLoad)
    {
        //Modify the string to make it in a Serializable list of LegoData we can read

        string SerializedLegoList = File.ReadAllText(fileToLoad);
        SerializedLegoList = SerializedLegoList.Replace(Environment.NewLine, "");
        SerializableList<LegoData> legoList = JsonUtility.FromJson<SerializableList<LegoData>>(SerializedLegoList);
        InstantiateSceneCoroutine = StartCoroutine(InstantiateScene(legoList.list));
        fileToLoad = null;
    }

    private IEnumerator InstantiateScene(List<LegoData> legoDataList)
    {
        List<GameObject> newLegosList = new();

        Dictionary<GameObject, string> GoToParent = new();

        //Cleam up of the scene by removing every GameObject in the scene and creating a new dictionary of lego placed

        foreach(LegoEnum key in GameManager.Instance.dictTypeOfLegoPlaced.Keys)
        {
            foreach(GameObject legoToRemove in GameManager.Instance.dictTypeOfLegoPlaced[key])
            {
                Destroy(legoToRemove);
            }
        }
    
        GameManager.Instance.dictTypeOfLegoPlaced = new();
        
        //We wait until the end of frame to be sure that all GameObjects are sucessfully destroyed

        yield return new WaitForEndOfFrame();        

        //Start instantiation

        foreach (LegoData legoData in legoDataList)
        {
            GameObject newLego = Instantiate(GameManager.Instance.usableLegoList[(int)legoData.legoEnum]);

            //Set all the information from LegoData to the GameObject

            newLego.name = legoData.name;
            newLego.transform.position = legoData.position;
            newLego.transform.rotation = legoData.rotation;
            newLego.GetComponent<Renderer>().material.color = legoData.color;
            newLego.GetComponent<LegoBlock>().EnumLego = legoData.legoEnum;
            newLego.GetComponent<LegoBlock>().id = legoData.id;

            //Enable Collision of the GameObject and its clips

            newLego.GetComponent<Collider>().enabled = true;
            for(int i = 0; i < newLego.transform.childCount; i++)
            {
                newLego.transform.GetChild(i).GetComponent<Collider>().enabled = true;
            }

            //Add the lego to the list of instanciated legos, as well as other list to be used later

            GameManager.Instance.AddNewLego(newLego, false);
            newLegosList.Add(newLego);

            //check if Lego have parent, YES = added to list of lego with parent, NO = nothing

            if (legoData.parent != "|")
            {
                GoToParent.Add(newLego, legoData.parent);
            }
            GameManager.Instance.AddNewLego(newLego, false);
        }

        //Add a parent of the Lego
        //To find the parent, we look into a list of all new Legos to find one with the same ID specified in LegoBlock component
        //Then, we look for the name of the clip its attached to

        foreach(GameObject go in GoToParent.Keys)
        {
            go.transform.parent = newLegosList.Find(obj => obj.GetComponent<LegoBlock>().id == uint.Parse(GoToParent[go].Split("|")[0])).transform.Find(GoToParent[go].Split("|")[1]);
        }
    }
}
