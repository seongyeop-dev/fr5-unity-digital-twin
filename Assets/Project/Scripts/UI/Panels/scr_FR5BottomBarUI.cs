using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// FR5 하단 시스템 로그 UI
///
/// 역할:
/// 1. BottomBar에 마지막 시스템 메시지를 표시한다.
/// 2. Console 패널에 최근 로그를 누적 표시한다.
/// 3. LOG 버튼으로 Console 패널을 열고 닫는다.
/// 4. Close 버튼으로 Console 패널을 닫는다.
/// 5. RectTransform, Anchor, 위치, 크기는 절대 수정하지 않는다.
/// </summary>
public class scr_FR5BottomBarUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;
    [SerializeField] private scr_FR5CSharpBridgeClient cSharpBridgeClient;
    [SerializeField] private scr_FR5RobotManualController robotController;

    [Header("BottomBar 텍스트")]
    [SerializeField] private TMP_Text logTitleText;
    [SerializeField] private TMP_Text logMessageText;
    [SerializeField] private TMP_Text currentModeText;
    [SerializeField] private TMP_Text currentSourceText;
    [SerializeField] private TMP_Text systemStateText;

    [Header("Console 패널")]
    [SerializeField] private GameObject bottomConsolePanel;
    [SerializeField] private TMP_Text consoleLogText;
    [SerializeField] private TMP_Text toggleConsoleButtonText;
    [SerializeField] private TMP_Text closeConsoleButtonText;

    [Header("표시 문구")]
    [SerializeField] private string logTitle = "SYSTEM LOG";
    [SerializeField] private string defaultIdleMessage = "SYSTEM READY";
    [SerializeField] private string openConsoleButtonLabel = "LOG";
    [SerializeField] private string closeConsoleButtonLabel = "CLOSE";
    [SerializeField] private float transientMessageHoldSeconds = 2.0f;

    [Header("Console 옵션")]
    [SerializeField] private bool consoleVisibleOnStart = false;
    [SerializeField] private int maxConsoleLines = 40;
    [SerializeField] private bool showTimeInConsole = true;

    [Header("갱신 옵션")]
    [SerializeField] private bool autoRefreshInUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.1f;

    private readonly List<string> consoleLines = new List<string>();

    private float nextRefreshTime = 0f;
    private string transientMessage = string.Empty;
    private float transientMessageExpireTime = -1f;
    private string lastDisplayedMessage = string.Empty;

    private void Start()
    {
        SetConsoleVisible(consoleVisibleOnStart);
        AddConsoleLine(defaultIdleMessage);
        RefreshUI();
    }

    private void Update()
    {
        if (!autoRefreshInUpdate)
        {
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshIntervalSeconds;
        RefreshUI();
    }

    public void SetLogMessage(string message)
    {
        SetLogMessage(message, transientMessageHoldSeconds);
    }

    public void SetLogMessage(string message, float holdSeconds)
    {
        string resolvedMessage = string.IsNullOrWhiteSpace(message)
            ? defaultIdleMessage
            : message;

        transientMessage = resolvedMessage;
        transientMessageExpireTime = Time.time + Mathf.Max(holdSeconds, 0.1f);

        AddConsoleLine(resolvedMessage);
        RefreshUI();
    }

    public void ClearTransientLogMessage()
    {
        transientMessage = string.Empty;
        transientMessageExpireTime = -1f;
        RefreshUI();
    }

    public void ToggleConsole()
    {
        bool nextState = bottomConsolePanel == null || !bottomConsolePanel.activeSelf;
        SetConsoleVisible(nextState);
        RefreshUI();
    }

    public void OpenConsole()
    {
        SetConsoleVisible(true);
        RefreshUI();
    }

    public void CloseConsole()
    {
        SetConsoleVisible(false);
        RefreshUI();
    }

    public void ClearConsole()
    {
        consoleLines.Clear();
        AddConsoleLine("Console cleared.");
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (logTitleText != null)
        {
            logTitleText.text = logTitle;
        }

        if (currentModeText != null)
        {
            currentModeText.text = $"MODE : {GetModeLabel()}";
        }

        if (currentSourceText != null)
        {
            currentSourceText.text = $"SOURCE : {GetSourceLabel()}";
        }

        if (systemStateText != null)
        {
            systemStateText.text = $"STATE : {GetSystemStateLabel()}";
        }

        string resolvedLogMessage = GetResolvedLogMessage();

        if (logMessageText != null)
        {
            logMessageText.text = resolvedLogMessage;
        }

        // Runtime에서 자동으로 들어오는 메시지는 같은 문구를 계속 누적하지 않도록 제한한다.
        if (!string.IsNullOrWhiteSpace(resolvedLogMessage) &&
            resolvedLogMessage != lastDisplayedMessage &&
            resolvedLogMessage != defaultIdleMessage)
        {
            AddConsoleLine(resolvedLogMessage);
            lastDisplayedMessage = resolvedLogMessage;
        }

        RefreshConsoleText();
        RefreshConsoleButtonLabels();
    }

    private void SetConsoleVisible(bool visible)
    {
        if (bottomConsolePanel == null)
        {
            return;
        }

        bottomConsolePanel.SetActive(visible);
    }

    private void RefreshConsoleButtonLabels()
    {
        if (toggleConsoleButtonText != null)
        {
            toggleConsoleButtonText.text = openConsoleButtonLabel;
        }

        if (closeConsoleButtonText != null)
        {
            closeConsoleButtonText.text = closeConsoleButtonLabel;
        }
    }

    private void AddConsoleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string line = showTimeInConsole
            ? $"[{System.DateTime.Now:HH:mm:ss}] {message}"
            : message;

        consoleLines.Add(line);

        while (consoleLines.Count > maxConsoleLines)
        {
            consoleLines.RemoveAt(0);
        }

        RefreshConsoleText();
    }

    private void RefreshConsoleText()
    {
        if (consoleLogText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < consoleLines.Count; i++)
        {
            builder.AppendLine(consoleLines[i]);
        }

        consoleLogText.text = builder.ToString();
    }

    private string GetModeLabel()
    {
        if (runtimeSyncManager == null)
        {
            return "N/A";
        }

        return runtimeSyncManager.GetFormattedModeLabel();
    }

    private string GetSourceLabel()
    {
        if (runtimeSyncManager == null)
        {
            return "N/A";
        }

        return runtimeSyncManager.GetFormattedSourceLabel();
    }

    private string GetSystemStateLabel()
    {
        string controllerState = robotController != null
            ? robotController.GetCurrentStateLabel()
            : "Ready";

        if (runtimeSyncManager == null)
        {
            return controllerState.ToUpperInvariant();
        }

        return runtimeSyncManager.GetEffectiveSystemStateLabel(controllerState);
    }

    private string GetResolvedLogMessage()
    {
        if (!string.IsNullOrWhiteSpace(transientMessage) && Time.time <= transientMessageExpireTime)
        {
            return transientMessage;
        }

        if (runtimeSyncManager == null)
        {
            return defaultIdleMessage;
        }

        if (runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.CSharpBridge &&
            cSharpBridgeClient != null)
        {
            string bridgeMessage = cSharpBridgeClient.GetDisplayMessageLabel(runtimeSyncManager.LastSyncMessage);

            if (!string.IsNullOrWhiteSpace(bridgeMessage) &&
                bridgeMessage != "Bridge ready.")
            {
                return bridgeMessage;
            }
        }

        string runtimeMessage = runtimeSyncManager.GetEffectiveMessageLabel();

        if (!string.IsNullOrWhiteSpace(runtimeMessage) &&
            runtimeMessage != "Idle" &&
            runtimeMessage != "None")
        {
            return runtimeMessage;
        }

        return defaultIdleMessage;
    }
}