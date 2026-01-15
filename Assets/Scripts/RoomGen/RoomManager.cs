using UnityEngine;

public class RoomManager : MonoBehaviour
{

    //stores a list of created rooms
    public RoomSpawnInfo[] roomList;

    [SerializeField] private int currentRoom = 0;

    [Header("Spawning Fields")]
    [SerializeField] private Transform roomSpawnLocation;
    [SerializeField] private Transform roomSpawnRotation;


    [Header("Door Open Fields")]

    //is the door ready to open? is the room behind fully spawned in?
    public bool canOpen = false;

    //is the door actually open? determined by sensors/physical door
    public bool isOpen = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

}
