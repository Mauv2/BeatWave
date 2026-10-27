using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PicoBoxInputTest : MonoBehaviour
{
    void Start()
    {
        foreach (var device in InputSystem.devices)
        {
            Debug.Log(
                $"장치 발견: {device.displayName} / {device.layout}"
            );
        }
    }

    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                if (control is ButtonControl button &&
                    button.wasPressedThisFrame)
                {
                    Debug.Log(
                        $"버튼 입력: {device.displayName} / {control.path}"
                    );
                }
            }
        }
    }
}
