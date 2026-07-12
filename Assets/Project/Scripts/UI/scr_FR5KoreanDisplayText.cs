using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Translates user-facing FR5 UI text without changing command, topic, path, or protocol strings.
/// </summary>
public static class scr_FR5KoreanDisplayText
{
    private static readonly Dictionary<string, string> Exact =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "OPERATE", "운영" },
            { "RUNTIME", "런타임" },
            { "VALID", "검증" },
            { "VALIDATION", "검증" },
            { "IO", "I/O" },
            { "CAM", "카메라" },
            { "CAMERA", "카메라" },
            { "MODE", "모드" },
            { "LIVE", "라이브" },
            { "LIVE MODE", "라이브" },
            { "SOURCE", "소스" },
            { "ROBOT", "로봇" },
            { "LINK", "연결" },
            { "C# BRIDGE", "C# 브리지" },
            { "C SHARP BRIDGE", "C# 브리지" },
            { "ROS2", "ROS2" },
            { "REPLAY", "리플레이" },
            { "PERSP", "원근" },
            { "PERSPECTIVE", "원근" },
            { "FRONT", "정면" },
            { "FRONT VIEW", "정면" },
            { "TCP", "TCP" },
            { "EE", "EE" },
            { "RAW", "RAW" },
            { "RAW / FOCUS", "RAW" },
            { "CAMERA CONTROL", "카메라 제어" },

            { "JOINT CONTROL", "관절 제어" },
            { "GRIPPER CONTROL", "그리퍼 제어" },
            { "MANUAL JOG", "수동 조그" },
            { "MOTION", "동작 명령" },
            { "SPEED", "속도" },
            { "TOOL COORD", "툴 좌표" },
            { "HOME", "홈" },
            { "RESET", "초기화" },
            { "OPEN", "열기" },
            { "SMALL", "소형" },
            { "CLOSE", "닫기" },
            { "RETURN", "복귀" },
            { "JOG JOINT", "관절 조그" },
            { "JOG BASE", "베이스 조그" },
            { "JOG TOOL", "툴 조그" },
            { "JOG WORK", "작업좌표 조그" },
            { "MOVE J", "관절 이동" },
            { "MOVE L", "직선 이동" },
            { "MOVE C", "원호 이동" },
            { "STOP", "정지" },
            { "PAUSE", "일시정지" },
            { "RESUME", "재개" },
            { "TOOL 0", "툴 0" },
            { "USER 0", "사용자 0" },
            { "RESET OFFSET", "오프셋 초기화" },
            { "GRIPPER OPEN", "그리퍼 열기" },
            { "GRIPPER CLOSE", "그리퍼 닫기" },

            { "MONITOR", "모니터" },
            { "SUMMARY", "요약" },
            { "COMPARE", "비교" },
            { "ALARM", "알람" },
            { "TCP / DELTA", "TCP / 오차" },
            { "TCP POS", "TCP 위치" },
            { "DELTA", "오차" },
            { "TCP ROT", "TCP 회전" },
            { "SYSTEM STATUS", "시스템 상태" },
            { "STATUS", "상태" },

            { "SYSTEM LOG", "시스템 로그" },
            { "SYSTEM READY", "시스템 대기" },
            { "UI ready.", "UI 준비 완료." },
            { "UI initialized.", "UI 초기화 완료." },
            { "LOG", "로그" },
            { "COMMAND : READY", "명령 : 대기" },
            { "QUEUE : 0", "대기열 : 0" },
            { "MOTION : READY", "동작 : 대기" },
            { "COMMAND READY", "명령 대기" },
            { "MOTION READY", "동작 대기" },
            { "READY", "대기" },
            { "WAITING", "대기 중" },
            { "ROS2 WAITING", "ROS2 대기 중" },
            { "ROS2 WAITING ENDPOINT", "ROS2 Endpoint 대기 중" },
            { "ROS2 LIVE", "ROS2 수신 중" },
            { "ROS2 STALE", "ROS2 수신 지연" },
            { "ROS2 TIMEOUT", "ROS2 수신 지연" },
            { "ROS2 DISCONNECTED", "ROS2 연결 끊김" },
            { "VALID : DISCONNECTED", "검증 : 연결 대기" },
            { "DISCONNECTED", "연결 끊김" },
            { "CONNECTED", "연결됨" },
            { "TRUE", "예" },
            { "FALSE", "아니오" },

            { "Waiting for ros_tcp_endpoint and /fr5/joint_states.", "ROS TCP Endpoint와 /fr5/joint_states 대기 중입니다." },
            { "Exported current joint to Python input.", "현재 관절값을 Python 입력 파일로 저장했습니다." },
            { "Current joint exported to Python input.", "현재 관절값을 Python 입력 파일로 저장했습니다." },
            { "Python Ground Truth completed.", "Python Ground Truth 계산이 완료되었습니다." },
            { "Python ground truth validation completed.", "Python Ground Truth 계산이 완료되었습니다." },
            { "Runtime source changed.", "런타임 소스가 변경되었습니다." },
            { "Sample is invalid.", "샘플이 유효하지 않습니다." },
            { "Sample is invalid. Check bridge / robot connection.", "샘플이 유효하지 않습니다. 브리지/로봇 연결을 확인하세요." },
            { "Console cleared.", "로그를 지웠습니다." },
            { "ROS2 client is not connected.", "ROS2 연결 대기 중입니다." }
        };

    private static readonly Regex JogLoaded = new Regex(
        @"^J([1-6]) input ([+-]?\d+(?:\.\d+)?) deg loaded\. Press MOVE J to execute\.$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Localize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string exact;
        if (Exact.TryGetValue(value.Trim(), out exact))
        {
            return exact;
        }

        Match jog = JogLoaded.Match(value.Trim());
        if (jog.Success)
        {
            return $"J{jog.Groups[1].Value} 입력값을 {jog.Groups[2].Value}도 적용했습니다. 실행하려면 [관절 이동]을 누르세요.";
        }

        const string runtimeSourcePrefix = "Runtime source changed:";
        if (value.StartsWith(runtimeSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "런타임 소스가 변경되었습니다:" + value.Substring(runtimeSourcePrefix.Length);
        }

        return LocalizePrefixedValue(value);
    }

    private static string LocalizePrefixedValue(string value)
    {
        string[] prefixes =
        {
            "COMMAND : ", "QUEUE : ", "MOTION : ", "MODE : ",
            "SOURCE : ", "ROBOT : ", "VALID : ", "STATE : "
        };
        string[] koreanPrefixes =
        {
            "명령 : ", "대기열 : ", "동작 : ", "모드 : ",
            "소스 : ", "로봇 : ", "검증 : ", "상태 : "
        };

        for (int i = 0; i < prefixes.Length; i++)
        {
            if (value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
            {
                string remainder = value.Substring(prefixes[i].Length);
                return koreanPrefixes[i] + Localize(remainder);
            }
        }

        return value;
    }
}
