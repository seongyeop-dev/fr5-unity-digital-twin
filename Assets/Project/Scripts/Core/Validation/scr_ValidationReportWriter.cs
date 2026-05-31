using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Validation Report 저장 전용 클래스
///
/// 역할:
/// 1. validation_report.txt 경로 관리
/// 2. 고유 Test ID 생성
/// 3. Validation 결과를 텍스트 형식으로 저장
/// 4. FAIL 원인 문자열 생성
///
/// 원칙:
/// - 계산하지 않음
/// - UI 갱신하지 않음
/// - FK 실행하지 않음
/// - 오직 파일 저장만 담당
///
/// 현재 기준:
/// - 메인 validation 결과는 FK TCP vs Python FK 비교 결과를 저장한다
/// </summary>
public class scr_ValidationReportWriter : MonoBehaviour
{
    [Header("Output File")]
    [SerializeField] private string outputFolderName = "Output";
    [SerializeField] private string outputFileName = "validation_report.txt";

    [Header("Tolerance Info (for report text only)")]
    [SerializeField] private float positionAxisTolerance = 0.001f;       // 1 mm
    [SerializeField] private float positionMagnitudeTolerance = 0.0015f; // 1.5 mm
    [SerializeField] private float rotationAxisTolerance = 0.5f;         // 0.5 deg
    [SerializeField] private float rotationMagnitudeTolerance = 1.0f;    // 1.0 deg

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private int currentTestIdNumber = 0;
    private string reportPath = string.Empty;

    /// <summary>
    /// 현재 report 저장 경로
    /// </summary>
    public string ReportPath => reportPath;

    private void Awake()
    {
        reportPath = Path.Combine(
            Application.streamingAssetsPath,
            outputFolderName,
            outputFileName
        );

        EnsureOutputDirectoryExists();
        currentTestIdNumber = LoadLastTestIdNumber();
    }

    /// <summary>
    /// 외부에서 tolerance 표시값을 맞추고 싶을 때 사용
    /// validator와 같은 값을 넣어주기 위한 setter
    /// </summary>
    public void SetToleranceInfo(
        float posAxis,
        float posMagnitude,
        float rotAxis,
        float rotMagnitude)
    {
        positionAxisTolerance = posAxis;
        positionMagnitudeTolerance = posMagnitude;
        rotationAxisTolerance = rotAxis;
        rotationMagnitudeTolerance = rotMagnitude;
    }

    /// <summary>
    /// Validation 결과 저장
    /// </summary>
    public void SaveReport(
        scr_FKValidator.ValidationResult result,
        string testCaseId,
        string testCaseName)
    {
        if (result == null)
        {
            Debug.LogWarning("[ValidationReportWriter] Result is null. Save skipped.");
            return;
        }

        EnsureOutputDirectoryExists();

        string reportText = BuildReportText(result, testCaseId, testCaseName);
        File.AppendAllText(reportPath, reportText);

        if (verboseLog)
        {
            Debug.Log($"[ValidationReportWriter] Saved report: {reportPath}");
        }
    }

