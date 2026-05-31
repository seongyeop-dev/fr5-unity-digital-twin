using System.Diagnostics;
using System.IO;
using System.Globalization;
using UnityEngine;

public class scr_PythonAutoRunner : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private scr_FR5RobotManualController fr5Controller;
    [SerializeField] private scr_TCPCompareManager tcpCompareManager;
    [SerializeField] private scr_FR5ValidationManager validationManager;

    [Header("Python Execution")]
    [SerializeField] private string pythonModuleName = "Phase1_Kinematics.GroundTruth.groundtruth_single_runner";
    [SerializeField] private string pythonWorkingDirectoryRelative = "../../Python";

    [Header("Live IO")]
    [SerializeField] private string liveJointFileName = "current_joint.txt";
    [SerializeField] private string liveCaseId = "LIVE";
    [SerializeField] private string liveCaseName = "UnityLive";

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [ContextMenu("Run Live Validation")]
    public void RunLiveValidationFromInspector()
    {
        RunLiveValidation();
    }

    public bool RunLiveValidation()
    {
        if (!ValidateReferences())
        {
            return false;
        }

        bool saved = SaveCurrentJointToStreamingAssets();

        if (!saved)
        {
            return false;
        }

        bool pythonExecuted = ExecutePythonSingleRunner();

        if (!pythonExecuted)
        {
            return false;
        }

        bool loaded = tcpCompareManager.LoadAndApplyPythonByTestCaseId(liveCaseId);

        if (!loaded)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] Failed to load LIVE Python result.");
            return false;
        }

        validationManager.ValidateCurrentCase(liveCaseId, liveCaseName);

        if (verboseLog)
        {
            UnityEngine.Debug.Log("[PythonAutoRunner] Live validation completed.");
        }

        return true;
    }

    private bool SaveCurrentJointToStreamingAssets()
    {
        float[] joints = fr5Controller.GetCurrentJointArray();

        if (joints == null || joints.Length != 6)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] Invalid joint array.");
            return false;
        }

        string inputDir = Path.Combine(Application.streamingAssetsPath, "Input");
        Directory.CreateDirectory(inputDir);

        string filePath = Path.Combine(inputDir, liveJointFileName);

        string[] lines = new string[6];
        for (int i = 0; i < 6; i++)
        {
            lines[i] = joints[i].ToString("F6", CultureInfo.InvariantCulture);
        }

        File.WriteAllLines(filePath, lines);

        if (verboseLog)
        {
            UnityEngine.Debug.Log($"[PythonAutoRunner] Saved live joint file: {filePath}");
        }

        return true;
    }

    private bool ExecutePythonSingleRunner()
    {
        try
        {
            string pythonExe = FindPythonExecutable();

            if (string.IsNullOrWhiteSpace(pythonExe))
            {
                UnityEngine.Debug.LogError("[PythonAutoRunner] Python executable not found.");
                return false;
            }

            string workingDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, pythonWorkingDirectoryRelative)
            );

            if (!Directory.Exists(workingDirectory))
            {
                UnityEngine.Debug.LogError($"[PythonAutoRunner] Python working directory not found: {workingDirectory}");
                return false;
            }

            Process process = new Process();

            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = $"-m {pythonModuleName}";
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (verboseLog && !string.IsNullOrWhiteSpace(stdout))
            {
                UnityEngine.Debug.Log("[PythonAutoRunner][STDOUT]\n" + stdout);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                UnityEngine.Debug.LogError("[PythonAutoRunner][STDERR]\n" + stderr);
            }

            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"[PythonAutoRunner] Python process failed. ExitCode={process.ExitCode}");
                return false;
            }

            if (verboseLog)
            {
                UnityEngine.Debug.Log($"[PythonAutoRunner] Python single runner executed successfully. Executable={pythonExe}");
            }

            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] ExecutePythonSingleRunner failed: " + ex.Message);
            return false;
        }
    }

    private string FindPythonExecutable()
    {
        string userName = System.Environment.UserName;

        string[] candidates =
        {
            "python",
            "python3",
            "py",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            @"C:\Python39\python.exe",
            $@"C:\Users\{userName}\AppData\Local\Programs\Python\Python312\python.exe",
            $@"C:\Users\{userName}\AppData\Local\Programs\Python\Python311\python.exe",
            $@"C:\Users\{userName}\AppData\Local\Programs\Python\Python310\python.exe",
            $@"C:\Users\{userName}\AppData\Local\Programs\Python\Python39\python.exe"
        };

        foreach (string candidate in candidates)
        {
            try
            {
                Process probe = new Process();
                probe.StartInfo.FileName = candidate;
                probe.StartInfo.Arguments = "--version";
                probe.StartInfo.CreateNoWindow = true;
                probe.StartInfo.UseShellExecute = false;
                probe.StartInfo.RedirectStandardOutput = true;
                probe.StartInfo.RedirectStandardError = true;

                probe.Start();
                probe.WaitForExit(1000);

                if (probe.ExitCode == 0)
                {
                    if (verboseLog)
                    {
                        UnityEngine.Debug.Log($"[PythonAutoRunner] Python executable found: {candidate}");
                    }

                    return candidate;
                }
            }
            catch
            {
                // 다음 후보 계속 탐색
            }
        }

        return null;
    }

    private bool ValidateReferences()
    {
        if (fr5Controller == null)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] FR5 Controller is not assigned.");
            return false;
        }

        if (tcpCompareManager == null)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] TCP Compare Manager is not assigned.");
            return false;
        }

        if (validationManager == null)
        {
            UnityEngine.Debug.LogError("[PythonAutoRunner] Validation Manager is not assigned.");
            return false;
        }

        return true;
    }
}