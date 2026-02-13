using UnityEngine;

public class LegoBlock : MonoBehaviour
{
    private new Renderer renderer;

    [HideInInspector]public Material ActualLegoMaterial;
    [HideInInspector]public Material HoveringLegoMaterial;
    public LegoEnum EnumLego;

    public void Awake()
    {
        renderer = GetComponent<Renderer>();
        ActualLegoMaterial = renderer.material;
        ActualLegoMaterial.color = GameManager.Instance.colorSelected;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }

    public void SetMaterial(Material material)
    {
        ActualLegoMaterial = material;
        renderer.material = ActualLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }

    public void SetHoveringMaterial(Material material)
    {
        HoveringLegoMaterial = material;
        renderer.material = HoveringLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = HoveringLegoMaterial;
        }
    }

    public void ResetHoveringMaterial()
    {
        renderer.material = ActualLegoMaterial;
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Renderer>().material = ActualLegoMaterial;
        }
    }
}