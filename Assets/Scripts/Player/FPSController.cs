using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;
    public Transform cameraHolder;

    private CharacterController _cc;
    private Camera[] _cameras;
    private AudioListener[] _audioListeners;
    private Vector3 _velocity;
    private float _xRotation;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cameras = GetComponentsInChildren<Camera>(true);
        _audioListeners = GetComponentsInChildren<AudioListener>(true);
    }

    public override void OnStartClient()
    {
        SetLocalCameraState(false);
    }

    public override void OnStartLocalPlayer()
    {
        SetLocalCameraState(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStopLocalPlayer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

        cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        bool grounded = _cc.isGrounded;
        if (grounded && _velocity.y < 0f) _velocity.y = -2f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * h + transform.forward * v;
        _cc.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && grounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void SetLocalCameraState(bool active)
    {
        foreach (Camera cam in _cameras)
            cam.enabled = active;

        foreach (AudioListener listener in _audioListeners)
            listener.enabled = active;
    }
}
