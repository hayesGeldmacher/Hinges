using UnityEngine;

public class MonsterAnimations : MonoBehaviour
{

    [Header("Audio Fields")]
    [SerializeField] private AudioSource breathAudio;
    [SerializeField] private float maxAudio = 1.0f;
    [SerializeField] private float minAudio = 0.5f;

    //if monster is behind closed door, subtract following amount from audio volume
    public float doorClosedReduce = 0.5f;

    //if monster is in far state, subtract following amount from audio volume
    public float farAwayReduce = 0.4f;

    public bool doorClosed = true;
    public bool farAway = true;

    [Header("Animation Fields")]
    [SerializeField] private Animator anim;

    private void Start()
    {
        SetAudioVolume();
    }

    private void OnEnable()
    {
        if (DoorInput.instance != null)
        {
            DoorInput.instance.OnDoorClosed += OnDoorClosed;
            DoorInput.instance.OnDoorOpened += OnDoorOpen;
        }
    }

    private void OnDisable()
    {
        if (DoorInput.instance != null)
        {
            DoorInput.instance.OnDoorClosed -= OnDoorClosed;
            DoorInput.instance.OnDoorOpened -= OnDoorOpen;
        }

    }

    public void PlayScareAnimation()
    {
        anim.SetTrigger("scare");
    }

    public void OnDoorClosed()
    {
        doorClosed = true;
        SetAudioVolume();
    }

    public void OnDoorOpen()
    {
        doorClosed = false;
        breathAudio.volume = maxAudio;
        SetAudioVolume();
    }

    public void MonsterChangeStage(bool far)
    {
        farAway = far;
        SetAudioVolume();
    }

    private void SetAudioVolume()
    {
        float baseVolume = 1.0f;
        if (doorClosed) { baseVolume -= doorClosedReduce; }
        if (farAway) { baseVolume -= farAwayReduce; }
        breathAudio.volume = baseVolume;
    }
    public void StopBreathAudio()
    {
        breathAudio.Stop();
    }

    public void PlayBreathAudio()
    {
        breathAudio.Play();
    }
}
