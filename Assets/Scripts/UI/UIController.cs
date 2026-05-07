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

        Button fileButton = uiDocument.rootVisualElement.Q<Button>("FileButton");
        VisualElement fileWindow = uiDocument.rootVisualElement.Q<VisualElement>("FileWindow");
        Button fileNew = uiDocument.rootVisualElement.Q<Button>("FileNew");
        Button fileOpen = uiDocument.rootVisualElement.Q<Button>("FileOpen");
        Button fileImport = uiDocument.rootVisualElement.Q<Button>("FileImport");
        Button fileExport = uiDocument.rootVisualElement.Q<Button>("FileExport");
        Button fileSave = uiDocument.rootVisualElement.Q<Button>("FileSave");
        Button fileSaveAs = uiDocument.rootVisualElement.Q<Button>("FileSaveAs");

        Button optionsButton = uiDocument.rootVisualElement.Q<Button>("OptionsButton");
        VisualElement optionsWindow = uiDocument.rootVisualElement.Q<VisualElement>("OptionsWindow");
        Toggle fullscreenToggle = uiDocument.rootVisualElement.Q<Toggle>("FullscreenToggle");
        Toggle vSyncToggle = uiDocument.rootVisualElement.Q<Toggle>("VSyncToggle");
        Button optionQuit = uiDocument.rootVisualElement.Q<Button>("OptionQuit");

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
        
        //Set first value in ButtonGroupLegoSelector as selected (for visual only)
        LegoSelectors[0].SetCheckedPseudoState(true);

        //Set Fullscreen value
        fullscreenToggle.value = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;

        //Set VSync value
        vSyncToggle.value = Convert.ToBoolean(QualitySettings.vSyncCount);

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

        //File Buttons Event
        fileButton.clicked += delegate 
        {
            fileWindow.visible = !fileWindow.visible;
            optionsWindow.visible = false;
        };
        fileNew.clicked += delegate 
        {
            ShowWarning(NewScene); 
            fileWindow.visible = false;
        };
        fileOpen.clicked += delegate 
        {
            ShowWarning(OpenOpenWindow); 
            fileWindow.visible = false;
        };
        fileImport.clicked += delegate 
        {
            OpenImportWindow(); 
            fileWindow.visible = false;
        };
        fileExport.clicked += delegate
        {
            ExportScene();
            fileWindow.visible = false;
        };
        fileSave.clicked += delegate 
        {
            SaveScene(); 
            fileWindow.visible = false;
        };
        fileSaveAs.clicked += delegate 
        {
            OpenSaveWindow(); fileWindow.visible = false;
        };

        saveAsWindowCancelButton.clicked += CloseSaveWindow;
        saveAsWindowSaveButton.clicked += SaveAsScene;
        openCancelButton.clicked += CloseOpenWindow;
        openOpenButton.clicked += OpenScene;
        openRefreshButton.clicked += delegate{RefreshImportFiles(sceneToOpenListView);};
        importCancelButton.clicked += CloseImportWindow;
        importImportButton.clicked += ImportScene;
        importRefreshButton.clicked += delegate{RefreshImportFiles(sceneToImportListView);};

        //Options Buttons Event

        optionsButton.clicked += delegate 
        {
            optionsWindow.visible = !optionsWindow.visible;
            fileWindow.visible = false;
        };

        fullscreenToggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
            {
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
                //Debug.Log("Fullscreen");
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                //Debug.Log("Windowed");
            }
        });

        vSyncToggle.RegisterValueChangedCallback(evt =>{ QualitySettings.vSyncCount = Convert.ToInt32(evt.newValue); });

        optionQuit.clicked += delegate{ShowWarning(Application.Quit, "All your unsaved progress will be lost !");};

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
            fileButton,
            fileNew,
            fileOpen,
            fileImport,
            fileExport,
            fileSave,
            fileSaveAs,
            importImportButton,
            importRefreshButton,
            importCancelButton,
            optionsButton,
            fullscreenToggle,
            vSyncToggle,
            optionQuit,
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
            
            using var evt = new NavigationSubmitEvent() {target = legoButton};
            LegoSelectors.Find(item => item.legoIndex == itemIndex).SendEvent(evt);
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

        GameManager.Instance.actualFileName = "";
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
        VisualElement fileWindow = uiDocument.rootVisualElement.Q<VisualElement>("FileWindow");

        fileWindow.visible = false;
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
