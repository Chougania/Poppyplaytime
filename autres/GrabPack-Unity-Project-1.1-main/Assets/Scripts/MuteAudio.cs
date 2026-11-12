using UnityEngine;

public class MuteAudio : MonoBehaviour
{
    private bool isMuted = false;

    public void ToggleMute()
    {
        isMuted = !isMuted;

        AudioListener.volume = isMuted ? 0f : 1f;
    }

    public void Unmute()
    {
        isMuted = false;

        AudioListener.volume = 1f;
    }
}
