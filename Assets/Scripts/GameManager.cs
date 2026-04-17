using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public Dictionary<LegoEnum, List<GameObject>> dictTypeOfLegoPlaced = new();
    [HideInInspector] public Color colorSelected;
    [HideInInspector] public bool paintModeEnabled;
    [HideInInspector] public int legoSelected;
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
}
