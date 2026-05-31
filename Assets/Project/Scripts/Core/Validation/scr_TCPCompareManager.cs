using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Python readable loader + FR5 Controller updater
///
/// 역할:
/// - python_groundtruth_readable.txt 로드
/// - Case ID 기준으로 블록 찾기
/// - TCP Position 추출
/// - RotationMatrix 3x3 추출
/// - Raw frame -> Unity frame 변환
/// - scr_FR5RobotManualController 에 Python 기준값 전달
///
/// 원칙:
/// - Python readable 형식에 맞춰 파싱한다
/// - Euler 직접 저장에 의존하지 않는다
/// - Position / Rotation Matrix 기준으로 Unity에 전달한다
/// - 좌표계 변환은 scr_FR5CoordinateMapper 기준을 사용한다
/// </summary>
public class scr_TCPCompareManager : MonoBehaviour
{
    [Header("Python File")]
    [SerializeField] private string pythonReportFileName = "python_groundtruth_readable.txt";

    [Header("FR5 Reference")]
    [SerializeField] private scr_FR5RobotManualController fr5Controller;

    [Header("Optional Marker")]
    [SerializeField] private Transform pythonTcpMarker;

    [Header("Optional Base Reference")]
    [SerializeField] private Transform comparisonBaseTransform;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private PythonCaseResult lastLoadedCaseResult = null;

    [Serializable]
    public class PythonCaseResult
    {
        public string caseId;
        public string caseName;

        // Python readable 에서 읽은 raw frame position (meter)
        public Vector3 position;

        // Python readable 에서 읽은 raw 3x3 rotation matrix
        public Matrix4x4 rotationMatrix;

        // Unity frame 으로 변환된 Euler (degree)
        public Vector3 rotationEuler;
    }

    public PythonCaseResult GetLastLoadedCaseResult()
    {
        return lastLoadedCaseResult;
    }

    public bool LoadAndApplyPythonByTestCaseId(string testCaseId)
    {
        if (fr5Controller == null)
        {
            Debug.LogError("[TCPCompareManager] FR5 Controller not assigned.");
            return false;
        }

        if (!LoadPythonResultByTestCaseId(testCaseId, out PythonCaseResult result))
        {
            fr5Controller.ClearPythonTCP();
            return false;
        }

        Vector3 convertedLocalPos = ConvertPythonToUnity(result.position);
        Vector3 convertedLocalRot = result.rotationEuler;

        fr5Controller.SetPythonTCP(convertedLocalPos, convertedLocalRot);
        lastLoadedCaseResult = result;

        ApplyPythonMarker(convertedLocalPos, convertedLocalRot);

        if (verboseLog)
        {
            Debug.Log(
                $"[TCPCompareManager] Python applied | " +
                $"Case={result.caseId} | Pos={convertedLocalPos} | Rot={convertedLocalRot}"
            );
        }

        return true;
    }

    private bool LoadPythonResultByTestCaseId(string targetCaseId, out PythonCaseResult result)
    {
        result = null;

        string filePath = Path.Combine(
            Application.streamingAssetsPath,
            "Output",
            pythonReportFileName
        );

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[TCPCompareManager] Python report file not found: {filePath}");
            return false;
        }

        string fullText = File.ReadAllText(filePath);

        if (!TryFindCaseBlock(fullText, targetCaseId, out string caseBlock))
        {
            Debug.LogError($"[TCPCompareManager] Failed to find case block: {targetCaseId}");
            return false;
        }

        if (!TryParsePythonCaseBlock(caseBlock, out result))
        {
            Debug.LogError($"[TCPCompareManager] Failed to parse case block: {targetCaseId}");
            Debug.LogError(caseBlock);
            return false;
        }

