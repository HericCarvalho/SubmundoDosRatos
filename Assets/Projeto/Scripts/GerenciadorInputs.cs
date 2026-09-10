using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GerenciadorInputs : MonoBehaviour
{
    public static event Action AoApertarPulo;
    public static event Action AoApertarTrocarCor;

    private void Update()
    {
        VerificarEntradas();
    }

    private void VerificarEntradas()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 posicaoToque = Touchscreen.current.primaryTouch.position.ReadValue();

            if (posicaoToque.x < Screen.width / 2f)
            {
                AoApertarTrocarCor?.Invoke();
            }
            else
            {
                AoApertarPulo?.Invoke();
            }
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                AoApertarPulo?.Invoke();
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                AoApertarTrocarCor?.Invoke();
            }
        }
    }
}