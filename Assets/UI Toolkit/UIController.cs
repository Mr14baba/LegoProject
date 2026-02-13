using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private SerializableList<LegoData> SerializableLegoList;
    private Dictionary<string, Color> predefinedColorList = new Dictionary<string, Color>();
    private string fileToLoad;
    List<string> itemList = new();
    private readonly string sceneFolderPath  = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\LegoScenes";
    void Start()
    {
        Button colorSwitchButton = uiDocument.rootVisualElement.Q<Button>("ColorSwitchButton");
        Button exportButton = uiDocument.rootVisualElement.Q<Button>("ExportButton");
        TextField exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField");
        Button exportWindowExportButton = uiDocument.rootVisualElement.Q<Button>("ExportExportButton");
        Button exportWindowCancelButton = uiDocument.rootVisualElement.Q<Button>("ExportCancelButton");

        Button importButton = uiDocument.rootVisualElement.Q<Button>("ImportButton");
        Button importWindowImportButton = uiDocument.rootVisualElement.Q<Button>("ImportImportButton");
        Button importWindowRefreshButton = uiDocument.rootVisualElement.Q<Button>("ImportRefreshButton");
        Button importWindowCancelButton = uiDocument.rootVisualElement.Q<Button>("ImportCancelButton");
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("ListTest");

        DropdownField colorSelectorDropDownField = uiDocument.rootVisualElement.Q<DropdownField>("ColorSelector");

        DropdownField legoSelectorDropDownField = uiDocument.rootVisualElement.Q<DropdownField>("LegoSelector");

        colorSwitchButton.clicked += OnColorSwitchButtonClicked;

        exportButton.clicked += OpenExportWindow;
        exportTextField.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocusGained());
        exportTextField.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocusLost());
        exportWindowCancelButton.clicked += CloseExportWindow;
        exportWindowExportButton.clicked += ExportScene;

        importButton.clicked += OpenImportWindow;
        importWindowCancelButton.clicked += CloseImportWindow;
        importWindowImportButton.clicked += ImportScene;
        importWindowRefreshButton.clicked += RefreshImportFiles;
        sceneToImportListView.selectionChanged += (fileSelected) => fileToLoad = fileSelected.First().ToSafeString();

        colorSelectorDropDownField.RegisterValueChangedCallback(evt => OnColorSwitched(evt.newValue));
        
        legoSelectorDropDownField.RegisterValueChangedCallback(evt => OnLegoSelected(legoSelectorDropDownField.index));

        //Assign all predefined colors of Unity to our list of colors.
        foreach (var color in typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            //Debug.Log(color.Name);
            predefinedColorList.Add(color.Name, (Color)color.GetValue(null));
            colorSelectorDropDownField.choices.Add(color.Name);
        }
        //This is the color automatically selected at the start of the software.
        colorSelectorDropDownField.value = "red";

        foreach (GameObject lego in GameManager.Instance.usableLegoList)
        {
            legoSelectorDropDownField.choices.Add(lego.name);
        }

        legoSelectorDropDownField.index = 0;

        CloseExportWindow();
        CloseImportWindow();
        OnColorSwitched(colorSelectorDropDownField.value);
    }

    private void OnColorSwitchButtonClicked()
    {
        GameManager.Instance.paintModeEnabled = !GameManager.Instance.paintModeEnabled;
        PaintModeModified();
    }

    private void OnColorSwitched(string colorName)
    {
        Image colorPreviewImage = uiDocument.rootVisualElement.Q<Image>("ImageColorPreview");
        GameManager.Instance.colorSelected = predefinedColorList[colorName];
        colorPreviewImage.style.backgroundColor = predefinedColorList[colorName];
    }

    public void OnLegoSelected(int index)
    {
        DropdownField legoSelectorDropDownField = uiDocument.rootVisualElement.Q<DropdownField>("LegoSelector");
        GameManager.Instance.legoSelected = index;
        legoSelectorDropDownField.index = index;
    }

    public void PaintModeModified()
    {
        Button colorSwitchButton = uiDocument.rootVisualElement.Q<Button>("ColorSwitchButton");
        Color newColor;
        if (GameManager.Instance.paintModeEnabled)
        {
            newColor = Color.softGreen;
        }
        else
        {
            newColor = Color.softRed;
        }
        colorSwitchButton.style.backgroundColor = newColor;  
    }
    private string SerializeLego()
    {
        //Serialization of the lego pieces
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

    private void OpenExportWindow()
    {
        VisualElement ExportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ExportSceneWindow");
        ExportSceneWindow.visible = true;
    }

    private void CloseExportWindow()
    {
        VisualElement ExportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ExportSceneWindow");
        ExportSceneWindow.visible = false;
    }

    private void OpenImportWindow()
    {
        VisualElement ImportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ImportSceneWindow");
        ImportSceneWindow.visible = true;
        RefreshImportFiles();
    }

    private void RefreshImportFiles()
    {
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("ListTest");

        itemList = Directory.GetFiles(sceneFolderPath, "*.json").ToList();

        Func<VisualElement> makeItem = () => new Label();
        //We select last backslash to have the .json file and we split the .json part of the name.
        Action<VisualElement, int> bindItem = (e, i) => ((Label)e).text = itemList[i].Substring(itemList[i].LastIndexOf("\\") + 1).Split(".")[0];

        sceneToImportListView.makeItem = makeItem;
        sceneToImportListView.bindItem = bindItem;
        sceneToImportListView.itemsSource = itemList;
        fileToLoad = null;
    }

    private void CloseImportWindow()
    {
        VisualElement ImportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ImportSceneWindow");
        ImportSceneWindow.visible = false;
        fileToLoad = null;
    }

    private void OnTextFieldFocusGained()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        playerController.controls.Disable();
    }

    private void OnTextFieldFocusLost()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        playerController.controls.Enable();
    }

    private void ExportScene()
    {
        TextField exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField");

        if(!Directory.Exists(sceneFolderPath))
        {
            Directory.CreateDirectory(sceneFolderPath);
        }

        string fileName = exportTextField.value;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "newScene";
        }
        File.WriteAllText(sceneFolderPath + "\\" + fileName +".json", SerializeLego());
        CloseExportWindow();
    }

    private void ImportScene()
    {
        if(fileToLoad != null)
        {
            string SerializedLegoList = File.ReadAllText(fileToLoad);
            SerializedLegoList = SerializedLegoList.Replace(Environment.NewLine, "");
            SerializableList<LegoData> legoList = JsonUtility.FromJson<SerializableList<LegoData>>(SerializedLegoList);
            GameManager.Instance.InstantiateSceneCoroutine = StartCoroutine(GameManager.Instance.InstantiateScene(legoList.list));
            fileToLoad = null;
            CloseImportWindow();
        }
    }
}
