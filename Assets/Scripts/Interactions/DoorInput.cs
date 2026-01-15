using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;

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

    [Header("Test Fields")]
    [SerializeField] GameObject doorOpened;
    [SerializeField] GameObject doorClosed;




    void Start()
    {
        OnDoorOpened += OpenDoor;
        OnDoorClosed += CloseDoor;
    }

    // Update is called once per frame
    void Update()
    {

        //here we collect data from the door input about whether it should be considered open

        //just for testing:
        if (Input.GetMouseButtonDown(1))
        {
            if (opened)
            {
                OnDoorClosed?.Invoke();
            }
            else
            {
                OnDoorOpened?.Invoke();
            }
        }




        opened = openedLastFrame;

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
