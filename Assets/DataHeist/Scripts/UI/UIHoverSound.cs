using UnityEngine;
using UnityEngine.EventSystems;

namespace DataHeist
{
    /// <summary>Plays a soft tick when a button is hovered or keyboard-selected.</summary>
    public class UIHoverSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public void OnPointerEnter(PointerEventData e) => AudioManager.I.Sfx("ui_move", 0.5f);
        public void OnSelect(BaseEventData e) => AudioManager.I.Sfx("ui_move", 0.5f);
    }
}
