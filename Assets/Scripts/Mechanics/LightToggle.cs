using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightToggle : MonoBehaviour
{
    //Public References
    public Light targetLight; // Reference to the light component to be toggled

    //Private References
    private PlayerControls controls; // Input action asset
    public bool isLightOn = true; // State of the light - changed to public so doorInput can read- HG

    //array of audio clips to be played when light is toggled
    [SerializeField] private AudioClip[] clickAudioClips;
    //audio source that above clips are played off
    [SerializeField] private AudioSource clickAudioSource;

    [Header("Rapid Flicking Fields")] //checks if the player is flipping light on/off fast -HG
    //how much time player has to toggle immediately after a toggle for flicking -
    [SerializeField] private float flickTimeThreshold;
    //how much time currently has passed since last toggle
    [SerializeField] private float flickTimeCurrent;
    //must toggle at least three times within flicktimethreshold to flash
    [SerializeField] private int currentFlickCount = 0;

    //delegate for light flashing - HG
    //delegate for when a new room is spawned - HG
    public delegate void LightFlashedDelegate();
    public LightFlashedDelegate OnLightFlashed;

    void Awake()
    {
        //Initialize the input action
        controls = new PlayerControls();
        //Bind the ToggleLight action to the Toggle method
        controls.Player.ToggleLight.performed += ctx => Toggle();
    }

    public void DisableLightOnStart()
    {
       //targetLight.enabled = false;
       targetLight.gameObject.SetActive(false); //- changed to disable gameObject -HG
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
        //changed to enable and disable game objects - HG
        targetLight.gameObject.SetActive(isLightOn);
        //targetLight.enabled = isLightOn;

        //if flicked more than twice in succession, the light is flashed
        currentFlickCount++;
        if(currentFlickCount > 2)
        {
            flickTimeCurrent = 0;
            currentFlickCount = 0;
            OnLightFlashed?.Invoke();
            Debug.Log("LIGHT WAS FLASHED");
        }
        //set flick timer
        flickTimeCurrent = flickTimeThreshold;

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

    private void Update()
    {
        if(currentFlickCount > 0)
        {
            flickTimeCurrent -= Time.deltaTime;
            if(flickTimeCurrent <= 0)
            {
                currentFlickCount = 0;
            }
        }
    }
}