    /// <summary>
    /// 저장용 텍스트 구성
    /// 현재 ValidationResult의 unity 필드는 actual 측 값을 담고 있으며,
    /// 현재 프로젝트에서는 FK TCP 값을 actual로 사용한다.
    /// </summary>
    private string BuildReportText(
        scr_FKValidator.ValidationResult result,
        string testCaseId,
        string testCaseName)
    {
        string testId = GenerateNextTestId();
        string runTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string failReason = GetFailReason(result);

        string report = "";
        report += "====================================================================\n";
        report += $"Run Time     : {runTime}\n";
        report += $"Test ID      : {testId}\n";
        report += $"TestCase ID  : {testCaseId}\n";
        report += $"Test Name    : {testCaseName}\n";
        report += $"Result       : {(result.isFinalPass ? "PASS" : "FAIL")}\n";
        report += $"Fail Reason  : {failReason}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[FK TCP]\n";
        report += $"Position X = {result.unityPosition.x:0.000000}\n";
        report += $"Position Y = {result.unityPosition.y:0.000000}\n";
        report += $"Position Z = {result.unityPosition.z:0.000000}\n";
        report += $"Rotation X = {result.unityRotation.x:0.000000}\n";
        report += $"Rotation Y = {result.unityRotation.y:0.000000}\n";
        report += $"Rotation Z = {result.unityRotation.z:0.000000}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[Python FK]\n";
        report += $"Position X = {result.pythonPosition.x:0.000000}\n";
        report += $"Position Y = {result.pythonPosition.y:0.000000}\n";
        report += $"Position Z = {result.pythonPosition.z:0.000000}\n";
        report += $"Rotation X = {result.pythonRotation.x:0.000000}\n";
        report += $"Rotation Y = {result.pythonRotation.y:0.000000}\n";
        report += $"Rotation Z = {result.pythonRotation.z:0.000000}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[Axis Error]\n";
        report += $"Position Error X = {result.positionErrorX:0.000000}\n";
        report += $"Position Error Y = {result.positionErrorY:0.000000}\n";
        report += $"Position Error Z = {result.positionErrorZ:0.000000}\n";
        report += $"Rotation Error X = {result.rotationErrorX:0.000000}\n";
        report += $"Rotation Error Y = {result.rotationErrorY:0.000000}\n";
        report += $"Rotation Error Z = {result.rotationErrorZ:0.000000}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[Error Magnitude]\n";
        report += $"Position Error Magnitude = {result.positionErrorMagnitude:0.000000}\n";
        report += $"Rotation Error Magnitude = {result.rotationErrorMagnitude:0.000000}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[Threshold]\n";
        report += $"Position Axis Tolerance = {positionAxisTolerance:0.000000}\n";
        report += $"Position Magnitude Tolerance = {positionMagnitudeTolerance:0.000000}\n";
        report += $"Rotation Axis Tolerance = {rotationAxisTolerance:0.000000}\n";
        report += $"Rotation Magnitude Tolerance = {rotationMagnitudeTolerance:0.000000}\n";
        report += "--------------------------------------------------------------------\n";
        report += "[Judgement Detail]\n";
        report += $"Position Check = {(result.isPositionPass ? "PASS" : "FAIL")}\n";
        report += $"Rotation Check = {(result.isRotationPass ? "PASS" : "FAIL")}\n";
        report += $"Final Check = {(result.isFinalPass ? "PASS" : "FAIL")}\n";
        report += "====================================================================\n\n";

        return report;
    }

    /// <summary>
    /// 기존 report에서 마지막 Test ID 추출
    /// </summary>
    private int LoadLastTestIdNumber()
    {
        if (!File.Exists(reportPath))
        {
            if (verboseLog)
            {
                Debug.Log("[ValidationReportWriter] validation_report.txt not found. Start from T001.");
            }

            return 0;
        }

        string reportText = File.ReadAllText(reportPath);
        MatchCollection matches = Regex.Matches(reportText, @"Test ID\s*:\s*T(\d+)");

        if (matches.Count == 0)
        {
            if (verboseLog)
            {
                Debug.Log("[ValidationReportWriter] No existing Test ID found. Start from T001.");
            }

            return 0;
        }

        int maxId = 0;

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int parsedId))
            {
                if (parsedId > maxId)
                {
                    maxId = parsedId;
                }
            }
        }

        if (verboseLog)
        {
            Debug.Log($"[ValidationReportWriter] Last Test ID found: T{maxId:D3}");
        }

        return maxId;
    }

    /// <summary>
    /// 다음 Test ID 생성
    /// </summary>
    private string GenerateNextTestId()
    {
        currentTestIdNumber++;
        return $"T{currentTestIdNumber:D3}";
    }

    /// <summary>
    /// FAIL 원인 문자열 생성
    /// </summary>
    private string GetFailReason(scr_FKValidator.ValidationResult result)
    {
        if (result == null)
        {
            return "Invalid Result";
        }

        if (result.isFinalPass)
        {
            return "None";
        }

        string reason = "";

        if (Mathf.Abs(result.positionErrorX) > positionAxisTolerance) reason += "Position X Exceeded, ";
        if (Mathf.Abs(result.positionErrorY) > positionAxisTolerance) reason += "Position Y Exceeded, ";
        if (Mathf.Abs(result.positionErrorZ) > positionAxisTolerance) reason += "Position Z Exceeded, ";
        if (result.positionErrorMagnitude > positionMagnitudeTolerance) reason += "Position Magnitude Exceeded, ";

        if (Mathf.Abs(result.rotationErrorX) > rotationAxisTolerance) reason += "Rotation X Exceeded, ";
        if (Mathf.Abs(result.rotationErrorY) > rotationAxisTolerance) reason += "Rotation Y Exceeded, ";
        if (Mathf.Abs(result.rotationErrorZ) > rotationAxisTolerance) reason += "Rotation Z Exceeded, ";
        if (result.rotationErrorMagnitude > rotationMagnitudeTolerance) reason += "Rotation Magnitude Exceeded, ";

        if (string.IsNullOrEmpty(reason))
        {
            return "Unknown";
        }

        return reason.TrimEnd(' ', ',');
    }

    /// <summary>
    /// Output 폴더 없으면 생성
    /// </summary>
    private void EnsureOutputDirectoryExists()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, outputFolderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);

            if (verboseLog)
            {
                Debug.Log($"[ValidationReportWriter] Created directory: {folderPath}");
            }
        }
    }
}