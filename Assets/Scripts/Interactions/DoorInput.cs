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
    public ArduinoDistanceReader reader; //this script reads the raw sensor data. 
    public bool useRotator = false;
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

    public delegate void doorClearedDelegate();
    public doorClearedDelegate OnDoorCleared;
    #endregion

    [Header("Rotation Fields")]
    //zero is calibrated to be the door at "closed position"
    [SerializeField] private int rotationValue = 0;
    //the rotation value from last frame
    [SerializeField] private int rotationValueLastFrame = 0;
    //tracks direction the door is rotating in - 0 = still, -1 = closing, 1 = opening
    [SerializeField] private int moveDirection = 0;
    //tracks the distance covered when rotating the door
    [SerializeField] private int moveDistance = 0;
    //minimum distance moved for the door to be "slammed"
    [SerializeField] private int doorSlammedDistance = 4;
    //minimum rotation past zero to be considered "peeking"
    [SerializeField] private int peekBuffer = 2;
    //minimum rotation past zero to be considered "open"
    [SerializeField] private int openBuffer = 6;

    [Header("Distance Fields")]
    //the starting distance value from the closed door
    [SerializeField] private int baseDistance = 2;



    [Header("Clear Fields")]
    //tracks how long the door has been open without closing
    [SerializeField] private float openTime = 0f;
    //the minimum time the door has to be open in order to "clear" the room
    [SerializeField] private float minClearTime = 5f;

    [SerializeField] private float clearTime = 5f;

    //max time needed to clear the room
    [SerializeField] private float maxClearTime = 9f;
    //bool tracking whether door has been cleared/finished
    public bool cleared = false;
    //Light class, checks if the light is currently on - HG
    [SerializeField] LightToggle toggle;

    [Header("Audio Fields")]
    //Audio player + clips for the door creaking open
    [SerializeField] private AudioSource creakSource;
    [SerializeField] private AudioClip[] creakClips;
    //Audio player + clips for the door shutting closed
    [SerializeField] private AudioSource shutSource;
    [SerializeField] private AudioClip[] shutClips;
    //Audio clips for the door slamming closed 
    //shares audio player with shutting sound, as they will never be played at same time
    [SerializeField] private AudioClip[] slamClips;


    //three possible door states: 
    public enum DoorStatus 
    { 
        closed = 0,
        peeking = 1,
        open = 2

    }
    public DoorStatus status;
    private DoorStatus previousStatus;


    private void Start()
    {
        clearTime = Random.Range(minClearTime, maxClearTime);
    }

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
            if (useRotator)
            {
                encoder.OnEncoderChanged += HandleEncoder;
            }
            else
            {
                reader.OnEncoderChanged += HandleReader;

            }
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
            if (useRotator)
            {
                encoder.OnEncoderChanged -= HandleEncoder;
            }
            else
            {
                reader.OnEncoderChanged -= HandleReader;
            }
        }
    }

    //called whenever the reader changes distance
    void HandleReader(int value)
    {
        // in this example, this will print out whenever the encoder value changes
        Debug.Log("Changed Encoder Value: " + value);

        //subtract the base distance value to account for initial distance when door is closed
        rotationValue = value - baseDistance;


        moveDistance = rotationValue - rotationValueLastFrame;

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


        //get the current status of the door
        if (rotationValue >= openBuffer)
        {
            //check if door is fully opened first
            if (rotationValue >= openBuffer)
            {
                //if door is not already open, change to open state
                if (status != DoorStatus.open)
                {
                    ChangeDoorStatus(DoorStatus.open);
                }
            }
        }
        //if not fully open, check if door is peeking
        else if (rotationValue >= peekBuffer)
        {
            //if door is not already peeking, change to peeking state
            if (status != DoorStatus.peeking)
            {
                ChangeDoorStatus(DoorStatus.peeking);
            }
            ;
        }
        else
        {
            //if not already closed, change to closed state
            if (status != DoorStatus.closed)
            {
                ChangeDoorStatus(DoorStatus.closed);
            }
        }

    }

    //called whenever the encoder changes rotation
    void HandleEncoder(int value)
    {
       
        // in this example, this will print out whenever the encoder value changes
        Debug.Log("Changed Encoder Value: " + value);

        rotationValue = value;

        moveDistance = rotationValue - rotationValueLastFrame;

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
        
    }

    private void LateUpdate()
    {
        //get this value late in update, allows us to check how fast door has moved
        //if we tracked this value in handleEncoder, we would never be able to "skip" rotations for fast movement
        rotationValueLastFrame = rotationValue;
    }

    private void Update()
    {
        
        //track how long the door is open
        if (status == DoorStatus.open && !cleared)
        {
            //only advance to clear status if door light is on!
            if (toggle.isLightOn)
            {
                openTime += Time.deltaTime;
                //once door open time reaches threshold, "clear" the room!
                if (openTime >= clearTime)
                {
                    ClearDoor();
                }
            }
        }

        //below - keyboard controls for debug testing, not important for game functionality
        if (useKeyboard) { KeyboardUpdate(); }

    }

    //update function just for debug keyboard testing
    private void KeyboardUpdate()
    {
        int InputValue = 0;

        //if pressing A, close the door
        if (Input.GetKeyDown(KeyCode.A))
        {
            InputValue = -1 * debugSensitivity + rotationValue;
        }
        //if pressing D, open the door
        else if (Input.GetKeyDown(KeyCode.D))
        {
            InputValue = 1 * debugSensitivity + rotationValue;
        }
        //if pressing W, slam the door!
        else if (Input.GetKeyDown(KeyCode.W))
        {
            InputValue = -4 * debugSensitivity + rotationValue;
        }
        
        if(Mathf.Abs(InputValue) > 0)
        {
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
                if(previousStatus == DoorStatus.open)
                {
                    PeekDoor(false);
                }
                else
                {
                    PeekDoor(true);
                }
                //make door status peeking!
                break;
            case DoorStatus.closed:
                //both slam and close will set status to closed along with unique effects
                //check can only slam if its moved enough distance AND was fully open
                if(Mathf.Abs(moveDistance) >= doorSlammedDistance && status == DoorStatus.open)
                {
                    SlamDoor();
                }
                else
                {
                  CloseDoor();
                }
                //make door status closed!
                break;


        }

        previousStatus = status;
    }

    void ClearDoor()
    {
        cleared = true;
        RoomManager.instance.CallClearedRoom();
        OnDoorCleared?.Invoke();
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
        PlaySound(1);
        status = DoorStatus.closed;
        OnDoorClosed?.Invoke();
    }

    void SlamDoor()
    {
        openTime = 0;
        PlaySound(2);
        status = DoorStatus.closed;
        OnDoorClosed?.Invoke();
    }

    void PeekDoor(bool opening)
    {
        status = DoorStatus.peeking;
        //only play creaking sound if we are opening the door to a creak
        if (opening)
        {
            PlaySound(0);
        }
        OnDoorPeeked?.Invoke();
    }

    //play audio effect for door shutting or creaking open
    private void PlaySound(int SoundCategory)
    {
        AudioSource source;
        AudioClip clip;

        if (creakClips.Length > 0)
        {
            source = creakSource;
            clip = creakClips[Random.Range(0, creakClips.Length)];
        }
        else
        {
            Debug.LogWarning("No audio clips included for door creaking sound!");
            return;
        }

        switch (SoundCategory)
        {
            case (0):
                //already assigned creak above, nothing to do here
                break;
            case (1):
                if (shutClips.Length > 0)
                {
                    source = shutSource;
                    clip = shutClips[Random.Range(0, shutClips.Length)];
                }
                else
                {
                    Debug.LogWarning("No audio clips included for door shutting sound!");
                    return;
                }
                break;
            case (2):
                if (slamClips.Length > 0)
                {
                    source = shutSource;
                    clip = slamClips[Random.Range(0, slamClips.Length)];
                }
                else
                {
                    Debug.LogWarning("No audio clips included for door slamming sound!");
                    return;
                }
                break;
            default:
                //already assigned creak above, nothing to do here
                break;

        }

        source.clip = clip;
        source.pitch = Random.Range(0.8f, 1f);
        source.Play();

    }
}
