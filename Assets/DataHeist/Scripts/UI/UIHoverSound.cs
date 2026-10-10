using UnityEngine;
using UnityEngine.EventSystems;

namespace DataHeist
{
    /// <summary>
    /// Plays a soft tick when the player hovers or keyboard-selects a button. Selections made by
    /// code (UIKit.Select when a screen opens) stay silent, so no sound plays without input.
    /// </summary>
    public class UIHoverSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public static bool Suppress;
        static float _lastTick;

        public void OnPointerEnter(PointerEventData e) => Tick();
        public void OnSelect(BaseEventData e) { if (!Suppress) Tick(); }

        static void Tick()
        {
            // hover and select often fire together; one tick per move
            if (Time.unscaledTime - _lastTick < 0.05f) return;
            _lastTick = Time.unscaledTime;
            AudioManager.I.Sfx("ui_move", 0.5f);
        }
    }
}
