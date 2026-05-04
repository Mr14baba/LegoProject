using UnityEngine;

public class LegoBlock : MonoBehaviour
{
    private Renderer legoRenderer;

    [HideInInspector]public Material ActualLegoMaterial;
    [HideInInspector]public Material HoveringLegoMaterial;
    public LegoEnum EnumLego;
    public uint id;

    public void Awake()
    {
        legoRenderer = GetComponent<Renderer>();
        ActualLegoMaterial = legoRenderer.material;
        ActualLegoMaterial.color = GameManager.Instance.colorSelected;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }

    public void Start()
    {
        if (id == 0)
        {
            id = GenerateID();
            Debug.Log(id);
        }
    }

    public void SetMaterial(Material material)
    {
        ActualLegoMaterial = material;
        legoRenderer.material = ActualLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }

    public void SetHoveringMaterial(Material material)
    {
        HoveringLegoMaterial = material;
        legoRenderer.material = HoveringLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = HoveringLegoMaterial;
        }
    }

    public void ResetHoveringMaterial()
    {
        legoRenderer.material = ActualLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }

    public uint GenerateID()
    {
        string newID = "";

        for(int i = 0; i < 8; i++)
        {
            int newNum = Random.Range(0, 9);
            newID += newNum.ToString();
        }

        return uint.Parse(newID);
    }
}