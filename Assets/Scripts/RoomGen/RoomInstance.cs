using UnityEngine;

public class RoomInstance : MonoBehaviour
{

    [Header("Room Fields")]
    public bool dangerous = false;
    public bool cleared = false;
    public Light roomLight;

    //Monster Positions
    public Transform monsterFar;
    public Transform monsterNear;
    public Transform monsterDoor; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
