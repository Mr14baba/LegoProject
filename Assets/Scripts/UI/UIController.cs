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
    public Texture2D mouseHoverSprite;
    public Texture2D mouseWriteSprite;
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
        legoSelector.mouseHoverSprite = mouseHoverSprite;

        // Set all legos to put in the dropdown

        foreach(var item in legos.items)
        {
            legoSelector.AddItem(new DropdownWithImage.Item{ Label = item.label, Icon = item.icon});
        }
        legoSelector.SelectItem(legoSelector.items[0]);
        
        legoSelector.RegisterValueChangedCallback(evt => OnLegoSwitched(evt.newValue));

        // Set all colors to put in the radio buttons

        foreach(var item in colors.items)
        {
            VisualElement checkmarkBackground = ColorButtons[colors.items.IndexOf(item)].Q<VisualElement>(className: "unity-radio-button__checkmark-background");
            VisualElement checkmark = ColorButtons[colors.items.IndexOf(item)].Q<VisualElement>(className: "unity-radio-button__checkmark");
            
            //Set color of each buttons background and hide the checkmark
            checkmarkBackground.style.backgroundColor = item.color;
            checkmark.style.backgroundColor = new Color(0f, 0f, 0f, 0f);

            //Set the event to change the color selected for each button
            ColorButtons[colors.items.IndexOf(item)].RegisterValueChangedCallback(evt => 
            {
                if (evt.newValue)
                {
                    OnColorSwitched(colors.items.IndexOf(item));
                }
            });

            // Set all callbacks to update mouse sprite when hovered
            ColorButtons[colors.items.IndexOf(item)].RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseHoverSprite, new(16,0), CursorMode.Auto));
            ColorButtons[colors.items.IndexOf(item)].RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
        }

        // Set all events when buttons are clicked

        colorSwitchButton.clicked += OnColorSwitchButtonClicked;

        exportButton.clicked += OpenExportWindow;
        exportWindowCancelButton.clicked += CloseExportWindow;
        exportWindowExportButton.clicked += ExportSceneUI;

        importButton.clicked += OpenImportWindow;
        importWindowCancelButton.clicked += CloseImportWindow;
        importWindowImportButton.clicked += ImportSceneUI;
        importWindowRefreshButton.clicked += RefreshImportFiles;
        sceneToImportListView.selectionChanged += (fileSelected) => fileToLoad = fileSelected.First().ToSafeString();

        exportTextField.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocusGained());
        exportTextField.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocusLost());

        // Close the export and import window

        CloseExportWindow();
        CloseImportWindow();

        // Set all callbacks to update mouse sprite

        VisualElement[] elementsWithMouseEvent = 
        {
            colorSwitchButton,
            exportButton,
            exportWindowExportButton,
            exportWindowCancelButton,
            importButton,
            importWindowImportButton,
            importWindowRefreshButton,
            importWindowCancelButton,
            legoSelector,
        };

        foreach(VisualElement element in elementsWithMouseEvent)
        {
            element.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseHoverSprite, new(16,0), CursorMode.Auto));
            element.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
        };

        exportTextField.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseWriteSprite, Vector2.zero, CursorMode.Auto));
        exportTextField.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto));
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
            colorSwitchButton.style.borderTopColor = Color.softGreen;
            colorSwitchButton.style.borderBottomColor = Color.softGreen;
            colorSwitchButton.style.borderLeftColor = Color.softGreen;
            colorSwitchButton.style.borderRightColor = Color.softGreen;
        }
        else
        {
            colorSwitchButton.style.borderTopColor = Color.softRed;
            colorSwitchButton.style.borderBottomColor = Color.softRed;
            colorSwitchButton.style.borderLeftColor = Color.softRed;
            colorSwitchButton.style.borderRightColor = Color.softRed;
        }
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

        List<string> itemList = Directory.GetFiles(ExportScript.Instance.sceneFolderPath, "*.json").ToList();

        Func<VisualElement> makeItem = () => new Label();
        //We select last backslash to have the .json file and we split the .json part of the name.
        Action<VisualElement, int > bindItem = (e, i) => ((Label)e).text = itemList[i].Substring(itemList[i].LastIndexOf("\\") + 1).Split(".")[0];

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
        UnityEngine.Cursor.SetCursor(mouseWriteSprite, Vector2.zero, CursorMode.Auto);
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController.controls.Disable();
    }

    private void OnTextFieldFocusLost()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController.controls.Enable();
    }

    private void ExportSceneUI()
    {
        string exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField").value;

        ExportScript.Instance.ExportScene(exportTextField, SerializableLegoList);
        CloseExportWindow();
    }

    private void ImportSceneUI()
    {
        if(fileToLoad != null)
        {
            ImportScript.Instance.ImportScene(fileToLoad);
            CloseImportWindow();
        }
    }
}
