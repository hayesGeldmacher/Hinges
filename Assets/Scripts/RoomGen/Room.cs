using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]

public class Room
{
    public bool dangerous;
    public GameObject roomInterior;

    public enum RoomDanger{

        //the room has no danger
        Safe = 0,

        //the room requires that you click to flash a light several times to dispel monster
        Light = 1,

        //the room requires that you quickly leave to stay safe, then slowly open again
        Quick = 2,
    }

    public enum RoomType { 
    
       //Fancy models/decorations
       Fancy = 0,

       //dingy models/decorations
       Dingy = 1,

       //empty room
       Empty = 2,

       //storage room
       Storage = 3,
    }
}
