

using System.ComponentModel;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
   
    //This script is responsible for spawning rooms and deleting rooms 
    
    
    
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

    //stores a list of room spawn configurations+
    public RoomSpawnInfo[] roomList;

    [SerializeField] private int currentRoom = 0;

    [Header("Spawning Fields")]
    [SerializeField] private Transform roomSpawnLocation;
    [SerializeField] private Transform roomSpawnRotation;


    [Header("Door Open Fields")]
    DoorInput doorInput;

    [Header("Light Fields")]
    [SerializeField] private LightToggle lightToggle;

    [Header("Test Fields")]
    [SerializeField] GameObject doorOpened;
    [SerializeField] GameObject doorClosed;

    //is the door ready to open? is the room behind fully spawned in?
    public bool canOpen = false;

    //is the door actually open? determined by sensors/physical door
    public bool isOpen = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Intializations
        doorInput.OnDoorOpened += DoorOpened;
        
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
        GameObject spawnedRoom =  Instantiate(roomToSpawn.roomInterior, roomSpawnLocation, roomSpawnRotation);
        lightToggle.AssignLightTarget(roomToSpawn.roomLight);

        //when done spawning, increase current room count
        currentRoom++;

        //finally, when room is fully ready, 'open curtains'
        OpenCurtains();

    }


    void OpenCurtains()
    {
        
        canOpen = true;
    }


    //Spawns a random room
    void SpawnRoomRandom()
    {
        
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
        Debug.Log("Door is closed in RoomManager!");
    }

    //called once a room is completed and the door has been closed
    void DeleteRoom(GameObject roomToDelete)
    {
        Destroy(roomToDelete);
    }

}
