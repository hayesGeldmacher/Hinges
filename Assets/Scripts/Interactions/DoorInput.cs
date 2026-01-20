using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.InputSystem;

public class DoorInput : MonoBehaviour
{
    [Header("Input Fields")]
    public InputActionAsset InputActions; //the asset our controls are bound to
    [SerializeField] private float inputAxis; //this float tracks door rotation input
    private InputAction doorAction; //stores the rotation door input asset
    private PlayerControls controls; // Input action asset

    //Singleton pattern to ensure only one DoorInput instance exists
    #region
    public static DoorInput instance;
    private void Awake()
    {


        doorAction = InputSystem.actions.FindAction("Door/Rotate");

        if (instance != null)
        {
            Debug.LogWarning("More than once instance of DoorInput in scene!");
            return;
        }

        instance = this;
    }
    #endregion

    public bool opened = false;
    public bool openedLastFrame = false;

    public delegate void doorOpenedDelegate();
    public doorOpenedDelegate OnDoorOpened;

    public delegate void doorClosedDelegate();
    public doorClosedDelegate OnDoorClosed;

    [Header("Rotation Fields")]
    //zero is calibrated to be the door at "closed position"
    [SerializeField] private float rotationValue = 0;
    //the rotation value from last frame
    [SerializeField] private float rotationValueLastFrame = 0;
    //minimum rotation past zero to be considered "open"
    [SerializeField] private float openBuffer = 0.5f;
    //maximum value that the door can be rotated - minimum is zero, so not tracking in variable!
    [SerializeField] private float maxRotation = 10;
    //minimum value door needs to be opened to be considered "cleared"
    [SerializeField] private float minClearRotation = 5;
    //bool tracking whether door has been cleared
    public bool cleared = false;

    [Header("Opened Fields")]
    //0 = still, -1 means closing, 1 means opening
    [SerializeField] private int moveDirection = 0;




    private void OnEnable()
    {
        InputActions.FindActionMap("Door").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Door").Disable();
    }

    void Start()
    {

         OnDoorOpened += OpenDoor;
         OnDoorClosed += CloseDoor;


    }

    // Update is called once per frame
    void Update()
    {

        inputAxis = doorAction.ReadValue<float>();
        rotationValue += inputAxis;
        //clamp the rotation of the door
        rotationValue = Mathf.Clamp(rotationValue, 0, maxRotation);

        //get the current status of the door
        opened = false;
        if (rotationValue >= openBuffer)
        {
            opened = true;
            if(rotationValue >= minClearRotation)
            {
                if (!cleared)
                {
                    ClearDoor();
                }
            }
        }

        //if door status is changed, call toggle function
        if(opened != openedLastFrame)
        {
            ToggleDoor();
        }

        //get the direction the door is moving
        if (rotationValue > rotationValueLastFrame)
        {
            //door is opening
            moveDirection = 1;
        }
        else if (rotationValue < rotationValueLastFrame)
        {
            //door is closing
            moveDirection = -1;
        }
        else
        {
            //door is still
            moveDirection = 0;
        }
        
        //record the "last frame" status of the door position
        rotationValueLastFrame = rotationValue;
        openedLastFrame = opened;

    }

    void ClearDoor()
    {
        cleared = true;
        Debug.Log("Cleared the door!");
    }

    void ToggleDoor()
    {
        Debug.Log("Toggled the door");   
        
        if (opened)
        {
            OnDoorClosed?.Invoke();
        }
        else
        {
            OnDoorOpened?.Invoke();
        }

    }

    void OpenDoor()
    {
        Debug.Log("Opened door from input");
    }

    void CloseDoor()
    {
        Debug.Log("Closed door from input");
    }


}
