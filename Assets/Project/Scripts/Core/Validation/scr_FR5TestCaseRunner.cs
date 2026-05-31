using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// FR5 Test Case Runner
///
/// 역할:
/// - test_case.txt 파싱
/// - joint 적용
/// - Python 기준값 로드
/// - validation 실행
/// </summary>
public class scr_FR5TestCaseRunner : MonoBehaviour
{
    [Header("FR5 References")]
    [SerializeField] private scr_FR5RobotManualController fr5ManualController;
    [SerializeField] private scr_TCPCompareManager tcpCompareManager;
    [SerializeField] private scr_FR5ValidationManager validationManager;

    [Header("Reference Case Options")]
    [SerializeField] private bool loadReferenceCaseOnStart = true;
    [SerializeField] private string referenceTestCaseIdOnStart = "T001";

    [Header("Test Replay Options")]
    [SerializeField] private bool replayAllTestCasesOnPlay = false;
    [SerializeField] private bool resetToHomeBeforeReplay = true;
    [SerializeField] private float replayStartDelaySeconds = 0.3f;

    [Header("Execution Delay")]
    [SerializeField] private float applyDelaySeconds = 0.05f;
    [SerializeField] private float compareDelaySeconds = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private readonly List<FR5TestCase> loadedTestCases = new List<FR5TestCase>();
    private bool isRunning = false;

    public IReadOnlyList<FR5TestCase> LoadedTestCases => loadedTestCases;
    public bool IsRunning => isRunning;

    private void Start()
    {
        LoadTestCases();

        if (loadReferenceCaseOnStart && tcpCompareManager != null)
        {
            tcpCompareManager.LoadAndApplyPythonByTestCaseId(referenceTestCaseIdOnStart);
        }

        if (replayAllTestCasesOnPlay)
        {
            StartCoroutine(RunAllTests());
        }
    }

    [ContextMenu("Load Test Cases")]
    public void LoadTestCases()
    {
        loadedTestCases.Clear();

        string inputPath = BuildInputPath();

        if (!File.Exists(inputPath))
        {
            Debug.LogError($"[FR5TestCaseRunner] test_case.txt not found: {inputPath}");
            return;
        }

        string fileText = File.ReadAllText(inputPath);
        loadedTestCases.AddRange(ParseTestCaseFile(fileText));

        if (verboseLog)
        {
            Debug.Log($"[FR5TestCaseRunner] Loaded Test Cases Count: {loadedTestCases.Count}");
        }
    }

    [ContextMenu("Run All Tests")]
    public void RunAllTestsFromInspector()
    {
        StartCoroutine(RunAllTests());
    }

    [ContextMenu("Run First Test")]
    public void RunFirstTestFromInspector()
    {
        StartCoroutine(RunSingleTestByIndex(0));
    }

    public IEnumerator RunAllTests()
    {
        if (isRunning) yield break;
        if (!ValidateReferences()) yield break;

        if (loadedTestCases.Count == 0)
        {
            LoadTestCases();
        }

        if (loadedTestCases.Count == 0)
        {
            Debug.LogWarning("[FR5TestCaseRunner] No test cases loaded.");
            yield break;
        }

        isRunning = true;

        if (resetToHomeBeforeReplay)
        {
            fr5ManualController.MoveToHome();
        }

        if (replayStartDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(replayStartDelaySeconds);
        }

        foreach (FR5TestCase testCase in loadedTestCases)
        {
            yield return ExecuteTestCase(testCase);
        }

        isRunning = false;
    }

    public IEnumerator RunSingleTestByIndex(int index)
    {
        if (isRunning) yield break;
        if (index < 0 || index >= loadedTestCases.Count) yield break;

        isRunning = true;
        yield return ExecuteTestCase(loadedTestCases[index]);
        isRunning = false;
    }

    private IEnumerator ExecuteTestCase(FR5TestCase testCase)
    {
        if (testCase == null) yield break;

        fr5ManualController.ApplyJointAngles(
            testCase.J1,
            testCase.J2,
            testCase.J3,
            testCase.J4,
            testCase.J5,
            testCase.J6
        );

        yield return new WaitForSeconds(applyDelaySeconds);

        bool pythonLoaded = tcpCompareManager.LoadAndApplyPythonByTestCaseId(testCase.Id);

        if (!pythonLoaded)
        {
            Debug.LogError($"[FR5TestCaseRunner] Python load failed: {testCase.Id}");
            yield break;
        }

        yield return new WaitForSeconds(compareDelaySeconds);

        validationManager.ValidateCurrentCase(testCase.Id, testCase.Name);
    }

    private bool ValidateReferences()
    {
        if (fr5ManualController == null)
        {
            Debug.LogError("[FR5TestCaseRunner] FR5 Manual Controller is not assigned.");
            return false;
        }

        if (tcpCompareManager == null)
        {
            Debug.LogError("[FR5TestCaseRunner] TCP Compare Manager is not assigned.");
            return false;
        }

        if (validationManager == null)
        {
            Debug.LogError("[FR5TestCaseRunner] Validation Manager is not assigned.");
            return false;
        }

        return true;
    }

    private string BuildInputPath()
    {
        return Path.Combine(Application.streamingAssetsPath, "Input", "test_case.txt");
    }

    private List<FR5TestCase> ParseTestCaseFile(string text)
    {
        List<FR5TestCase> results = new List<FR5TestCase>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        FR5TestCase current = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("//") || line.StartsWith("#")) continue;

            if (line.Equals("[TestCase]", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null && current.IsValid())
                {
                    results.Add(current);
                }

                current = new FR5TestCase();
                continue;
            }

            if (current == null) continue;

            int idx = line.IndexOf('=');
            if (idx < 0) continue;

            string key = line.Substring(0, idx).Trim();
            string value = line.Substring(idx + 1).Trim();

            switch (key)
            {
                case "ID": current.Id = value; break;
                case "Name": current.Name = value; break;
                case "J1": current.J1 = ParseFloat(value); break;
                case "J2": current.J2 = ParseFloat(value); break;
                case "J3": current.J3 = ParseFloat(value); break;
                case "J4": current.J4 = ParseFloat(value); break;
                case "J5": current.J5 = ParseFloat(value); break;
                case "J6": current.J6 = ParseFloat(value); break;
            }
        }

        if (current != null && current.IsValid())
        {
            results.Add(current);
        }

        return results;
    }

    private float ParseFloat(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            return parsed;
        }

        return 0f;
    }

    [Serializable]
    public class FR5TestCase
    {
        public string Id;
        public string Name;
        public float J1;
        public float J2;
        public float J3;
        public float J4;
        public float J5;
        public float J6;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name);
        }
    }
}