using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightToggle : MonoBehaviour
{
    //Public References
    public Light targetLight; // Reference to the light component to be toggled

    //Private References
    private PlayerControls controls; // Input action asset
    private bool playerInRange = false; // Flag to check if player is in range
    private bool isLightOn = true; // State of the light

    void Awake()
    {
        //Initialize the input action
        controls = new PlayerControls();
        //Bind the ToggleLight action to the Toggle method
        controls.Player.ToggleLight.performed += ctx => Toggle();
    }
    void Start()
    {
        //Start with the light off
        targetLight.enabled = false;
    }

    private void OnEnable()
    {
        //Enable the input action when the object is enabled
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        //Disable the input action when the object is disabled
        controls.Player.Disable();
    }

    private void Toggle()
    {
        if(!playerInRange) return; //Only toggle if the player is in range

        //Toggle the light on and off
        isLightOn = !isLightOn;
        targetLight.enabled = isLightOn;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player in light toggle zone.");
        //Check if the player entered the trigger zone
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        //Check if the player exited the trigger zone
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}