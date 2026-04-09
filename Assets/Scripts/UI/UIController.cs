using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private UIColorAvailable colors;
    [SerializeField] private UILegoAvailable legos;
    [SerializeField] private SerializableList<LegoData> SerializableLegoList;
    private string fileToLoad;
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
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("SceneListView");

        List<RadioButton> ColorButtons = uiDocument.rootVisualElement.Q<VisualElement>("ColorButtonGroup").Query<RadioButton>().ToList();

        DropdownWithImage legoSelector = uiDocument.rootVisualElement.Q<DropdownWithImage>("LegoSelector");

        foreach(var item in legos.items)
        {
            legoSelector.AddItem(new DropdownWithImage.Item{ Label = item.label, Icon = item.icon});
        }
        legoSelector.SelectItem(legoSelector.items[0]);
        
        legoSelector.RegisterValueChangedCallback(evt => OnLegoSwitched(evt.newValue));

        DropdownWithImage colorSelector = uiDocument.rootVisualElement.Q<DropdownWithImage>("ColorSelector");

        foreach(var item in colors.items)
        {
            colorSelector.AddItem(new DropdownWithImage.Item{ Label = item.label, Icon = item.icon});
            VisualElement checkmarkBackground = ColorButtons[colors.items.IndexOf(item)].Q<VisualElement>(className: "unity-radio-button__checkmark-background");
            VisualElement checkmark = ColorButtons[colors.items.IndexOf(item)].Q<VisualElement>(className: "unity-radio-button__checkmark");
            checkmarkBackground.style.backgroundColor = item.color;
            checkmark.style.backgroundColor = new Color(-item.color.r + 1f, -item.color.g + 1f, -item.color.b + 1f, 1f);
            ColorButtons[colors.items.IndexOf(item)].RegisterValueChangedCallback(evt => 
            {
                if (evt.newValue)
                {
                    OnColorSwitched(colors.items.IndexOf(item));
                }
            });
        }

        colorSelector.SelectItem(colorSelector.items[0]);
        
        colorSelector.RegisterValueChangedCallback(evt => OnColorSwitched(evt.newValue));

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

        CloseExportWindow();
        CloseImportWindow();
    }

    private void OnColorSwitchButtonClicked()
    {
        GameManager.Instance.paintModeEnabled = !GameManager.Instance.paintModeEnabled;
        PaintModeModified();
    }


    public void OnLegoSwitched(int itemIndex, bool setItemInUI = false)
    {
        GameManager.Instance.legoSelected = itemIndex;

        if (setItemInUI)
        {
            DropdownWithImage ls = uiDocument.rootVisualElement.Q<DropdownWithImage>("LegoSelector");
            ls.SelectItem(ls.items[itemIndex]);
        }
    }

    private void OnColorSwitched(int itemIndex)
    {
        GameManager.Instance.colorSelected = colors.items[itemIndex].color;
    }

    public void PaintModeModified()
    {
        Button colorSwitchButton = uiDocument.rootVisualElement.Q<Button>("ColorSwitchButton");

        if (GameManager.Instance.paintModeEnabled)
        {
            colorSwitchButton.style.backgroundColor = Color.softGreen;
        }
        else
        {
            colorSwitchButton.style.backgroundColor = Color.softRed;
        }
    }
    private string SerializeLego()
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

    private void OpenExportWindow()
    {
        VisualElement ExportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ExportSceneWindow");
        ExportSceneWindow.visible = !ExportSceneWindow.visible;
        CloseImportWindow();
    }

    private void CloseExportWindow()
    {
        VisualElement ExportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ExportSceneWindow");
        ExportSceneWindow.visible = false;
    }

    private void OpenImportWindow()
    {
        VisualElement ImportSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ImportSceneWindow");
        ImportSceneWindow.visible = !ImportSceneWindow.visible;
        RefreshImportFiles();
        CloseExportWindow();
    }

    private void RefreshImportFiles()
    {
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("SceneListView");

        List<string> itemList = Directory.GetFiles(sceneFolderPath, "*.json").ToList();

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
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController.controls.Disable();
    }

    private void OnTextFieldFocusLost()
    {
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
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
