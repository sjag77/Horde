using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Horde
{
    /// <summary>
    /// One-thumb control: the stick appears wherever the finger lands and stays fixed there;
    /// the knob is clamped to the ring.
    /// WASD / arrows also work for testing in the editor.
    /// </summary>
    public sealed class FloatingJoystick
    {
        public bool Active;
        public Vector2 Origin;
        public Vector2 Knob;

        public float SizeScale = 1f;          // settings: small / medium / large
        public bool DoubleTapBlink = true;    // settings: double-tap to dash
        public float RadiusPx => Mathf.Max(60f, Screen.height * 0.06f) * SizeScale;

        /// <summary>Screen points (like the ULT button) where a new touch must not start the stick.</summary>
        public System.Func<Vector2, bool> IsBlocked;
        bool ignoring;
        float lastTap = -1f;
        bool blinkQueued;

        /// <summary>True once after a double tap (two touches starting within 0.3 s).</summary>
        public bool ConsumeBlink()
        {
            bool b = blinkQueued;
            blinkQueued = false;
            return b;
        }

        bool Ignore(Vector2 pos, bool began)
        {
            if (began) ignoring = IsBlocked != null && IsBlocked(pos);
            return ignoring;
        }

        public Vector2 Read()
        {
            Vector2 result = Vector2.zero;
            if (TryPointer(out Vector2 pos, out bool began) && !Ignore(pos, began))
            {
                if (began)
                {
                    float now = Time.unscaledTime;
                    if (DoubleTapBlink && now - lastTap < 0.3f) blinkQueued = true;
                    lastTap = now;
                }
                if (began || !Active)
                {
                    Active = true;
                    Origin = pos;
                }
                Vector2 delta = pos - Origin;
                float r = RadiusPx;
                if (delta.magnitude > r) delta = delta.normalized * r;   // base stays put; knob clamps to the ring
                Knob = Origin + delta;
                Vector2 v = delta / r;
                if (v.magnitude > 0.12f) result = v; // dead zone
            }
            else
            {
                Active = false;
            }

            Vector2 keys = KeyboardVector();
            if (keys != Vector2.zero) result = keys;
            return Vector2.ClampMagnitude(result, 1f);
        }

        static bool TryPointer(out Vector2 pos, out bool began)
        {
#if ENABLE_INPUT_SYSTEM
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                pos = touch.primaryTouch.position.ReadValue();
                began = touch.primaryTouch.press.wasPressedThisFrame;
                return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                pos = mouse.position.ReadValue();
                began = mouse.leftButton.wasPressedThisFrame;
                return true;
            }
#else
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                began = t.phase == TouchPhase.Began;
                return true;
            }
            if (Input.GetMouseButton(0))
            {
                pos = Input.mousePosition;
                began = Input.GetMouseButtonDown(0);
                return true;
            }
#endif
            pos = default;
            began = false;
            return false;
        }

        static Vector2 KeyboardVector()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y).normalized;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
        }
    }
}
