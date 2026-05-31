using UnityEngine;

/// <summary>
/// FR5 Validation Manager
///
/// 역할:
/// 1. FK TCP vs Python TCP 기준으로 validation 수행
/// 2. validation_report 저장 요청
/// 3. tolerance 값을 report writer와 동기화
///
/// 원칙:
/// - Solver validation과 Scene follow validation을 구분한다
/// - 현재 본 validation은 FK Solver 기준 검증이다
/// - Unity scene의 실제 tcp transform 비교는 보조 확인용으로 남긴다
/// </summary>
public class scr_FR5ValidationManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private scr_FR5RobotManualController fr5Controller;
    [SerializeField] private scr_FKValidator fkValidator;
    [SerializeField] private scr_ValidationReportWriter reportWriter;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private void Awake()
    {
        SyncToleranceToWriter();
    }

    /// <summary>
    /// 메인 validation:
    /// FK TCP vs Python TCP
    /// </summary>
    public scr_FKValidator.ValidationResult ValidateCurrentPoseOnly()
    {
        if (!ValidateReferences())
        {
            return null;
        }

        fr5Controller.ForceRefreshStatus();

        if (!fr5Controller.IsPythonTCPLoaded())
        {
            Debug.LogWarning("[FR5ValidationManager] Python TCP is not loaded. Validation skipped.");
            return null;
        }

        Vector3 fkPosition = fr5Controller.GetCurrentFKTCPLocalPosition();
        Vector3 pythonPosition = fr5Controller.GetCurrentPythonTCPLocalPosition();
        Vector3 fkRotation = fr5Controller.GetCurrentFKTCPLocalRotation();
        Vector3 pythonRotation = fr5Controller.GetCurrentPythonTCPLocalRotation();

        scr_FKValidator.ValidationResult result = fkValidator.ValidatePose(
            fkPosition,
            pythonPosition,
            fkRotation,
            pythonRotation
        );

        if (verboseLog && result != null)
        {
            Debug.Log(
                $"[FR5ValidationManager] FK vs Python | " +
                $"Result={(result.isFinalPass ? "PASS" : "FAIL")} | " +
                $"FK Pos={fkPosition} | Python Pos={pythonPosition} | " +
                $"FK Rot={fkRotation} | Python Rot={pythonRotation}"
            );
        }

        return result;
    }

    /// <summary>
    /// 보조 확인:
    /// Unity TCP vs FK TCP
    /// 이 값은 scene follow / tcp object 연결 상태 확인용
    /// </summary>
    public scr_FKValidator.ValidationResult ValidateUnityTcpAgainstFk()
    {
        if (!ValidateReferences())
        {
            return null;
        }

        fr5Controller.ForceRefreshStatus();

        Vector3 unityPosition = fr5Controller.GetCurrentUnityTCPLocalPosition();
        Vector3 fkPosition = fr5Controller.GetCurrentFKTCPLocalPosition();
        Vector3 unityRotation = fr5Controller.GetCurrentUnityTCPLocalRotation();
        Vector3 fkRotation = fr5Controller.GetCurrentFKTCPLocalRotation();

        scr_FKValidator.ValidationResult result = fkValidator.ValidatePose(
            unityPosition,
            fkPosition,
            unityRotation,
            fkRotation
        );

        if (verboseLog && result != null)
        {
            Debug.Log(
                $"[FR5ValidationManager] Unity TCP vs FK | " +
                $"Result={(result.isFinalPass ? "PASS" : "FAIL")} | " +
                $"Unity Pos={unityPosition} | FK Pos={fkPosition} | " +
                $"Unity Rot={unityRotation} | FK Rot={fkRotation}"
            );
        }

        return result;
    }

    public scr_FKValidator.ValidationResult ValidateCurrentCase(string testCaseId, string testCaseName)
    {
        scr_FKValidator.ValidationResult result = ValidateCurrentPoseOnly();

        if (result == null)
        {
            return null;
        }

        if (reportWriter == null)
        {
            Debug.LogError("[FR5ValidationManager] Validation Report Writer is not assigned.");
            return result;
        }

        reportWriter.SaveReport(result, testCaseId, testCaseName);

        if (verboseLog)
        {
            Debug.Log($"[FR5ValidationManager] Saved report | Case={testCaseId} | Name={testCaseName}");
        }

        return result;
    }

    public void ProcessValidation(string testCaseId, string testCaseName)
    {
        ValidateCurrentCase(testCaseId, testCaseName);
    }

    private bool ValidateReferences()
    {
        if (fr5Controller == null)
        {
            Debug.LogError("[FR5ValidationManager] FR5 Controller is not assigned.");
            return false;
        }

        if (fkValidator == null)
        {
            Debug.LogError("[FR5ValidationManager] FK Validator is not assigned.");
            return false;
        }

        return true;
    }

    private void SyncToleranceToWriter()
    {
        if (fkValidator == null || reportWriter == null)
        {
            return;
        }

        reportWriter.SetToleranceInfo(
            fkValidator.GetPositionAxisTolerance(),
            fkValidator.GetPositionMagnitudeTolerance(),
            fkValidator.GetRotationAxisTolerance(),
            fkValidator.GetRotationMagnitudeTolerance()
        );
    }

    public void RefreshToleranceSync()
    {
        SyncToleranceToWriter();
    }
}