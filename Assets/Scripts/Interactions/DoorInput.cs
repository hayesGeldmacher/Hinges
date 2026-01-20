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

    [Header("Arduino Fields")]
    public ArduinoEncoderReader encoder;


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

    public delegate void doorPeekingDelegate();
    public doorPeekingDelegate OnDoorPeeked;

    [Header("Rotation Fields")]
    //zero is calibrated to be the door at "closed position"
    [SerializeField] private float rotationValue = 0;
    //the rotation value from last frame
    [SerializeField] private float rotationValueLastFrame = 0;
    //minimum rotation past zero to be considered "peeking"
    [SerializeField] private float peekBuffer = 2f;
    //minimum rotation past zero to be considered "open"
    [SerializeField] private float openBuffer = 6f;
    //maximum value that the door can be rotated - minimum is zero, so not tracking in variable!
    [SerializeField] private float maxRotation = 10;
    //bool tracking whether door has been cleared
    public bool cleared = false;

    [Header("Clear Fields")]
    [SerializeField] private float openTime = 0f;
    [SerializeField] private float minClearTime = 5f;


    //three possible door states: 
    public enum DoorStatus 
    { 
        closed = 0,
        peeking = 1,
        half = 2,
        open = 3

    }
    public DoorStatus status;
    //open
    //peeking
    //closed


    [Header("Opened Fields")]
    //0 = still, -1 means closing, 1 means opening
    [SerializeField] private int moveDirection = 0;



    private void OnEnable()
    {
        InputActions.FindActionMap("Door").Enable();

        if (encoder != null)
        {
            // we can read the initial value of the encoder
            Debug.Log("Binding Encoder Listener, Current Value: " + encoder.EncoderValue);

            // you can subscribe a function when the encoder changes
            encoder.OnEncoderChanged += HandleEncoder;
        }
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Door").Disable();

        if (encoder != null)
        {
            // unsubscribing the function
            encoder.OnEncoderChanged -= HandleEncoder;
        }
    }

    void Start()
    {
    }

    void HandleEncoder(int value)
    {
        // in this example, this will print out whenever the encoder value changes
        Debug.Log("Changed Encoder Value: " + value);

        rotationValue = value;

        //get the current status of the door
        opened = false;
        if (rotationValue >= openBuffer)
        {
            //check if door is fully opened first
            if (rotationValue >= openBuffer)
            {
               //if door is not already open, change to open state
                if(status != DoorStatus.open)
                {
                    ChangeDoorStatus(DoorStatus.open);
                }
            }
        }
        //if not fully open, check if door is peeking
        else if(rotationValue >= peekBuffer)
        {
            //if door is not already peeking, change to peeking state
            if(status != DoorStatus.peeking) 
            {
                ChangeDoorStatus(DoorStatus.peeking);
            };
        }
        else
        {
            if(status != DoorStatus.closed)
            {
                ChangeDoorStatus(DoorStatus.closed);
            }
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

        rotationValueLastFrame = rotationValue;
    }


    private void Update()
    {
        if (status == DoorStatus.open)
        {
            openTime += Time.deltaTime;
            if (openTime >= minClearTime)
            {
                ClearDoor();
            }

        }
    }

    private void ChangeDoorStatus(DoorStatus newStatus) {

        
        switch (newStatus) {

            case DoorStatus.open:
                //make door status open!
                OpenDoor();
                break;
            case DoorStatus.peeking:
                PeekDoor();
                //make door status peeking!
                break;
            case DoorStatus.closed:
                CloseDoor();
                //make door status closed!
                break;
        
        }
    }

    void ClearDoor()
    {
        cleared = true;
        Debug.Log("Cleared the door!");
    }


    void OpenDoor()
    {
        status = DoorStatus.open;
        OnDoorOpened?.Invoke();
    }

    void CloseDoor()
    {
        openTime = 0;
        status = DoorStatus.closed;
        OnDoorClosed?.Invoke();
    }

    void PeekDoor()
    {
        status = DoorStatus.peeking;
        OnDoorPeeked?.Invoke();
    }


}
