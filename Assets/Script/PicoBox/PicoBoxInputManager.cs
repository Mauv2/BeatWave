using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PicoBoxInputManager : MonoBehaviour
{
    public bool WDown => GetDown(NoteLine.Up);
    public bool SDown => GetDown(NoteLine.Down);
    public bool WHeld => GetHeld(NoteLine.Up);
    public bool SHeld => GetHeld(NoteLine.Down);
    public bool WUp => GetUp(NoteLine.Up);
    public bool SUp => GetUp(NoteLine.Down);

    // Read directly in the judging frame: no scene dependency or Update-order delay.
    public static bool GetDown(NoteLine line)
    {
        return Input.GetKeyDown(Key(line)) || ReadShoulder(line, 0);
    }

    public static bool GetHeld(NoteLine line)
    {
        return Input.GetKey(Key(line)) || ReadShoulder(line, 1);
    }

    public static bool GetUp(NoteLine line)
    {
        return (Input.GetKeyUp(Key(line)) || ReadShoulder(line, 2)) && !GetHeld(line);
    }

    static KeyCode Key(NoteLine line) => line == NoteLine.Up ? KeyCode.W : KeyCode.S;

    static bool ReadShoulder(NoteLine line, int phase)
    {
        foreach (var pad in Gamepad.all)
        {
            if (!pad.enabled || !IsPicoBox(pad.displayName)) continue;
            ButtonControl button = line == NoteLine.Up ? pad.leftShoulder : pad.rightShoulder;
            if (phase == 0 ? button.wasPressedThisFrame : phase == 1 ? button.isPressed : button.wasReleasedThisFrame)
                return true;
        }
        return false;
    }

    static bool IsPicoBox(string name) => !string.IsNullOrEmpty(name) &&
        (name.IndexOf("POKKEN", StringComparison.OrdinalIgnoreCase) >= 0 ||
         name.IndexOf("PicoBox", StringComparison.OrdinalIgnoreCase) >= 0);
}
