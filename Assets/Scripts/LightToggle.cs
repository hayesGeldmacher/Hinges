using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightToggle : MonoBehaviour
{
    public Light targetLight; // Reference to the light component to be toggled
    private PlayerControls controls; // Input action asset
    private bool isLightOn = true; // State of the light

    void Start()
    {
        //Start with the light off
        isLightOn = false;
        targetLight .enabled = isLightOn;
    }
    void Awake()
    {
        //Initialize the input action
        controls = new PlayerControls();
        //Bind the ToggleLight action to the Toggle method
        controls.Player.ToggleLight.performed += ctx => Toggle();

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
        //Toggle the light on and off
        isLightOn = !isLightOn;
        targetLight.enabled = isLightOn;
    }
}