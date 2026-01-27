using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightToggle : MonoBehaviour
{
    //Public References
    public Light targetLight; // Reference to the light component to be toggled

    //Private References
    private PlayerControls controls; // Input action asset
    private bool isLightOn = true; // State of the light

    //array of audio clips to be played when light is toggled
    [SerializeField] private AudioClip[] clickAudioClips;
    //audio source that above clips are played off
    [SerializeField] private AudioSource clickAudioSource;

    void Awake()
    {
        //Initialize the input action
        controls = new PlayerControls();
        //Bind the ToggleLight action to the Toggle method
        controls.Player.ToggleLight.performed += ctx => Toggle();
    }

    public void DisableLightOnStart()
    {
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
        
        //if(!playerInRange) return; //Only toggle if the player is in range

        //Toggle the light on and off
        isLightOn = !isLightOn;
        targetLight.enabled = isLightOn;

        //play audio
        PlayAudioClip();
    }

    //added to play audio - HG
    private void PlayAudioClip()
    {
        //assign a random audio clip 
        AudioClip randomClip = clickAudioClips[Random.Range(0, clickAudioClips.Length)];
        clickAudioSource.clip = randomClip;
        //slightly randomize audio pitch
        clickAudioSource.pitch = Random.Range(0.8f, 1.1f);
        //play audio clip
        clickAudioSource.Play();
    }

    //Allows roomManager to assign target light based on spawned room - HG
    public void AssignLightTarget(Light target)
    {
        targetLight = target;
    }
}