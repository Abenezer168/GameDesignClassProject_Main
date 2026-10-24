using UnityEngine;
using UnityEngine.UI;

// The Options page: music and sound volume sliders. The values are saved with SaveManager.
public class OptionsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider soundSlider;
    public AudioSource music;               // the menu music, so the change can be heard right away
    public AudioSource soundPreview;        // plays a short sound when the sound volume changes
    public AudioClip previewSound;

    void Start()
    {
        // Show the saved values on the sliders (without triggering the sliders' events)
        musicSlider.SetValueWithoutNotify(SaveManager.GetMusicVolume());
        soundSlider.SetValueWithoutNotify(SaveManager.GetSoundVolume());
    }

    // Called by the music slider
    public void SetMusicVolume(float volume)
    {
        SaveManager.SetMusicVolume(volume);
        music.volume = volume;
    }

    // Called by the sound slider
    public void SetSoundVolume(float volume)
    {
        SaveManager.SetSoundVolume(volume);
        if (!soundPreview.isPlaying)
        {
            soundPreview.PlayOneShot(previewSound, volume);
        }
    }
}
