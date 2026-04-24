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
    private SerializableList<LegoData> SerializableLegoList;
    private string fileToLoad;
    [HideInInspector] public Texture2D DefaultMouseSprite = null;
    public Texture2D mouseHoverSprite;
    public Texture2D mouseWriteSprite;
    public Texture2D mousePaintSprite;
    void Start()
    {
        Button colorSwitchButton = uiDocument.rootVisualElement.Q<Button>("ColorSwitchButton");
        TextField exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField");
        Button saveAsWindowSaveButton = uiDocument.rootVisualElement.Q<Button>("SaveAsSaveButton");
        Button saveAsWindowCancelButton = uiDocument.rootVisualElement.Q<Button>("SaveAsCancelButton");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        VisualElement warningScreen = uiDocument.rootVisualElement.Q<VisualElement>("WarningScreen");
        Button warningCancelButton = uiDocument.rootVisualElement.Q<Button>("WarningCancelButton");
        Button warningConfirmButton = uiDocument.rootVisualElement.Q<Button>("WarningConfirmButton");
        Button fileOptionsButton = uiDocument.rootVisualElement.Q<Button>("FileOptionsButton");
        VisualElement fileOptionsWindow = uiDocument.rootVisualElement.Q<VisualElement>("FileOptionsWindow");
        Button fileOptionsNew = uiDocument.rootVisualElement.Q<Button>("FileOptionsNew");
        Button fileOptionsOpen = uiDocument.rootVisualElement.Q<Button>("FileOptionsOpen");
        Button fileOptionsImport = uiDocument.rootVisualElement.Q<Button>("FileOptionsImport");
        Button fileOptionsExport = uiDocument.rootVisualElement.Q<Button>("FileOptionsExport");
        Button fileOptionsSave = uiDocument.rootVisualElement.Q<Button>("FileOptionsSave");
        Button fileOptionsSaveAs = uiDocument.rootVisualElement.Q<Button>("FileOptionsSaveAs");
        Button importWindowImportButton = uiDocument.rootVisualElement.Q<Button>("ImportImportButton");
        Button importWindowRefreshButton = uiDocument.rootVisualElement.Q<Button>("ImportRefreshButton");
        Button importWindowCancelButton = uiDocument.rootVisualElement.Q<Button>("ImportCancelButton");
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("SceneListView");

        List<RadioButton> ColorButtons = uiDocument.rootVisualElement.Q<VisualElement>("ColorButtonGroup").Query<RadioButton>().ToList();

        DropdownWithImage legoSelector = uiDocument.rootVisualElement.Q<DropdownWithImage>("LegoSelector");
        legoSelector.mouseHoverSprite = mouseHoverSprite;
        legoSelector.selectedIcon.tintColor = GameManager.Instance.colorSelected;

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
            ColorButtons[colors.items.IndexOf(item)].RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto));
        }

        // Set all events when buttons are clicked

        colorSwitchButton.clicked += OnColorSwitchButtonClicked;

        saveAsWindowCancelButton.clicked += CloseSaveWindow;
        saveAsWindowSaveButton.clicked += SaveAsSceneUI;
        warningCancelButton.clicked += delegate {
            popupBackground.visible = false;
            warningScreen.style.display = DisplayStyle.None;
            };
        fileOptionsButton.clicked += delegate {fileOptionsWindow.visible = !fileOptionsWindow.visible;};
        fileOptionsNew.clicked += delegate {ShowWarning(NewSceneUI);};
        fileOptionsOpen.clicked += delegate {ShowWarning(OpenOpenWindow);};
        //fileOptionsImport.clicked += ;
        //fileOptionsExport.clicked += ;
        fileOptionsSave.clicked += SaveScene;
        fileOptionsSaveAs.clicked += OpenSaveWindow;

        importWindowCancelButton.clicked += CloseOpenWindow;
        importWindowImportButton.clicked += OpenSceneUI;
        importWindowRefreshButton.clicked += RefreshImportFiles;
        sceneToImportListView.selectionChanged += (fileSelected) => fileToLoad = fileSelected.First().ToSafeString();

        exportTextField.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocusGained());
        exportTextField.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocusLost());

        // Close the export and import window

        CloseSaveWindow();
        CloseOpenWindow();

        // Set all callbacks to update mouse sprite

        VisualElement[] elementsWithMouseEvent = 
        {
            colorSwitchButton,
            saveAsWindowSaveButton,
            saveAsWindowCancelButton,
            importWindowImportButton,
            importWindowRefreshButton,
            importWindowCancelButton,
            legoSelector,
            warningCancelButton,
            warningConfirmButton,
            fileOptionsButton,
            fileOptionsNew,
            fileOptionsOpen,
            fileOptionsImport,
            fileOptionsExport,
            fileOptionsSave,
            fileOptionsSaveAs,
        };

        foreach(VisualElement element in elementsWithMouseEvent)
        {
            element.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseHoverSprite, new(16,0), CursorMode.Auto));
            element.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto));
        };

        exportTextField.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseWriteSprite, Vector2.zero, CursorMode.Auto));
        exportTextField.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto));
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
        DropdownWithImage ls = uiDocument.rootVisualElement.Q<DropdownWithImage>("LegoSelector");
        ls.selectedIcon.tintColor = GameManager.Instance.colorSelected;
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
            DefaultMouseSprite = mousePaintSprite;
        }
        else
        {
            colorSwitchButton.style.borderTopColor = Color.softRed;
            colorSwitchButton.style.borderBottomColor = Color.softRed;
            colorSwitchButton.style.borderLeftColor = Color.softRed;
            colorSwitchButton.style.borderRightColor = Color.softRed;
            DefaultMouseSprite = null;
        }
        UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto);
    }
    private void OpenSaveWindow()
    {
        VisualElement SaveSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("SaveSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");

        SaveSceneWindow.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;
    }

    private void CloseSaveWindow()
    {
        VisualElement SaveSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("SaveSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");

        SaveSceneWindow.style.display = DisplayStyle.None;
        popupBackground.visible = false;
    }

    private void OpenOpenWindow()
    {
        
        VisualElement OpenSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("OpenSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        OpenSceneWindow.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;

        RefreshImportFiles();
    }

    private void RefreshImportFiles()
    {
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("SceneListView");

        List<string> itemList = Directory.GetFiles(SaveScript.Instance.sceneFolderPath, "*.json").ToList();

        Func<VisualElement> makeItem = () => new Label();
        //We select last backslash to have the .json file and we split the .json part of the name.
        Action<VisualElement, int > bindItem = (e, i) => ((Label)e).text = itemList[i].Substring(itemList[i].LastIndexOf("\\") + 1).Split(".")[0];

        sceneToImportListView.makeItem = makeItem;
        sceneToImportListView.bindItem = bindItem;
        sceneToImportListView.itemsSource = itemList;
        fileToLoad = null;
    }

    private void CloseOpenWindow()
    {
        VisualElement OpenSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("OpenSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        
        OpenSceneWindow.style.display = DisplayStyle.None;
        popupBackground.visible = false;
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
        UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto);
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController.controls.Enable();
    }

    private void NewSceneUI()
    {
        var dictTypeOfLegoPlaced = GameManager.Instance.dictTypeOfLegoPlaced;
        Label fileNameLabel = uiDocument.rootVisualElement.Q<Label>("FileNameLabel");

        foreach(LegoEnum key in dictTypeOfLegoPlaced.Keys)
        {
            foreach(GameObject legoToRemove in dictTypeOfLegoPlaced[key])
            {
                Destroy(legoToRemove);
            }
        }

        GameManager.Instance.actualFileName = null;
        fileNameLabel.text = "newScene";
    }

    private void SaveScene()
    {
        if (GameManager.Instance.actualFileName == null)
        {
            OpenSaveWindow();
        }
        else
        {
            SaveScript.Instance.ExportScene(SerializableLegoList);
        }
        
    }

    private void SaveAsSceneUI()
    {
        Label fileNameLabel = uiDocument.rootVisualElement.Q<Label>("FileNameLabel");

        string exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField").value;
        GameManager.Instance.actualFileName = exportTextField;
        SaveScript.Instance.ExportScene(SerializableLegoList);
        fileNameLabel.text = exportTextField + ".json";
        CloseSaveWindow();
    }

    private void OpenSceneUI()
    {
        Label fileNameLabel = uiDocument.rootVisualElement.Q<Label>("FileNameLabel");
        if(fileToLoad != null)
        {
            OpenScript.Instance.ImportScene(fileToLoad);
            fileNameLabel.text = fileToLoad.Substring(fileToLoad.LastIndexOf("\\") + 1);
            GameManager.Instance.actualFileName = fileNameLabel.text.Split(".")[0];
            CloseOpenWindow();
        }
    }

    private void ShowWarning(Action functionToExecute, string warningTextToShow = "All unsaved progress will be lost !")
    {
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        VisualElement warningScreen = uiDocument.rootVisualElement.Q<VisualElement>("WarningScreen");
        Label warningLabel = uiDocument.rootVisualElement.Q<Label>("WarningLabel");
        Button warningConfirmButton = uiDocument.rootVisualElement.Q<Button>("WarningConfirmButton");
        warningLabel.text = warningTextToShow;
        warningScreen.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;

        warningConfirmButton.clickable = null;
        warningConfirmButton.clicked += delegate {
            popupBackground.visible = false;
            warningScreen.style.display = DisplayStyle.None; 
            };
        warningConfirmButton.clicked += functionToExecute;
    }
}
