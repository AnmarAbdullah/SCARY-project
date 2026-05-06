using UnityEngine;

namespace TimeFracture.Input
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Mouse Settings")]
        public float mouseSensitivity = 2f;

        public Vector2 MoveInput  { get; private set; }
        public Vector2 LookInput  { get; private set; }
        public bool    JumpPressed { get; private set; }
        public bool    SprintHeld  { get; private set; }
        public bool    CrouchHeld  { get; private set; }

        private void Update()
        {
            MoveInput = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));
            LookInput = new Vector2(
                UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity,
                UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity);
            JumpPressed = UnityEngine.Input.GetButtonDown("Jump");
            SprintHeld  = UnityEngine.Input.GetKey(KeyCode.LeftShift);
            CrouchHeld  = UnityEngine.Input.GetKey(KeyCode.LeftControl);
        }

        public void ConsumeJump() { }
    }
}
