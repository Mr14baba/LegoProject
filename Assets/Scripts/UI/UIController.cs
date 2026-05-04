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
        Button openOpenButton = uiDocument.rootVisualElement.Q<Button>("OpenOpenButton");
        Button openRefreshButton = uiDocument.rootVisualElement.Q<Button>("OpenRefreshButton");
        Button openCancelButton = uiDocument.rootVisualElement.Q<Button>("OpenCancelButton");
        Button importImportButton = uiDocument.rootVisualElement.Q<Button>("ImportImportButton");
        Button importRefreshButton = uiDocument.rootVisualElement.Q<Button>("ImportRefreshButton");
        Button importCancelButton = uiDocument.rootVisualElement.Q<Button>("ImportCancelButton");
        ListView sceneToOpenListView = uiDocument.rootVisualElement.Q<ListView>("OpenSceneListView");
        ListView sceneToImportListView = uiDocument.rootVisualElement.Q<ListView>("ImportSceneListView");

        List<RadioButton> ColorButtons = uiDocument.rootVisualElement.Q<VisualElement>("ColorButtonGroup").Query<RadioButton>().ToList();

        List<LegoBlockButton> LegoSelectors = uiDocument.rootVisualElement.Q<ToggleButtonGroup>("ButtonGroupLegoSelected").Query<LegoBlockButton>().ToList();

        foreach (LegoBlockButton button in LegoSelectors)
        {
            button.Q<Image>("").tintColor = GameManager.Instance.colorSelected;
            button.clicked += delegate {OnLegoSwitched(button.legoIndex);};

            button.RegisterCallback<MouseEnterEvent>(evt => UnityEngine.Cursor.SetCursor(mouseHoverSprite, new(16,0), CursorMode.Auto));
            button.RegisterCallback<MouseLeaveEvent>(evt => UnityEngine.Cursor.SetCursor(DefaultMouseSprite, Vector2.zero, CursorMode.Auto));
        }

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

        warningCancelButton.clicked += delegate 
        {
            popupBackground.visible = false; 
            warningScreen.style.display = DisplayStyle.None;
        };
        fileOptionsButton.clicked += delegate 
        {
            fileOptionsWindow.visible = !fileOptionsWindow.visible;
        };
        fileOptionsNew.clicked += delegate 
        {
            ShowWarning(NewScene); 
            fileOptionsWindow.visible = false;
        };
        fileOptionsOpen.clicked += delegate 
        {
            ShowWarning(OpenOpenWindow); 
            fileOptionsWindow.visible = false;
        };
        fileOptionsImport.clicked += delegate 
        {
            OpenImportWindow(); 
            fileOptionsWindow.visible = false;
        };
        fileOptionsExport.clicked += delegate
        {
            ExportScene();
            fileOptionsWindow.visible = false;
        };
        fileOptionsSave.clicked += delegate 
        {
            SaveScene(); 
            fileOptionsWindow.visible = false;
        };
        fileOptionsSaveAs.clicked += delegate 
        {
            OpenSaveWindow(); fileOptionsWindow.visible = false;
        };

        saveAsWindowCancelButton.clicked += CloseSaveWindow;
        saveAsWindowSaveButton.clicked += SaveAsScene;
        openCancelButton.clicked += CloseOpenWindow;
        openOpenButton.clicked += OpenScene;
        openRefreshButton.clicked += delegate{RefreshImportFiles(sceneToOpenListView);};
        importCancelButton.clicked += CloseImportWindow;
        importImportButton.clicked += ImportScene;
        importRefreshButton.clicked += delegate{RefreshImportFiles(sceneToImportListView);};

        sceneToOpenListView.selectionChanged += (fileSelected) => fileToLoad = fileSelected.First().ToSafeString();
        sceneToImportListView.selectionChanged += (fileSelected) => fileToLoad = fileSelected.First().ToSafeString();

        exportTextField.RegisterCallback<FocusInEvent>(evt => OnTextFieldFocusGained());
        exportTextField.RegisterCallback<FocusOutEvent>(evt => OnTextFieldFocusLost());

        // Set all callbacks to update mouse sprite

        VisualElement[] elementsWithMouseEvent = 
        {
            colorSwitchButton,
            saveAsWindowSaveButton,
            saveAsWindowCancelButton,
            openOpenButton,
            openRefreshButton,
            openCancelButton,
            warningCancelButton,
            warningConfirmButton,
            fileOptionsButton,
            fileOptionsNew,
            fileOptionsOpen,
            fileOptionsImport,
            fileOptionsExport,
            fileOptionsSave,
            fileOptionsSaveAs,
            importImportButton,
            importRefreshButton,
            importCancelButton,
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
            List<LegoBlockButton> LegoSelectors = uiDocument.rootVisualElement.Q<ToggleButtonGroup>("ButtonGroupLegoSelected").Query<LegoBlockButton>().ToList();
            Button legoButton = LegoSelectors.Find(item => item.legoIndex == itemIndex);
            
            using var e = new NavigationSubmitEvent() {target = legoButton};
            LegoSelectors.Find(item => item.legoIndex == itemIndex).SendEvent(e);
        }
    }

    private void OnColorSwitched(int itemIndex)
    {
        GameManager.Instance.colorSelected = colors.items[itemIndex].color;

        List<LegoBlockButton> LegoSelectors = uiDocument.rootVisualElement.Q<ToggleButtonGroup>("ButtonGroupLegoSelected").Query<LegoBlockButton>().ToList();
            foreach(LegoBlockButton button in LegoSelectors)
            {
                button.Q<Image>("").tintColor = colors.items[itemIndex].color;
            }
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
        VisualElement saveSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("SaveSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");

        saveSceneWindow.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;
    }

    private void CloseSaveWindow()
    {
        VisualElement saveSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("SaveSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");

        saveSceneWindow.style.display = DisplayStyle.None;
        popupBackground.visible = false;
    }

    private void OpenOpenWindow()
    {
        ListView openList = uiDocument.rootVisualElement.Q<ListView>("OpenSceneListView");
        VisualElement openSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("OpenSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        openSceneWindow.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;

        RefreshImportFiles(openList);
    }

    private void CloseOpenWindow()
    {
        VisualElement openSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("OpenSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        
        openSceneWindow.style.display = DisplayStyle.None;
        popupBackground.visible = false;
        fileToLoad = null;
    }

    private void OpenImportWindow()
    {
        ListView importList = uiDocument.rootVisualElement.Q<ListView>("ImportSceneListView");
        VisualElement importSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ImportSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        importSceneWindow.style.display = DisplayStyle.Flex;
        popupBackground.visible = true;

        RefreshImportFiles(importList);
    }

    private void CloseImportWindow()
    {
        VisualElement importSceneWindow = uiDocument.rootVisualElement.Q<VisualElement>("ImportSceneWindow");
        VisualElement popupBackground = uiDocument.rootVisualElement.Q<VisualElement>("PopupBackground");
        
        importSceneWindow.style.display = DisplayStyle.None;
        popupBackground.visible = false;
        fileToLoad = null;
    }

    private void RefreshImportFiles(ListView listToRefresh)
    {

        List<string> itemList = Directory.GetFiles(SaveScript.Instance.sceneFolderPath, "*.json").ToList();

        Func<VisualElement> makeItem = () => new Label();
        //We select last backslash to have the .json file and we split the .json part of the name.
        Action<VisualElement, int > bindItem = (e, i) => ((Label)e).text = itemList[i].Substring(itemList[i].LastIndexOf("\\") + 1).Split(".")[0];

        listToRefresh.makeItem = makeItem;
        listToRefresh.bindItem = bindItem;
        listToRefresh.itemsSource = itemList;
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

    private void NewScene()
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
        fileNameLabel.text = "NewScene.json";
    }

        private void OpenScene()
    {
        Label fileNameLabel = uiDocument.rootVisualElement.Q<Label>("FileNameLabel");

        if(fileToLoad != null)
        {
            OpenScript.Instance.OpenScene(fileToLoad);
            fileNameLabel.text = fileToLoad.Substring(fileToLoad.LastIndexOf("\\") + 1);
            GameManager.Instance.actualFileName = fileNameLabel.text.Split(".")[0];
            CloseOpenWindow();
        }
    }

    private void ImportScene()
    {
        if(fileToLoad != null)
        {
            ImportScript.Instance.ImportScene(fileToLoad);
            CloseImportWindow();
        }
    }

    private void ExportScene()
    {
        
    }

    private void SaveScene()
    {
        if (GameManager.Instance.actualFileName == "")
        {
            OpenSaveWindow();
        }
        else
        {
            SaveScript.Instance.ExportScene(SerializableLegoList);
        }
        
    }

    private void SaveAsScene()
    {
        Label fileNameLabel = uiDocument.rootVisualElement.Q<Label>("FileNameLabel");
        VisualElement fileOptionsWindow = uiDocument.rootVisualElement.Q<VisualElement>("FileOptionsWindow");

        fileOptionsWindow.visible = false;
        string exportTextField = uiDocument.rootVisualElement.Q<TextField>("ExportTextField").value;
        GameManager.Instance.actualFileName = exportTextField;
        SaveScript.Instance.ExportScene(SerializableLegoList);
        fileNameLabel.text = exportTextField + ".json";
        CloseSaveWindow();
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
