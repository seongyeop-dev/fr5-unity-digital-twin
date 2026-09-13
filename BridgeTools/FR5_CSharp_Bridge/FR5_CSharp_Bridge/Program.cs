using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using fairino;

namespace FR5_CSharp_Bridge
{
    internal class Program
    {
        private const string DefaultRobotIp = "192.168.58.2";
        private const int DefaultPollIntervalMilliseconds = 200;
        private const int MockMinimumPollIntervalMilliseconds = 200;
        private const int JsonWriteRetryCount = 5;
        private const string DefaultOutputFileName = "fr5_live_state.json";
        private static readonly string UnityBridgeFolderPath = ResolveUnityBridgeFolderPath();
        private static bool _isRunning = true;

        private static void Main(string[] args)
        {
            bool mockMode = Array.Exists(args, arg => string.Equals(arg, "--mock", StringComparison.OrdinalIgnoreCase));
            BridgeConfig config = LoadConfig();
            string outputPath = Path.Combine(UnityBridgeFolderPath, config.OutputFileName);

            Console.WriteLine("==========================================");
            Console.WriteLine("FR5 C# SDK Bridge Start");
            Console.WriteLine("READ-ONLY SDK FEEDBACK MODE");
            Console.WriteLine("==========================================");
            Console.WriteLine("Mode: " + (mockMode ? "Mock (no robot SDK connection)" : "SDK read-only"));
            Console.WriteLine("Robot IP: " + config.RobotIp);
            Console.WriteLine("Poll interval: " + config.PollIntervalMilliseconds + " ms");
            Console.WriteLine("Read-only config: " + config.ReadOnly);
            Console.WriteLine("Output path: " + outputPath);

            if (!config.ReadOnly)
            {
                Console.WriteLine("Configuration rejected: readOnly must be true.");
                return;
            }

            Directory.CreateDirectory(UnityBridgeFolderPath);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _isRunning = false;
                Console.WriteLine("Stopping bridge...");
            };

            if (mockMode)
            {
                RunMockLoop(outputPath, config.PollIntervalMilliseconds);
                return;
            }

            RunReadOnlySdkLoop(outputPath, config);
        }

        private static void RunReadOnlySdkLoop(string outputPath, BridgeConfig config)
        {
            Robot robot = new Robot();

            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                Directory.CreateDirectory(logPath);
                robot.LoggerInit(FrLogType.BUFFER, FrLogLevel.INFO, logPath + "\\", 5, 5);
                robot.SetReconnectParam(true, 100, 1000);

                Console.WriteLine("Connecting to robot: " + config.RobotIp);
                int rpcRet = robot.RPC(config.RobotIp);
                Console.WriteLine("RPC Result = " + rpcRet);
                if (rpcRet != 0)
                {
                    Console.WriteLine("RPC connection failed. Writing invalid feedback only.");
                    WriteInvalidJson(outputPath, "CSharpBridge");
                    return;
                }

                int sdkState = -1;
                int sdkStateRet = robot.GetSDKComState(ref sdkState);
                Console.WriteLine("GetSDKComState Return = " + sdkStateRet);
                Console.WriteLine("GetSDKComState State = " + sdkState);
                if (sdkStateRet != 0 || sdkState != 0)
                {
                    Console.WriteLine("SDK communication state is abnormal. Writing invalid feedback only.");
                    WriteInvalidJson(outputPath, "CSharpBridge");
                    return;
                }

                Console.WriteLine("Robot connected. Polling read-only joint feedback; press Ctrl+C to stop.");
                while (_isRunning)
                {
                    try
                    {
                        WriteJsonAtomically(outputPath, BuildRobotStateJson(robot));
                    }
                    catch (Exception loopEx)
                    {
                        Console.WriteLine("[Loop Error] " + loopEx.Message);
                        WriteInvalidJson(outputPath, "CSharpBridge");
                    }

                    Thread.Sleep(config.PollIntervalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Fatal Error] " + ex.Message);
                WriteInvalidJson(outputPath, "CSharpBridge");
            }
            finally
            {
                try { robot.CloseRPC(); } catch { }
                Console.WriteLine("Bridge closed.");
            }
        }

        private static void RunMockLoop(string outputPath, int pollIntervalMilliseconds)
        {
            int effectivePollIntervalMilliseconds = Math.Max(
                pollIntervalMilliseconds,
                MockMinimumPollIntervalMilliseconds
            );
            Console.WriteLine("Mock feedback active. No Robot.RPC call is made. Press Ctrl+C to stop.");
            Console.WriteLine("Mock poll interval: " + effectivePollIntervalMilliseconds + " ms");
            DateTime startTime = DateTime.UtcNow;
            while (_isRunning)
            {
                double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                WriteJsonAtomically(outputPath, BuildMockStateJson(elapsedSeconds));
                Thread.Sleep(effectivePollIntervalMilliseconds);
            }
            Console.WriteLine("Mock bridge closed.");
        }

        private static string BuildRobotStateJson(Robot robot)
        {
            byte flag = 0;
            JointPos jointPos = new JointPos(0, 0, 0, 0, 0, 0);
            int retJoint = robot.GetActualJointPosDegree(flag, ref jointPos);
            bool isValid = retJoint == 0;

            if (isValid)
            {
                Console.WriteLine("[OK] Joint0=" + jointPos.jPos[0].ToString("F3", CultureInfo.InvariantCulture));
            }
            else
            {
                Console.WriteLine("[Read Fail] Joint=" + retJoint);
            }

            return BuildStateJson(isValid, "CSharpBridge", isValid ? "Connected" : "Disconnected", jointPos.jPos);
        }

