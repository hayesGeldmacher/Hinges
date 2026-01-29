using UnityEngine;

public class MonsterAnimations : MonoBehaviour
{

    [Header("Audio Fields")]
    [SerializeField] private AudioSource breathAudio;
    [SerializeField] private float maxAudio = 1.0f;
    [SerializeField] private float minAudio = 0.5f;


    [Header("Animation Fields")]
    [SerializeField] private Animator anim;


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
        breathAudio.volume = minAudio;
    }

    public void OnDoorOpen()
    {
        breathAudio.volume = maxAudio;
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
