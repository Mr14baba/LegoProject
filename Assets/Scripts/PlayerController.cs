using System;
using System.Collections;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Camera playerCam;
    private GameObject focusPoint;
    private Vector2 move;
    private Vector2 rotate;
    private Vector3 baseZoom;
    private Vector3 cameraRotation;
    private RaycastHit mouseCast;
    private float actualZoomPercentage = 100;
    private Coroutine zoomCoroutine;
    private Coroutine placePieceHoveringCoroutine;
    private Coroutine removePieceHoveringCoroutine;
    private Quaternion legoRotation = Quaternion.identity;
    private Vector3 zoomVelocity = Vector3.zero;
    private bool isCorrectSurface;
    private GameObject legoDecoy;
    private int actualLegoClip;

    public PlayerInputs controls;
    [Header("User Parameters")]
    [Tooltip("Speed of the user.")]
    public float speed = 10;
    [Tooltip("multiplier of speed when user press 'Shift'.")]
    public float sprintSpeed = 3;
    [Tooltip("Camera rotation speed of the user.")]
    public float cameraSpeed = 10;
    [Tooltip("Camera rotation speed multiplier of the user with keyboard shortcut.")]
    public Vector2 cameraYClamp = new(-45, 50);
    [Tooltip("Camera distance added or removed when the user scroll.")]
    public float zoomScrollDistance = 10;
    [Tooltip("Determine the minimum and maximum distance of the camera zoom.\nX = Minimum, Y = Maximum")]
    public Vector2 zoomScrollClamp = new(25, 500);
    [Tooltip("List of lego usable by the user, the order is determined from first to last element.")]
    void Awake()
    {
        playerCam = Camera.main;
        baseZoom = playerCam.transform.localPosition;
        focusPoint = transform.GetChild(0).gameObject;
        controls = new PlayerInputs();
        controls.Player.PlacePiece.started += ctx => PlacePieceStarted();
        controls.Player.RemovePiece.started += ctx => RemovePieceStarted();
        controls.Player.MoveCamera.performed += ctx => MoveCameraPressed(ctx.ReadValue<Vector2>());
        controls.Player.MoveCamera.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.MoveCamera.canceled += ctx => move = Vector2.zero;
        controls.Player.RotateCamera.performed += ctx => RotateCameraPressed(ctx.ReadValue<Vector2>());
        controls.Player.RotateCamera.performed += ctx => rotate = ctx.ReadValue<Vector2>();
        controls.Player.RotateCamera.canceled += ctx => rotate = Vector2.zero;
        controls.Player.ScrollZoom.performed += ctx => ScrollZoomPressed(ctx.ReadValue<float>());
        controls.Player.SwitchLego.started += ctx => SwitchLegoPiece(ctx.ReadValue<float>());
        controls.Player.RotateLego.started += ctx => RotateLego();
        controls.Player.SwitchClipBottom.started += ctx => SwitchLegoClip();
        controls.Player.PaintMode.started += ctx => enablePaintMode();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private Vector3 RoundVector3AwayFromZero(Vector3 vector3, bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false)
    {
        float x = 0;
        float y = 0;
        float z = 0;
        if (!ignoreX)
        {
            x = (int)Math.Round(vector3.x, MidpointRounding.AwayFromZero);
        }
        if (!ignoreY)
        {
            y = (int)Math.Round(vector3.y, MidpointRounding.AwayFromZero);
        }
        if (!ignoreZ)
        {
            z = (int)Math.Round(vector3.z, MidpointRounding.AwayFromZero);
        }
        return new Vector3(x,y,z);
    }

    IEnumerator ZoomSmoothing(Vector3 targetPos)
    {
        float speed = 2;
        //Debug.Log(targetPos);
        while (playerCam.transform.localPosition != targetPos)
        {
            playerCam.transform.localPosition = Vector3.SmoothDamp(playerCam.transform.localPosition, targetPos, ref zoomVelocity, speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator PlacePieceCoroutine()
    {
        bool isPlacable = false;
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = quaternion.identity;
        GameObject parentLego = null;
        legoDecoy = Instantiate(GameManager.Instance.usableLegoList[GameManager.Instance.legoSelected], spawnPos, spawnRot);
        legoDecoy.transform.GetComponent<LegoBlock>().SetHoveringMaterial(GameManager.Instance.addHoveringMaterial);

        while (controls.Player.PlacePiece.IsPressed() && !controls.Player.RemovePiece.IsPressed())
        {
            if (isCorrectSurface)
            {
                //select method to place lego on surface
                switch (mouseCast.collider.gameObject.layer)
                {
                    case 3:
                        mouseCast.transform.GetPositionAndRotation(out spawnPos, out spawnRot);
                        parentLego = mouseCast.collider.gameObject;
                        isPlacable = true;
                        break;

                    case 6:
                        spawnPos = RoundVector3AwayFromZero(mouseCast.point);
                        spawnRot = quaternion.identity;
                        parentLego = null;
                        isPlacable = true;
                        break;

                    case 7:
                        GameObject tempPivot = new();
                        Transform child = legoDecoy.transform.GetChild(actualLegoClip);
                        tempPivot.transform.position = child.position;
                        legoDecoy.transform.SetParent(tempPivot.transform);
                        tempPivot.transform.position = mouseCast.transform.position;
                        legoDecoy.transform.SetParent(null);
                        Destroy(tempPivot);
                        spawnPos = legoDecoy.transform.position;
                        spawnRot = mouseCast.transform.rotation * Quaternion.Inverse(child.localRotation);
                        parentLego = mouseCast.collider.gameObject;
                        isPlacable = true;
                        break;

                    default:
                        parentLego = null;
                        isPlacable = false;
                        break;
                }
            }
            else
            {
                isPlacable = false;
                spawnPos = Vector3.zero;
            }
            legoDecoy.transform.SetPositionAndRotation(spawnPos, spawnRot * legoRotation);
            yield return null;
        }

        //Place lego if it is placable, else destroys it
        if (isPlacable && !controls.Player.RemovePiece.IsPressed())
        {
            if (parentLego != null)
            {
                legoDecoy.transform.SetParent(parentLego.transform);
            }
            legoDecoy.GetComponent<LegoBlock>().ResetHoveringMaterial();
            
            for(int i = 0; i < legoDecoy.transform.childCount; i++)
            {
                legoDecoy.transform.GetChild(i).GetComponent<Collider>().enabled = true;
            }
            legoDecoy.GetComponent<Collider>().enabled = true;
            GameManager.Instance.AddNewLego(legoDecoy);
        }
        else
        {
            Destroy(legoDecoy);
        }
        legoRotation = Quaternion.identity;
    }

    IEnumerator RemovePieceCoroutine()
    {
        GameObject currentLegoSelected = null;
        GameObject lastLegoSelected = null;
        while (controls.Player.RemovePiece.IsPressed() && !controls.Player.PlacePiece.IsPressed())
        {
            switch (mouseCast.collider.gameObject.layer)
            {
                case 3:
                    mouseCast.transform.parent.gameObject.GetComponent<LegoBlock>().SetHoveringMaterial(GameManager.Instance.removeHoveringMaterial);
                    currentLegoSelected = mouseCast.transform.parent.gameObject;
                    break;

                case 6:
                    break;

                case 7:
                    mouseCast.transform.parent.gameObject.GetComponent<LegoBlock>().SetHoveringMaterial(GameManager.Instance.removeHoveringMaterial);
                    currentLegoSelected = mouseCast.transform.parent.gameObject;
                    break;
                
                default:
                    mouseCast.collider.gameObject.GetComponent<LegoBlock>().SetHoveringMaterial(GameManager.Instance.removeHoveringMaterial);
                    currentLegoSelected = mouseCast.collider.gameObject;
                    break;
            }
            if (currentLegoSelected != lastLegoSelected)
            {
                if ( lastLegoSelected != null)
                {
                    lastLegoSelected.GetComponent<LegoBlock>().ResetHoveringMaterial();   
                }
                lastLegoSelected = currentLegoSelected;
            }
            yield return null;
        }
        if (!controls.Player.PlacePiece.IsPressed() && currentLegoSelected != null)
        {
            for(int i = 0; i < currentLegoSelected.transform.childCount; i++)
            {
                currentLegoSelected.transform.GetChild(i).transform.DetachChildren();
            }
            GameManager.Instance.RemoveLego(currentLegoSelected);
            Destroy(currentLegoSelected);
        }
        else if (currentLegoSelected != null)
        {
            currentLegoSelected.GetComponent<LegoBlock>().ResetHoveringMaterial(); 
        }
    }

    private void MoveCameraPressed(Vector2 coordinates)
    {
        //Debug.Log("move coordinates = " + coordinates);
    }

    private void RotateCameraPressed(Vector2 coordinates)
    {
        //Debug.Log("rotate coordinates = " + coordinates);
    }

    private void PlacePieceStarted()
    {
        if (GameManager.Instance.paintModeEnabled)
        {
            changeLegoColor();
        }
        else
        {
            placePieceHoveringCoroutine = StartCoroutine(PlacePieceCoroutine());
        }
    }

    private void RemovePieceStarted()
    {
        removePieceHoveringCoroutine = StartCoroutine(RemovePieceCoroutine());
    }

    private void ScrollZoomPressed(float ScrollAxis)
    {
        if (isCorrectSurface)
        {
            actualZoomPercentage = Mathf.Clamp(actualZoomPercentage + zoomScrollDistance * ScrollAxis, zoomScrollClamp.x, zoomScrollClamp.y);
            //Debug.Log("Zoom Percentage = " + actualZoomPercentage);
        
            if(zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }
            zoomCoroutine = StartCoroutine(ZoomSmoothing(actualZoomPercentage / 100 * baseZoom));
        }
        
    }

    private void SwitchLegoPiece(float SwitchAxis)
    {
        if (!controls.Player.PlacePiece.IsPressed())
        {
            UIController uiController = FindFirstObjectByType<UIController>();
            uiController.OnLegoSelected(Mathf.Clamp((int)GameManager.Instance.legoSelected + (int)SwitchAxis, 0, GameManager.Instance.usableLegoList.Count() - 1));
        }
    }

    private void RotateLego()
    {
        if (!legoDecoy.IsUnityNull())
        {
            Debug.Log(legoDecoy.transform.InverseTransformDirection(legoDecoy.transform.GetChild(actualLegoClip).up));
            legoRotation *= Quaternion.Euler(legoDecoy.transform.InverseTransformDirection(legoDecoy.transform.GetChild(actualLegoClip).up) * 90);
        }
    }

    private void SwitchLegoClip()
    {
        if (!legoDecoy.IsUnityNull())
        {
            actualLegoClip = Mathf.Clamp(actualLegoClip + 1, 0, legoDecoy.transform.childCount);
            if (actualLegoClip == legoDecoy.transform.childCount)
            {
                actualLegoClip = 0;
            }
            if (legoDecoy.transform.GetChild(actualLegoClip).gameObject.layer != LayerMask.NameToLayer("LegoClip"))
            {
                SwitchLegoClip();
            }
            legoRotation = quaternion.identity;
        }
    }

    private void enablePaintMode()
    {
        GameManager.Instance.paintModeEnabled = !GameManager.Instance.paintModeEnabled;
        UIController uiController = FindFirstObjectByType<UIController>();
        uiController.PaintModeModified();
        //Debug.Log(GameManager.Instance.paintModeEnabled);
    }

    private void changeLegoColor()
    {
        if (isCorrectSurface)
        {
            Material paintMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            switch (mouseCast.collider.gameObject.layer)
            {
                case 3:
                    paintMaterial.color = GameManager.Instance.colorSelected;
                    mouseCast.transform.parent.gameObject.GetComponent<LegoBlock>().SetMaterial(paintMaterial);
                    break;

                case 6:
                    break;

                case 7:
                    paintMaterial.color = GameManager.Instance.colorSelected;
                    mouseCast.transform.parent.gameObject.GetComponent<LegoBlock>().SetMaterial(paintMaterial);
                    break;
                
                default:
                    paintMaterial.color = GameManager.Instance.colorSelected;
                    mouseCast.collider.GetComponent<LegoBlock>().SetMaterial(paintMaterial);
                    break;
            }
        }
    }

    void FixedUpdate()
    {
        //Player movement (The parent of this script, NOT MainCamera)
        Vector3 movement = (Vector3.ProjectOnPlane(playerCam.transform.forward, Vector3.up).normalized * move.y * speed * Time.deltaTime) + (playerCam.transform.right * move.x * speed * Time.deltaTime);
        movement = new Vector3(movement.x, 0.0f, movement.z);
        if (Keyboard.current.shiftKey.isPressed)
        {
            transform.Translate(movement * sprintSpeed, Space.World);
        }
        else
        {
            transform.Translate(movement, Space.World);
        }

        //Player rotation (Rotation of FocusPoint, NOT MainCamera !)
        cameraRotation += new Vector3(rotate.y, rotate.x, 0.0f) * cameraSpeed * Time.deltaTime;
        cameraRotation = new Vector3(Mathf.Clamp(cameraRotation.x, cameraYClamp.x, cameraYClamp.y), cameraRotation.y, 0.0f);
        focusPoint.transform.eulerAngles = cameraRotation;

        //Raycast to have mouse location and informations
        Ray ray = playerCam.ScreenPointToRay((Vector3)Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out mouseCast) && !EventSystem.current.IsPointerOverGameObject())
        {
            isCorrectSurface = true;
        }
        else
        {
            isCorrectSurface = false;
        }
    }
}