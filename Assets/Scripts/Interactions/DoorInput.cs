using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.InputSystem;

public class DoorInput : MonoBehaviour
{
    //This script interfaces with the ArduinoCodeReader to translate rotary input to usable game input
    
    [Header("Debug Input Fields")]
    public InputActionAsset InputActions; //the asset our controls are bound to
    private PlayerControls controls; // Input action asset
    private InputAction doorOpenAction; //stores the rotate open input asset
    private InputAction doorCloseAction; //stores the rotate close input asset
    [SerializeField] private float inputAxis; //this float tracks door rotation input
    [SerializeField] private bool useKeyboard = false;
    [SerializeField] private int debugSensitivity = 1;

    [Header("Arduino Fields")]
    public ArduinoEncoderReader encoder; //the script that reads raw arduino data


    //Singleton pattern to ensure only one DoorInput instance exists
    #region
    public static DoorInput instance;
    private void Awake()
    {


        doorOpenAction = InputSystem.actions.FindAction("Door/RotateOpen");
        doorCloseAction = InputSystem.actions.FindAction("Door/RotateClose");

        if (instance != null)
        {
            Debug.LogWarning("More than once instance of DoorInput in scene!");
            return;
        }

        instance = this;
    }
    #endregion

    //Delegates for door states, other scripts can subscribe to these
    #region
    public delegate void doorOpenedDelegate();
    public doorOpenedDelegate OnDoorOpened;

    public delegate void doorClosedDelegate();
    public doorClosedDelegate OnDoorClosed;

    public delegate void doorPeekingDelegate();
    public doorPeekingDelegate OnDoorPeeked;
    #endregion

    [Header("Rotation Fields")]
    //zero is calibrated to be the door at "closed position"
    [SerializeField] private int rotationValue = 0;
    //the rotation value from last frame
    [SerializeField] private int rotationValueLastFrame = 0;
    //tracks direction the door is rotating in - 0 = still, -1 = closing, 1 = opening
    [SerializeField] private int moveDirection = 0;
    //minimum rotation past zero to be considered "peeking"
    [SerializeField] private int peekBuffer = 2;
    //minimum rotation past zero to be considered "open"
    [SerializeField] private int openBuffer = 6;

    [Header("Clear Fields")]
    //tracks how long the door has been open without closing
    [SerializeField] private float openTime = 0f;
    //the minimum time the door has to be open in order to "clear" the room
    [SerializeField] private float minClearTime = 5f;
    //bool tracking whether door has been cleared/finished
    public bool cleared = false;

    [Header("Audio Fields")]
    [SerializeField] private AudioSource creakSource;
    [SerializeField] private AudioClip[] creakClips;


    //three possible door states: 
    public enum DoorStatus 
    { 
        closed = 0,
        peeking = 1,
        open = 2

    }
    public DoorStatus status;

    //on enable, subscribe to the encoder data
    //also enable test keyboard inputs
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

    //on enable, unsubscribe to the encoder data
    //also disable test keyboard inputs
    private void OnDisable()
    {
        InputActions.FindActionMap("Door").Disable();

        if (encoder != null)
        {
            // unsubscribing the function
            encoder.OnEncoderChanged -= HandleEncoder;
        }
    }

    //called whenever the encoder changes rotation
    void HandleEncoder(int value)
    {
       
        // in this example, this will print out whenever the encoder value changes
        Debug.Log("Changed Encoder Value: " + value);

        rotationValue = value;

        //get the current status of the door
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
            //if not already closed, change to closed state
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
        
        //track how long the door is open
        if (status == DoorStatus.open && !cleared)
        {
            openTime += Time.deltaTime;
            //once door open time reaches threshold, "clear" the room!
            if (openTime >= minClearTime)
            {
                ClearDoor();
            }
        }

        //below - keyboard controls for debug testing, not important for game functionality
        if (useKeyboard) { KeyboardUpdate(); }

    }

    //update function just for debug keyboard testing
    private void KeyboardUpdate()
    {
        //if pressing A, close the door
        if (Input.GetKeyDown(KeyCode.A))
        {
            int InputValue = -1 * debugSensitivity + rotationValue;
            InputValue = Mathf.Clamp(InputValue, 0, openBuffer);
            HandleEncoder(InputValue);
        }
        //if pressing D, open the door
        else if (Input.GetKeyDown(KeyCode.D))
        {
            int InputValue = 1 * debugSensitivity + rotationValue;
            InputValue = Mathf.Clamp(InputValue, 0, openBuffer);
            HandleEncoder(InputValue);
        }
    }

    //switch statement, called whenever door changes states
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
        RoomManager.instance.CallClearedRoom();
        Debug.Log("Cleared the door!");
    }

    void OpenDoor()
    {
        status = DoorStatus.open;
        PlayCreakSound();
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

    private void PlayCreakSound()
    {
        if (creakClips != null)
        {
            AudioClip clip = creakClips[Random.Range(0, creakClips.Length)];
            creakSource.clip = clip;
            creakSource.pitch = Random.Range(0.8f, 1.1f);
            creakSource.Play();
        }
        else
        {
            Debug.LogWarning("No audio clips included for door creeking sound!");
        }
    }
}