        return true;
    }

    private bool TryFindCaseBlock(string fullText, string targetCaseId, out string caseBlock)
    {
        caseBlock = string.Empty;

        if (string.IsNullOrWhiteSpace(fullText) || string.IsNullOrWhiteSpace(targetCaseId))
        {
            return false;
        }

        string[] blocks = fullText.Split(
            new string[] { "-----------------------------------" },
            StringSplitOptions.RemoveEmptyEntries
        );

        string normalizedTarget = NormalizeCaseId(targetCaseId);

        foreach (string rawBlock in blocks)
        {
            string block = rawBlock.Trim();

            if (string.IsNullOrWhiteSpace(block))
            {
                continue;
            }

            if (TryExtractCaseIdFromBlock(block, out string foundCaseId))
            {
                if (NormalizeCaseId(foundCaseId) == normalizedTarget)
                {
                    caseBlock = block;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryExtractCaseIdFromBlock(string block, out string caseId)
    {
        caseId = string.Empty;

        string[] lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.StartsWith("ID=", StringComparison.OrdinalIgnoreCase))
            {
                caseId = line.Substring("ID=".Length).Trim();
                return !string.IsNullOrWhiteSpace(caseId);
            }

            if (line.StartsWith("Case ID", StringComparison.OrdinalIgnoreCase))
            {
                int idx = line.IndexOf(':');
                if (idx >= 0)
                {
                    caseId = line.Substring(idx + 1).Trim();
                    return !string.IsNullOrWhiteSpace(caseId);
                }
            }
        }

        return false;
    }

    private bool TryParsePythonCaseBlock(string caseBlock, out PythonCaseResult result)
    {
        result = new PythonCaseResult();

        string[] lines = caseBlock.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        bool hasCaseId = false;
        bool hasCaseName = false;
        bool hasPosX = false;
        bool hasPosY = false;
        bool hasPosZ = false;

        float posX = 0f;
        float posY = 0f;
        float posZ = 0f;

        int rotationRowCount = 0;
        float[,] r = new float[3, 3];
        bool inRotationMatrixSection = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Equals("[RotationMatrix]", StringComparison.OrdinalIgnoreCase))
            {
                inRotationMatrixSection = true;
                continue;
            }

            if (line.StartsWith("[") && !line.Equals("[RotationMatrix]", StringComparison.OrdinalIgnoreCase))
            {
                inRotationMatrixSection = false;
            }

            if (line.StartsWith("ID=", StringComparison.OrdinalIgnoreCase))
            {
                result.caseId = line.Substring("ID=".Length).Trim();
                hasCaseId = true;
                continue;
            }

            if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
            {
                result.caseName = line.Substring("Name=".Length).Trim();
                hasCaseName = true;
                continue;
            }

            if (TryParseLabeledFloat(line, "TCP_Position_X=", out float parsedPosX))
            {
                posX = parsedPosX;
                hasPosX = true;
                continue;
            }

            if (TryParseLabeledFloat(line, "TCP_Position_Y=", out float parsedPosY))
            {
                posY = parsedPosY;
                hasPosY = true;
                continue;
            }

            if (TryParseLabeledFloat(line, "TCP_Position_Z=", out float parsedPosZ))
            {
                posZ = parsedPosZ;
                hasPosZ = true;
                continue;
            }

            if (inRotationMatrixSection && rotationRowCount < 3)
            {
                if (TryParseMatrixRow(line, out float a, out float b, out float c))
                {
                    r[rotationRowCount, 0] = a;
                    r[rotationRowCount, 1] = b;
                    r[rotationRowCount, 2] = c;
                    rotationRowCount++;
                    continue;
                }
            }
        }

        if (!hasCaseId || !hasCaseName || !hasPosX || !hasPosY || !hasPosZ || rotationRowCount != 3)
        {
            return false;
        }

        result.position = new Vector3(posX, posY, posZ);
        result.rotationMatrix = BuildRawMatrixFrom3x3(r);

        Quaternion rawRotation = ExtractRotation(result.rotationMatrix);
        result.rotationEuler = scr_FR5CoordinateMapper.ConvertFrBasisQuaternionToUnityEuler(
            rawRotation
        );

        return true;
    }

    private bool TryParseLabeledFloat(string line, string prefix, out float value)
    {
        value = 0f;

        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string numberText = line.Substring(prefix.Length).Trim();

        return float.TryParse(
            numberText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value
        );
    }

    private bool TryParseMatrixRow(string line, out float a, out float b, out float c)
    {
        a = 0f;
        b = 0f;
        c = 0f;

        string[] split = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length != 3)
        {
            return false;
        }

        return
            float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a) &&
            float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b) &&
            float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out c);
    }

    /// <summary>
    /// Python raw 3x3 rotation matrix 구성
    /// column 0 = right
    /// column 1 = up
    /// column 2 = forward
    /// </summary>
    private Matrix4x4 BuildRawMatrixFrom3x3(float[,] r)
    {
        Matrix4x4 m = Matrix4x4.identity;

        m.m00 = r[0, 0];
        m.m01 = r[0, 1];
        m.m02 = r[0, 2];

        m.m10 = r[1, 0];
        m.m11 = r[1, 1];
        m.m12 = r[1, 2];

        m.m20 = r[2, 0];
        m.m21 = r[2, 1];
        m.m22 = r[2, 2];

        return m;
    }

    private Quaternion ExtractRotation(Matrix4x4 m)
    {
        Vector3 forward = new Vector3(m.m02, m.m12, m.m22);
        Vector3 up = new Vector3(m.m01, m.m11, m.m21);

        if (forward.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, up.normalized);
    }

    private string NormalizeCaseId(string rawCaseId)
    {
        if (string.IsNullOrWhiteSpace(rawCaseId))
        {
            return string.Empty;
        }

        string trimmed = rawCaseId.Trim().ToUpperInvariant();

        if (trimmed.StartsWith("T"))
        {
            trimmed = trimmed.Substring(1);
        }

        trimmed = trimmed.TrimStart('0');

        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = "0";
        }

        return trimmed;
    }

    private Vector3 ConvertPythonToUnity(Vector3 pythonRawPositionMeters)
    {
        return scr_FR5CoordinateMapper.ConvertFrPositionMetersToUnity(
            pythonRawPositionMeters
        );
    }

    private void ApplyPythonMarker(Vector3 localPosition, Vector3 localEuler)
    {
        if (pythonTcpMarker == null)
        {
            return;
        }

        if (comparisonBaseTransform != null)
        {
            pythonTcpMarker.localPosition = localPosition;
            pythonTcpMarker.localRotation = Quaternion.Euler(localEuler);
        }
        else
        {
            pythonTcpMarker.position = localPosition;
            pythonTcpMarker.rotation = Quaternion.Euler(localEuler);
        }
    }
}