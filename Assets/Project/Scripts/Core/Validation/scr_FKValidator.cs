using UnityEngine;

/// <summary>
/// Validation 계산 전용
/// </summary>
public class scr_FKValidator : MonoBehaviour
{
    [Header("Position Tolerance")]
    [SerializeField] private float positionAxisTolerance = 0.001f;
    [SerializeField] private float positionMagnitudeTolerance = 0.0015f;

    [Header("Rotation Tolerance")]
    [SerializeField] private float rotationAxisTolerance = 0.5f;
    [SerializeField] private float rotationMagnitudeTolerance = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    [System.Serializable]
    public class ValidationResult
    {
        public Vector3 unityPosition;
        public Vector3 pythonPosition;
        public Vector3 unityRotation;
        public Vector3 pythonRotation;

        public float positionErrorX;
        public float positionErrorY;
        public float positionErrorZ;
        public float positionErrorMagnitude;

        public float rotationErrorX;
        public float rotationErrorY;
        public float rotationErrorZ;
        public float rotationErrorMagnitude;

        public bool isPositionPass;
        public bool isRotationPass;
        public bool isFinalPass;
    }

    public ValidationResult ValidatePose(
        Vector3 unityPosition,
        Vector3 pythonPosition,
        Vector3 unityRotation,
        Vector3 pythonRotation)
    {
        ValidationResult result = new ValidationResult();

        result.unityPosition = unityPosition;
        result.pythonPosition = pythonPosition;
        result.unityRotation = unityRotation;
        result.pythonRotation = pythonRotation;

        result.positionErrorX = unityPosition.x - pythonPosition.x;
        result.positionErrorY = unityPosition.y - pythonPosition.y;
        result.positionErrorZ = unityPosition.z - pythonPosition.z;
        result.positionErrorMagnitude = CalculateVectorMagnitude(
            result.positionErrorX,
            result.positionErrorY,
            result.positionErrorZ
        );

        result.rotationErrorX = Mathf.DeltaAngle(unityRotation.x, pythonRotation.x);
        result.rotationErrorY = Mathf.DeltaAngle(unityRotation.y, pythonRotation.y);
        result.rotationErrorZ = Mathf.DeltaAngle(unityRotation.z, pythonRotation.z);
        result.rotationErrorMagnitude = CalculateVectorMagnitude(
            result.rotationErrorX,
            result.rotationErrorY,
            result.rotationErrorZ
        );

        result.isPositionPass =
            Mathf.Abs(result.positionErrorX) <= positionAxisTolerance &&
            Mathf.Abs(result.positionErrorY) <= positionAxisTolerance &&
            Mathf.Abs(result.positionErrorZ) <= positionAxisTolerance &&
            result.positionErrorMagnitude <= positionMagnitudeTolerance;

        result.isRotationPass =
            Mathf.Abs(result.rotationErrorX) <= rotationAxisTolerance &&
            Mathf.Abs(result.rotationErrorY) <= rotationAxisTolerance &&
            Mathf.Abs(result.rotationErrorZ) <= rotationAxisTolerance &&
            result.rotationErrorMagnitude <= rotationMagnitudeTolerance;

        result.isFinalPass = result.isPositionPass && result.isRotationPass;

        if (verboseLog)
        {
            Debug.Log(
                $"[FKValidator] Result={(result.isFinalPass ? "PASS" : "FAIL")} | " +
                $"PosMag={result.positionErrorMagnitude:F6} | RotMag={result.rotationErrorMagnitude:F6}"
            );
        }

        return result;
    }

    private float CalculateVectorMagnitude(float x, float y, float z)
    {
        return Mathf.Sqrt((x * x) + (y * y) + (z * z));
    }

    public float GetPositionAxisTolerance() => positionAxisTolerance;
    public float GetPositionMagnitudeTolerance() => positionMagnitudeTolerance;
    public float GetRotationAxisTolerance() => rotationAxisTolerance;
    public float GetRotationMagnitudeTolerance() => rotationMagnitudeTolerance;
}