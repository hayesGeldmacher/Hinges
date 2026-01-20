

using System.ComponentModel;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
   
    //This script is responsible for spawning rooms and deleting rooms 
    
    
    
    //stores a list of room spawn configurations+
    public RoomSpawnInfo[] roomList;

    [SerializeField] private int currentRoom = 0;

    [Header("Spawning Fields")]
    [SerializeField] private Transform roomSpawnLocation;



    [Header("Light Fields")]
    [SerializeField] private LightToggle lightToggle;

    [Header("Test Fields")]
    [SerializeField] GameObject doorOpened;
    [SerializeField] GameObject doorClosed;

    [SerializeField] private GameObject activeRoom;

    //Sets up singleton pattern so that any script can call to roomManager without reference
    #region Singleton
    public static RoomManager instance;
    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("More than one instance of the RoomManager present");
        }

        instance = this;
    }
    #endregion


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Intializations
        DoorInput.instance.OnDoorOpened += DoorOpened;
        DoorInput.instance.OnDoorClosed += DoorClosed;
        
        SpawnRoomSpecific();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    

    //Spawns a particular type of room
    void SpawnRoomSpecific()
    {
        RoomSpawnInfo roomToSpawn = roomList[currentRoom];
        GameObject spawnedRoom =  Instantiate(roomToSpawn.roomInterior, roomSpawnLocation.position, Quaternion.identity);
        lightToggle.AssignLightTarget(spawnedRoom.GetComponent<RoomInstance>().roomLight);

        //when done spawning, increase current room count
        currentRoom++;
        if(currentRoom > roomList.Length - 1)
        {
            currentRoom = 0;
        }

        //store this room in a variable for later deletion
        activeRoom = spawnedRoom;


        //finally, when room is fully ready, 'open curtains'
        OpenCurtains();

    }


    void OpenCurtains()
    {
        Debug.Log("Opened Curtains!");
    }

    //called when the door is opened!
    void DoorOpened()
    {
        doorOpened.SetActive(true);
        doorClosed.SetActive(false);
        Debug.Log("Door is opened in RoomManager!");
    }

    //called when the door is closed!
    void DoorClosed()
    {
        doorOpened.SetActive(false);
        doorClosed.SetActive(true);

        //spawn new room only if door has been cleared
        if (DoorInput.instance.cleared)
        {
            Debug.Log("Deleted room!");

            DoorInput.instance.cleared = false;

            //delete current room
            DeleteRoom(activeRoom);

            SpawnRoomSpecific();

        }

        Debug.Log("Door is closed in RoomManager!");
    }

    //called once a room is completed and the door has been closed
    void DeleteRoom(GameObject roomToDelete)
    {
        Destroy(roomToDelete);
    }

}
