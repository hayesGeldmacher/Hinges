using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Monster : MonoBehaviour
{
    /// <summary>
    ///This monster approaches the player in stages based on mistakes made
    /// /(Mistakes include holding the light on too long(flashing is ok), and keeping the door open if they are too close.
    /// If the monster gets too close and the player makes a mistake then a jumpscare is triggered) 
    /// </summary>


    public enum Stage { None = -1, Far = 0, Near = 1, Door = 2, Jumpscare = 3 }

    [Header("References")]
    public DoorInput door;               //Find DoorInput.instance if left empty
    public Light watchedLight;           //Assigned per room
    public LightToggle toggle;
    public GameObject monsterVisual; //Monster model 
    public GameObject monsterVisualJumpScare;
    public MonsterAnimations monsterAnimation; //animations for monster

    [Header("Jumpscare Placeholder Settings")]
    public Image jumpscareFlash;
    public GameObject gameOverUI;
    private float onTime = 0.12f;
    private float offTime = 0.08f;
    public float delayBeforeGameOver = 0.25f;
    private int flashCount = 3;
    public AudioSource jumpscareAudio;
    public Animator blackFadeAnim; //black hud image that fades into cam view on death - HG
    public GameObject doorMesh; //disable door mesh on jumpscare so monster doesn't clip - HG
    public RestartGame restartGame;

    [Header("Room Positions")]
    public Transform farPoint;
    public Transform nearPoint;
    public Transform doorPoint;

    [Tooltip("Flashes shorter than this are safe.")]
    public float holdGraceSeconds = 0.35f;

    //how many times can we flash the monster before they become immune?
    [Header("Light Flicking Fields")]
    public int flashTimesTotal;
    public int flashTimesCurrent;
    //how many mistakes does a flash reduce? 
    public int flashMistakeReduction;

    [Tooltip("After grace, add a mistake this often while light is held on.")]
    public float holdMistakeEverySeconds = 1.2f;

    [Header("Stage Progression")]
    [Tooltip("Mistakes needed to move Far->Near, Near->Door.")]
    public int mistakesPerStage = 2;

    [Tooltip("If a mistake happens while at/closer than this stage, you die.")]
    public Stage killIfMistakeAtOrCloser = Stage.Door;

    [Header("Door Close Relief")]
    [Tooltip("Each door close reduces mistakes by this amount.")]
    public int mistakesReducedOnDoorClose = 2;

    [Header("Extra Pressure When Close")]
    public bool doorOpenNearIsDanger = true;

    [Tooltip("If monster is Near/Door and door isn't closed, it adds mistakes this often.")]
    public float nearDoorOpenMistakeEverySeconds = 0.8f;

    [Header("Revisit Memory")]
    [Tooltip("If you ever re-enter a room, restore the monster's last position in that room")]
    public bool restoreLocationIfRevisitingRoom = true;

    [Header("Debug")]
    public Stage currentStage = Stage.None;
    public int mistakes = 0;

    //Internal timers
    private bool lightOn = false;
    private float lightOnTime = 0f;
    private float nextHoldMistakeTime = 0f;
    private float nextNearDoorMistakeTime = 0f;

    //Room memory
    private struct RoomMemory
    {
        public Vector3 lastPos;
        public Quaternion lastRot;
        public bool hasLocation;
    }

    private readonly Dictionary<GameObject, RoomMemory> memory = new Dictionary<GameObject, RoomMemory>();
    private GameObject currentRoomOwner; //The active room GameObject this monster is associated with


    //bool for whether monster is currently active - HG
    [SerializeField] private bool active = false;

    private void Awake()
    {
        if (door == null) door = DoorInput.instance;
        SetVisual(false);
        SetStage(Stage.None);
        DetermineActive();
    }

    private void OnEnable()
    {
        if (DoorInput.instance != null)
        {
            DoorInput.instance.OnDoorClosed += OnDoorClosed;
            RoomManager.instance.OnRoomSpawned += OnRoomSpawned;
            toggle.OnLightFlashed += OnLightFlashed;

        }
    }

    private void OnDisable()
    {
        if (DoorInput.instance != null)
        {
            DoorInput.instance.OnDoorClosed -= OnDoorClosed;
            DoorInput.instance.OnDoorCleared -= OnRoomSpawned;
            toggle.OnLightFlashed -= OnLightFlashed;
        }
    }

    //callback when roommanager spawns a new room -HG
    private void OnRoomSpawned()
    {
        DetermineActive();
    }

    //called at start of new room, determines if monster should be active or not - HG
    private bool DetermineActive()
    {
        //random check if monster should be active
        int RandomCheck = Random.Range(0,3);
        if (RandomCheck == 1)
        {
            Debug.Log("Monster is NOT active in this room!");
            active = false;
            SetVisual(false);
            return (false);
        }
        else
        {
            Debug.Log("Monster is active in this room!");
            active = true;
            return (true);
        }

    }


    private void Update()
    {
        if (door == null) door = DoorInput.instance;

        //if monster is not currently active, do not track- HG
        if (!active) return;

        if (door != null && door.status == DoorInput.DoorStatus.closed)
        {
            lightOnTime = 0f;
            nextHoldMistakeTime = 0f;
            nextNearDoorMistakeTime = 0f;
            UpdateVisualTransform();
            return;
        }


        if (watchedLight != null)
            lightOn = watchedLight.enabled;


        //If light is on, track how long, if too long, register mistakes
        if (lightOn)
        {
            lightOnTime += Time.deltaTime;

            if (lightOnTime > holdGraceSeconds)
            {
                if (currentStage == Stage.None)
                    Summon();

                if (Time.time >= nextHoldMistakeTime)
                {
                    nextHoldMistakeTime = Time.time + holdMistakeEverySeconds;
                    RegisterMistake("Held light too long");
                }
            }
        }
        else
        {
            lightOnTime = 0f;
            nextHoldMistakeTime = 0f;
        }

        //if door is open while monster is close, register mistakes
        if (doorOpenNearIsDanger && (currentStage == Stage.Near || currentStage == Stage.Door))
        {
            //changed that peeking open the door does not increase danger -HG
            bool doorNotClosed = (door != null && door.status == DoorInput.DoorStatus.open);

            if (doorNotClosed)
            {
                if (Time.time >= nextNearDoorMistakeTime)
                {
                    nextNearDoorMistakeTime = Time.time + nearDoorOpenMistakeEverySeconds;
                    RegisterMistake("Door open while monster is close");
                }
            }
            else
            {
                nextNearDoorMistakeTime = 0f;
            }
        }

        UpdateVisualTransform();
    }

    //Call this from RoomManager when switching rooms
    public void AssignRoomAnchors(GameObject roomOwner, Light roomLight, Transform far, Transform near, Transform doorPos)
    {
        //Save current room's last visible position before changing ownership
        SaveCurrentRoomLocation();

        //Assign new room info
        currentRoomOwner = roomOwner;
        watchedLight = roomLight;
        farPoint = far;
        nearPoint = near;
        doorPoint = doorPos;

        // Reset logic for the new room (but keep memory of old room location)
        ResetForNewRoom();

        // If revisiting, restore last position for this room
        if (restoreLocationIfRevisitingRoom)
            RestoreLocationIfAvailable();

        UpdateVisualTransform();
    }

    //Saves the monster's current position for the current room
    public void SaveCurrentRoomLocation()
    {
        if (currentRoomOwner == null || monsterVisual == null) return;
        if (!monsterVisual.activeInHierarchy) return; // only save if it was actually shown

        RoomMemory m;
        if (!memory.TryGetValue(currentRoomOwner, out m))
            m = new RoomMemory();

        m.lastPos = monsterVisual.transform.position;
        m.lastRot = monsterVisual.transform.rotation;
        m.hasLocation = true;

        memory[currentRoomOwner] = m;
    }

    private void RestoreLocationIfAvailable()
    {
        if (currentRoomOwner == null || monsterVisual == null) return;

        if (memory.TryGetValue(currentRoomOwner, out var m) && m.hasLocation)
        {
            monsterVisual.transform.position = m.lastPos;
            monsterVisual.transform.rotation = m.lastRot;
        }
    }

    //Reset threat for the newly spawned room
    private void ResetForNewRoom()
    {
        
        mistakes = 0;
        SetStage(Stage.None);

        //reset timers
        lightOnTime = 0f;
        nextHoldMistakeTime = 0f;
        nextNearDoorMistakeTime = 0f;

        //check if should be active - HG
        DetermineActive();
    }

    private void Summon()
    {
        mistakes = Mathf.Max(0, mistakes);
        SetStage(Stage.Far);
        SetVisual(true);
    }

    private void RegisterMistake(string reason)
    {
        if (currentStage == Stage.Jumpscare) return;

        if (currentStage == Stage.None)
            Summon();

        if ((int)currentStage >= (int)killIfMistakeAtOrCloser)
        {
            TriggerJumpscare(reason);
            return;
        }

        mistakes++;
        RecomputeStage();
    }

    private void OnDoorClosed()
    {
        if (currentStage == Stage.None || currentStage == Stage.Jumpscare) return;

        mistakes = Mathf.Max(0, mistakes - mistakesReducedOnDoorClose);
        RecomputeStage();
    }

    private void OnLightFlashed()
    {
        if(flashTimesCurrent < flashTimesTotal)
        {
            flashTimesCurrent++;
            mistakes = Mathf.Max(0, mistakes - flashMistakeReduction);
            RecomputeStage();
        }
    }

    private void RecomputeStage()
    {
        if (!active) return;
        int stageIndex = (mistakesPerStage <= 0) ? 0 : (mistakes / mistakesPerStage);

        Stage newStage =
            stageIndex <= 0 ? Stage.Far :
            stageIndex == 1 ? Stage.Near :
            Stage.Door;

        SetStage(newStage);
    }

    private void SetStage(Stage newStage)
    {
        if (currentStage == newStage) return;

        currentStage = newStage;

        //check for state to change audio
        if(currentStage == Stage.None || currentStage == Stage.Far)
        {
            ChangeMonsterSoundState(true);
        }
        else
        {
            ChangeMonsterSoundState(false);
        }


        if (currentStage == Stage.None)
        {
            SetVisual(false);
        }
        else if (currentStage != Stage.Jumpscare)
        {
            SetVisual(true);
        }
        

            UpdateVisualTransform();
    }

    //changes breathing volume based on state
    private void ChangeMonsterSoundState(bool far)
    {
       monsterAnimation.MonsterChangeStage(far);
    }

    private void UpdateVisualTransform()
    {

        Transform target = currentStage switch
        {
            Stage.Far => farPoint,
            Stage.Near => nearPoint,
            Stage.Door => doorPoint,
            _ => null
        };

        if (target != null)
        {
            monsterVisual.transform.position = target.position;
            monsterVisual.transform.rotation = target.rotation;
        }
    }

    private void SetVisual(bool on)
    {
        if (monsterVisual != null)
            monsterVisual.SetActive(on);
    }

    private void TriggerJumpscare(string reason)
    {
        if (currentStage == Stage.Jumpscare) return;
        currentStage = Stage.Jumpscare;
        Debug.Log("Jumpscare triggered! Reason: " + reason);
        StartCoroutine(JumpscareSequence());
    }

    private IEnumerator JumpscareSequence()
    {

        if (jumpscareFlash != null)
        {

            //adding jumpscare animations - HG
            if (monsterAnimation != null) {

                monsterVisual.SetActive(false);
            }

            if (doorMesh != null)
            {
                doorMesh.SetActive(false);
                
            }


            yield return new WaitForSeconds(1.5f);
                monsterVisualJumpScare.SetActive(true);
                monsterAnimation.PlayScareAnimation();

            /*
            jumpscareFlash.gameObject.SetActive(true);
             jumpscareFlash.color = Color.white;

            for (int i = 0; i < flashCount; i++)
            {
                jumpscareFlash.gameObject.SetActive(true);
                yield return new WaitForSeconds(onTime);

                jumpscareFlash.gameObject.SetActive(false);
                yield return new WaitForSeconds(offTime);

                // Play jumpscare sound on first flash
                if (i == 0 && jumpscareAudio != null)
                {
                  //  jumpscareAudio.Play();
                }
            }
             */

           // jumpscareAudio.Play();
            if (blackFadeAnim != null)
            {
                blackFadeAnim.SetTrigger("fade");
            }
            yield return new WaitForSeconds(delayBeforeGameOver);

            if (gameOverUI != null)
            {
                gameOverUI.SetActive(true);

            }
            yield return new WaitForSeconds(2f);
            restartGame.RestartCurrentGame();
        }
    }
}