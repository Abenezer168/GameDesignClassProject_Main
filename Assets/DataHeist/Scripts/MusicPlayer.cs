using UnityEngine;

// Put this next to an AudioSource that plays music. It uses the music volume chosen in Options.
public class MusicPlayer : MonoBehaviour
{
    void Start()
    {
        GetComponent<AudioSource>().volume = SaveManager.GetMusicVolume();
    }
}
