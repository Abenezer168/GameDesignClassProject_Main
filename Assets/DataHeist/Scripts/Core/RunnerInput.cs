using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DataHeist
{
    /// <summary>
    /// Input System actions for the runner (the project uses the new Input System only).
    /// Bindings follow the GDD (Space / S / LMB / W / Esc) plus arrows and a gamepad,
    /// and every binding can be remapped; overrides persist in PlayerPrefs.
    /// </summary>
    public class RunnerInput : IDisposable
    {
        const string PrefsKey = "dataheist.bindings";

        public readonly InputActionMap Map;
        public readonly InputAction Jump, Slide, Attack, Ability, Pause;

        public RunnerInput()
        {
            Map = new InputActionMap("Runner");
            Jump = Map.AddAction("Jump", InputActionType.Button);
            Jump.AddBinding("<Keyboard>/space");
            Jump.AddBinding("<Keyboard>/upArrow");
            Jump.AddBinding("<Gamepad>/buttonSouth");

            Slide = Map.AddAction("Slide", InputActionType.Button);
            Slide.AddBinding("<Keyboard>/s");
            Slide.AddBinding("<Keyboard>/downArrow");
            Slide.AddBinding("<Gamepad>/buttonEast");

            Attack = Map.AddAction("Attack", InputActionType.Button);
            Attack.AddBinding("<Mouse>/leftButton");
            Attack.AddBinding("<Keyboard>/j");
            Attack.AddBinding("<Gamepad>/buttonWest");

            Ability = Map.AddAction("Ability", InputActionType.Button);
            Ability.AddBinding("<Keyboard>/w");
            Ability.AddBinding("<Keyboard>/leftShift");
            Ability.AddBinding("<Gamepad>/buttonNorth");

            Pause = Map.AddAction("Pause", InputActionType.Button);
            Pause.AddBinding("<Keyboard>/escape");
            Pause.AddBinding("<Keyboard>/p");
            Pause.AddBinding("<Gamepad>/start");

            LoadOverrides();
            Map.Enable();
        }

        public void LoadOverrides()
        {
            var json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { Map.LoadBindingOverridesFromJson(json); }
                catch (Exception e) { Debug.LogWarning("[DataHeist] Bad binding overrides: " + e.Message); }
            }
        }

        public void SaveOverrides()
        {
            PlayerPrefs.SetString(PrefsKey, Map.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public void ResetOverrides()
        {
            Map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PrefsKey);
        }

        /// <summary>Human readable primary key for an action (first keyboard/mouse binding).</summary>
        public static string KeyLabel(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var path = action.bindings[i].effectivePath;
                if (path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>"))
                    return action.GetBindingDisplayString(i).ToUpperInvariant();
            }
            return action.GetBindingDisplayString(0).ToUpperInvariant();
        }

        /// <summary>Interactive rebind of the first keyboard/mouse binding of an action.</summary>
        public InputActionRebindingExtensions.RebindingOperation Rebind(InputAction action, Action onDone)
        {
            int index = 0;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var path = action.bindings[i].effectivePath;
                if (path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>")) { index = i; break; }
            }
            Map.Disable();
            var op = action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f);
            op.OnComplete(o => { o.Dispose(); SaveOverrides(); Map.Enable(); onDone?.Invoke(); });
            op.OnCancel(o => { o.Dispose(); Map.Enable(); onDone?.Invoke(); });
            op.Start();
            return op;
        }

        public void Dispose()
        {
            Map.Disable();
            Map.Dispose();
        }
    }
}
