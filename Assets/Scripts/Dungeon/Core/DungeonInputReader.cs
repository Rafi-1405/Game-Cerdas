using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonGenerator.Core
{
    /// <summary>
    /// Unified input reader compatible with Unity New Input System package.
    /// </summary>
    public static class DungeonInputReader
    {
        #region CONFIG
        public static class INPUT_CONFIG
        {
            // Deadzone threshold for analog sticks
            public const float ANALOG_DEADZONE = 0.15f;

            // Scroll scaling multiplier
            public const float SCROLL_SENSITIVITY = 0.01f;
        }
        #endregion

        /// <summary>
        /// Reads 2D planar movement vector from Keyboard or Gamepad.
        /// </summary>
        public static Vector2 GetMovementVector()
        {
            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1.0f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1.0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1.0f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1.0f;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > (INPUT_CONFIG.ANALOG_DEADZONE * INPUT_CONFIG.ANALOG_DEADZONE))
                {
                    move += stick;
                }
            }
#else
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(move, 1.0f);
        }

        /// <summary>
        /// Detects camera toggle button press (C key or Gamepad North button).
        /// </summary>
        public static bool WasCameraTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) return true;

            Gamepad pad = Gamepad.current;
            if (pad != null && pad.buttonNorth.wasPressedThisFrame) return true;

            return false;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        /// <summary>
        /// Detects minimap radar toggle button press (M key or Gamepad Select button).
        /// </summary>
        public static bool WasMinimapTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) return true;

            Gamepad pad = Gamepad.current;
            if (pad != null && pad.selectButton.wasPressedThisFrame) return true;

            return false;
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }

        /// <summary>
        /// Detects HUD panel toggle button press (F1 key).
        /// </summary>
        public static bool WasHudTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) return true;

            return false;
#else
            return Input.GetKeyDown(KeyCode.F1);
#endif
        }

        /// <summary>
        /// Reads vertical mouse scroll delta for camera zoom.
        /// </summary>
        public static float GetMouseScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.scroll.ReadValue().y * INPUT_CONFIG.SCROLL_SENSITIVITY;
            }
            return 0.0f;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }
    }
}
