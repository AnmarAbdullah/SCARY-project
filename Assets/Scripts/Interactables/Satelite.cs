using UnityEngine;
using Mirror;

public class Satelite : Interactable
{
    private enum AngleSpace { World, Local }
    private enum RotationAxis { X, Y, Z }

    [Header("Rotating Parts")]
    [SerializeField] private Transform wheel;
    [SerializeField] private Transform satellite;
    [SerializeField] private RotationAxis wheelRotationAxis = RotationAxis.X;

    [Header("Rotation")]
    [SerializeField] private float satelliteRotateSpeed = 35f;
    [SerializeField] private float wheelRotateSpeed = 220f;
    [SerializeField] private bool rotateSatelliteClockwise;
    [SerializeField] private bool rotateWheelRightToLeft = true;

    [Header("Correct Angle Range")]
    [SerializeField, Range(0f, 360f)] private float correctMinYAngle = 90f;
    [SerializeField, Range(0f, 360f)] private float correctMaxYAngle = 170f;
    [SerializeField] private AngleSpace angleSpace = AngleSpace.World;

    [Header("Signal Power")]
    [SerializeField] private float maxSignalPower = 100f;
    [SyncVar] public float signalPower = 100f;
    [SerializeField] private float correctAngleDrainRate = 30f;
    [SerializeField] private float wrongAngleRecoveryRate = 10f;

    [Header("Satellite Resistance")]
    [SerializeField, Range(0f, 360f)] private float wrongRestYAngle = 270f;
    [SerializeField] private float wrongRestReturnSpeed = 20f;

    [Header("Sync")]
    [SyncVar] private float syncedSatelliteYAngle;
    [SyncVar] private float syncedWheelAngle;

    [Header("Debug")]
    [SerializeField] private float currentDebugYAngle;
    [SerializeField] private bool debugIsInCorrectAngle;

    public float SignalPower => signalPower;
    public float CurrentAngle => CurrentYAngle;
    public bool IsInCorrectAngle => IsAngleInsideRange(CurrentYAngle, correctMinYAngle, correctMaxYAngle);

    private float CurrentYAngle
    {
        get
        {
            Transform target = satellite != null ? satellite : transform;
            float yAngle = angleSpace == AngleSpace.World ? target.eulerAngles.y : target.localEulerAngles.y;
            return NormalizeAngle(yAngle);
        }
    }

    private void Reset()
    {
        interactionType = InteractionType.HoldFree;
        satellite = transform;
    }

    private void Awake()
    {
        interactionType = InteractionType.HoldFree;
        signalPower = Mathf.Clamp(signalPower, 0f, maxSignalPower);
    }

    private void Update()
    {
        if (isDone) return;

        if (isServer)
        {
            if (isOccupied)
            {
                RotateWheel();
                RotateSatellite();
                syncedSatelliteYAngle = CurrentYAngle;
                syncedWheelAngle = GetWheelAngle();
            }

            if (debugIsInCorrectAngle)
                PullSatelliteBackToWrongAngle();

            RefreshDebugState();
            UpdateSignalPower(debugIsInCorrectAngle);
        }
        else
        {
            ApplySyncedRotations();
        }
    }

    private void ApplySyncedRotations()
    {
        SetSatelliteYAngle(syncedSatelliteYAngle);

        if (wheel != null)
        {
            Vector3 angles = wheel.localEulerAngles;
            angles[(int)wheelRotationAxis] = syncedWheelAngle;
            wheel.localEulerAngles = angles;
        }
    }

    private float GetWheelAngle()
    {
        if (wheel == null) return 0f;
        return wheel.localEulerAngles[(int)wheelRotationAxis];
    }

    private void RotateWheel()
    {
        if (wheel == null) return;
        float direction = rotateWheelRightToLeft ? 1f : -1f;
        wheel.Rotate(GetAxisVector(wheelRotationAxis), wheelRotateSpeed * direction * Time.deltaTime, Space.Self);
    }

    private void RotateSatellite()
    {
        float playerDirection = rotateSatelliteClockwise ? 1f : -1f;
        float playerRotation = satelliteRotateSpeed * playerDirection * Time.deltaTime;
        float currentAngle = CurrentYAngle;
        SetSatelliteYAngle(currentAngle + playerRotation);
    }

    private void PullSatelliteBackToWrongAngle()
    {
        float currentAngle = CurrentYAngle;
        float targetAngle = NormalizeAngle(wrongRestYAngle);
        float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, wrongRestReturnSpeed * Time.deltaTime);
        SetSatelliteYAngle(nextAngle);
    }

    private void SetSatelliteYAngle(float yAngle)
    {
        Transform target = satellite != null ? satellite : transform;
        yAngle = NormalizeAngle(yAngle);

        if (angleSpace == AngleSpace.World)
        {
            Vector3 eulerAngles = target.eulerAngles;
            eulerAngles.y = yAngle;
            target.eulerAngles = eulerAngles;
            return;
        }

        Vector3 localEulerAngles = target.localEulerAngles;
        localEulerAngles.y = yAngle;
        target.localEulerAngles = localEulerAngles;
    }

    private void UpdateSignalPower(bool isInsideCorrectAngle)
    {
        if (isInsideCorrectAngle)
            signalPower -= correctAngleDrainRate * Time.deltaTime;
        else if (signalPower < maxSignalPower)
            signalPower += wrongAngleRecoveryRate * Time.deltaTime;

        signalPower = Mathf.Clamp(signalPower, 0f, maxSignalPower);

        if (signalPower <= 0f)
            CompleteInteraction();
    }

    private static bool IsAngleInsideRange(float angle, float min, float max)
    {
        angle = NormalizeAngle(angle);
        min = NormalizeAngle(min);
        max = NormalizeAngle(max);

        if (min <= max)
            return angle >= min && angle <= max;

        return angle >= min || angle <= max;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }

    private void RefreshDebugState()
    {
        currentDebugYAngle = CurrentYAngle;
        debugIsInCorrectAngle = IsAngleInsideRange(currentDebugYAngle, correctMinYAngle, correctMaxYAngle);
    }

    private static Vector3 GetAxisVector(RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            default: return Vector3.right;
        }
    }
}