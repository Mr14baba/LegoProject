using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public Dictionary<LegoEnum, List<GameObject>> dictTypeOfLegoPlaced = new();
    [HideInInspector] public Color colorSelected;
    [HideInInspector] public bool paintModeEnabled;
    [HideInInspector] public int legoSelected;
    [HideInInspector] public Coroutine InstantiateSceneCoroutine;
    public static GameManager Instance { get; private set;}
    public Material addHoveringMaterial;
    public Material removeHoveringMaterial;
    public GameObject[] usableLegoList;    
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
    public void AddNewLego(GameObject newLego, bool addNumbering = true)
    {
        //Rename Lego
        newLego.name = newLego.name.Replace("(Clone)", "");
        if (!dictTypeOfLegoPlaced.ContainsKey(newLego.GetComponent<LegoBlock>().EnumLego))
        {
            dictTypeOfLegoPlaced.Add(newLego.GetComponent<LegoBlock>().EnumLego, new List<GameObject>());
        }

        //Add Lego to list of lego placed
        List<GameObject> currentLegoList = dictTypeOfLegoPlaced[newLego.GetComponent<LegoBlock>().EnumLego];

        
        if (currentLegoList.Contains(null))
        {
            currentLegoList[currentLegoList.IndexOf(null)] = newLego;
        }
        else
        {
            currentLegoList.Add(newLego);
        }
        
        if (addNumbering)
        {
            newLego.name += "_" + currentLegoList.IndexOf(newLego);
            //Debug.Log(currentLegoList.Count);
        }
    }

    //get lego from the current lego list and removes it from list and scene
    public void RemoveLego(GameObject legoToRemove)
    {
        List<GameObject> currentLegoList = dictTypeOfLegoPlaced[legoToRemove.GetComponent<LegoBlock>().EnumLego];
        currentLegoList[currentLegoList.IndexOf(legoToRemove)] = null;
        Destroy(legoToRemove);
        while(currentLegoList.Count > 0 && currentLegoList[^1] == null)
        {
            Debug.Log(currentLegoList[^1]);
            currentLegoList.Remove(currentLegoList[^1]);
        }
    }

    public IEnumerator InstantiateScene(List<LegoData> legoDataList)
    {
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
            GameObject newLego = Instantiate(usableLegoList[(int)legoData.legoEnum]);
            newLego.name = legoData.name;
            newLego.transform.position = legoData.position;
            newLego.transform.rotation = legoData.rotation;
            newLego.GetComponent<Renderer>().material.color = legoData.color;
            newLego.GetComponent<Collider>().enabled = true;
            for(int i = 0; i < newLego.transform.childCount; i++)
            {
                newLego.transform.GetChild(i).GetComponent<Collider>().enabled = true;
            }
            AddNewLego(newLego, false);
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
