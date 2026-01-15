using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.InputSystem;

public class DoorInput : MonoBehaviour
{

    //Singleton pattern to ensure only one DoorInput instance exists
    #region
    public static DoorInput instance;
    private void Awake()
    {
        if(instance != null)
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

    private PlayerControls controls; // Input action asset

    void Start()
    {

         OnDoorOpened += OpenDoor;
         OnDoorClosed += CloseDoor;

        controls = new PlayerControls();
        //Bind the ToggleDoor action to the  method - not working, need to investigate why...
        controls.Player.ToggleDoor.performed += ctx => ToggleDoor();
    }

    // Update is called once per frame
    void Update()
    {

        //here we collect data from the door input about whether it should be considered open
        opened = openedLastFrame;

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
        opened = true;
        Debug.Log("Opened door from input");
    }

    void CloseDoor()
    {
        opened = false;
        Debug.Log("Closed door from input");
    }


}