        private static string BuildMockStateJson(double elapsedSeconds)
        {
            double[] joints = new double[6];
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = Math.Sin(elapsedSeconds * 0.8 + i * 0.55) * (8.0 + i * 2.0);
            }
            return BuildStateJson(true, "CSharpBridgeMock", "Mock", joints);
        }

        private static string BuildStateJson(bool isValid, string source, string robotState, double[] joints)
        {
            double timestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"isValid\": " + ToJsonBool(isValid) + ",");
            sb.AppendLine("  \"source\": \"" + source + "\",");
            sb.AppendLine("  \"timestampSeconds\": " + ToJsonDouble(timestampSeconds) + ",");
            sb.AppendLine("  \"jointDegrees\": [" + ToJsonDouble(joints[0]) + ", " + ToJsonDouble(joints[1]) + ", " + ToJsonDouble(joints[2]) + ", " + ToJsonDouble(joints[3]) + ", " + ToJsonDouble(joints[4]) + ", " + ToJsonDouble(joints[5]) + "],");
            sb.AppendLine("  \"flangePositionMillimeters\": [0, 0, 0],");
            sb.AppendLine("  \"flangeRotationDegrees\": [0, 0, 0],");
            sb.AppendLine("  \"tcpPositionMillimeters\": [0, 0, 0],");
            sb.AppendLine("  \"tcpRotationDegrees\": [0, 0, 0],");
            sb.AppendLine("  \"toolIndex\": 0,");
            sb.AppendLine("  \"userIndex\": 0,");
            sb.AppendLine("  \"robotMode\": \"Unknown\",");
            sb.AppendLine("  \"robotState\": \"" + robotState + "\",");
            sb.AppendLine("  \"alarmCode\": 0");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteInvalidJson(string outputPath, string source)
        {
            WriteJsonAtomically(outputPath, BuildStateJson(false, source, "Disconnected", new double[6]));
        }

        private static BridgeConfig LoadConfig()
        {
            string configPath = ResolveConfigPath();
            if (configPath == null)
            {
                Console.WriteLine("Config file not found. Using built-in defaults.");
                return BridgeConfig.CreateDefaults();
            }

            try
            {
                using (FileStream stream = File.OpenRead(configPath))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BridgeConfig));
                    BridgeConfig config = (BridgeConfig)serializer.ReadObject(stream);
                    config.ApplyDefaults();
                    Console.WriteLine("Config loaded: " + configPath);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Config load failed. Using built-in defaults. " + ex.Message);
                return BridgeConfig.CreateDefaults();
            }
        }

        private static string ResolveConfigPath()
        {
            DirectoryInfo current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                string candidate = Path.Combine(current.FullName, "config", "fr5_bridge_config.json");
                if (File.Exists(candidate)) return candidate;
                current = current.Parent;
            }
            return null;
        }

        private static void WriteJsonAtomically(string outputPath, string json)
        {
            for (int attempt = 1; attempt <= JsonWriteRetryCount; attempt++)
            {
                string tempPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(tempPath, json, Encoding.UTF8);
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                    File.Move(tempPath, outputPath);
                    return;
                }
                catch (IOException ex)
                {
                    Console.WriteLine(
                        "[Write Warning] JSON write attempt " + attempt + "/" + JsonWriteRetryCount +
                        " failed: " + ex.Message
                    );
                    Thread.Sleep(20 * attempt);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            Console.WriteLine("[Write Warning] JSON write retries exhausted. Bridge will continue polling.");
        }

        private static string ToJsonDouble(double value) { return value.ToString("0.######", CultureInfo.InvariantCulture); }
        private static string ToJsonBool(bool value) { return value ? "true" : "false"; }

        private static string ResolveUnityBridgeFolderPath()
        {
            DirectoryInfo current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                string streamingAssetsPath = Path.Combine(current.FullName, "Unity", "FAIRINO_FR5_DigitalTwin", "Assets", "StreamingAssets");
                if (Directory.Exists(streamingAssetsPath)) return Path.Combine(streamingAssetsPath, "Bridge");
                // The public Unity repository has Assets directly at its root.
                string repositoryAssetsPath = Path.Combine(current.FullName, "Assets", "StreamingAssets");
                if (Directory.Exists(repositoryAssetsPath)) return Path.Combine(repositoryAssetsPath, "Bridge");
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Could not find Assets/StreamingAssets in the public or development repository layout.");
        }
    }

    [DataContract]
    internal class BridgeConfig
    {
        [DataMember(Name = "robotIp")] public string RobotIp;
        [DataMember(Name = "pollIntervalMs")] public int PollIntervalMilliseconds;
        [DataMember(Name = "readOnly")] public bool ReadOnly;
        [DataMember(Name = "outputFileName")] public string OutputFileName;

        internal static BridgeConfig CreateDefaults()
        {
            return new BridgeConfig { RobotIp = "192.168.58.2", PollIntervalMilliseconds = 200, ReadOnly = true, OutputFileName = "fr5_live_state.json" };
        }

        internal void ApplyDefaults()
        {
            if (string.IsNullOrWhiteSpace(RobotIp)) RobotIp = "192.168.58.2";
            if (PollIntervalMilliseconds <= 0) PollIntervalMilliseconds = 200;
            if (string.IsNullOrWhiteSpace(OutputFileName)) OutputFileName = "fr5_live_state.json";
            OutputFileName = Path.GetFileName(OutputFileName);
        }
    }
}
