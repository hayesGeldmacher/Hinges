using UnityEngine;

// The Arduino Rotary Encoder will start at value 0.
// As it rotates, it will increment up in one direction.
// It will decrement in the other direction.

public class EncoderListenerTest : MonoBehaviour
{
    public ArduinoEncoderReader encoder; // this encoder is a Singleton, you set it in Inspector

    private void OnEnable()
    {
        if (encoder != null)
        {
            // we can read the initial value of the encoder
            Debug.Log("Binding Encoder Listener, Current Value: " + encoder.EncoderValue);

            // you can subscribe a function when the encoder changes
            encoder.OnEncoderChanged += HandleEncoder;
        }
    }

    private void OnDisable()
    {
        if (encoder != null)
        {
            // unsubscribing the function
            encoder.OnEncoderChanged -= HandleEncoder;
        }
    }

    void HandleEncoder(int value)
    {
        // in this example, this will print out whenever the encoder value changes
        Debug.Log("Changed Encoder Value: " + value);
    }
}
