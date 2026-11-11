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

            // Mouse delta dampening
            public const float MOUSE_DELTA_SCALE = 1.0f;
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
        /// Reads 3D flight movement vector for free camera navigation (WASDQE).
        /// </summary>
        public static Vector3 GetFlyMovementVector()
        {
            Vector3 fly = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) fly.z += 1.0f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) fly.z -= 1.0f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) fly.x += 1.0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) fly.x -= 1.0f;
                if (kb.eKey.isPressed || kb.pageUpKey.isPressed) fly.y += 1.0f;
                if (kb.qKey.isPressed || kb.pageDownKey.isPressed) fly.y -= 1.0f;
            }
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) fly.z += 1.0f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) fly.z -= 1.0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) fly.x += 1.0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) fly.x -= 1.0f;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageUp)) fly.y += 1.0f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageDown)) fly.y -= 1.0f;
#endif

            return Vector3.ClampMagnitude(fly, 1.0f);
        }

        /// <summary>
        /// Returns mouse movement delta between frames.
        /// </summary>
        public static Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.delta.ReadValue() * INPUT_CONFIG.MOUSE_DELTA_SCALE;
            }
            return Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        /// <summary>
        /// Returns current screen mouse position in pixels.
        /// </summary>
        public static Vector2 GetMousePositionScreen()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>
        /// Checks if Right Mouse Button is held.
        /// </summary>
        public static bool IsRightMouseButtonHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        /// <summary>
        /// Checks if Right Mouse Button was pressed this frame.
        /// </summary>
        public static bool WasRightMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        /// <summary>
        /// Checks if Right Mouse Button was released this frame.
        /// </summary>
        public static bool WasRightMouseButtonReleased()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(1);
#endif
        }

        /// <summary>
        /// Checks if Middle Mouse Button is held.
        /// </summary>
        public static bool IsMiddleMouseButtonHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.middleButton.isPressed;
#else
            return Input.GetMouseButton(2);
#endif
        }

        /// <summary>
        /// Checks if Left Mouse Button is held.
        /// </summary>
        public static bool IsLeftMouseButtonHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        /// <summary>
        /// Checks if Alt key is pressed (for Scene View Orbit and Pan combinations).
        /// </summary>
        public static bool IsAltKeyHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#endif
        }

        /// <summary>
        /// Checks if Shift key is pressed (for high-speed camera flying).
        /// </summary>
        public static bool IsShiftKeyHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        /// <summary>
        /// Checks if F key was pressed this frame (Focus on player).
        /// </summary>
        public static bool WasFocusKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
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
