using UnityEngine;

public class EncoderListenerTest : MonoBehaviour
{
    public ArduinoEncoderReader encoder;

    private void OnEnable()
    {
        if (encoder != null)
        {
            Debug.Log("Binding Encoder Listener, Current Value: " + encoder.EncoderValue);
            encoder.OnEncoderChanged += HandleEncoder;
        }
    }

    private void OnDisable()
    {
        if (encoder != null)
        {
            encoder.OnEncoderChanged -= HandleEncoder;
        }
    }

    void HandleEncoder(int value)
    {
        Debug.Log("Changed Encoder Value: " + value);
    }
}
