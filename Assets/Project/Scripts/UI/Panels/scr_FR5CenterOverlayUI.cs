using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 Center Overlay UI
///
/// Role:
/// 1. Show current camera name in Panel_CenterOverlay.
/// 2. Show TCP display name and current TCP local position.
/// 3. Manage central Crosshair display.
/// 4. Show focus header information in Camera mode.
/// 5. Show long runtime messages in ToastArea instead of TopBar.
/// </summary>
public class scr_FR5CenterOverlayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private scr_FR5RobotManualController robotController;
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;
    [SerializeField] private scr_FR5UILayoutModeController layoutModeController;

    [Header("Center Overlay Text")]
    [SerializeField] private TMP_Text currentCameraLabelText;
    [SerializeField] private TMP_Text currentCameraValueText;
    [SerializeField] private TMP_Text tcpLabelText;
    [SerializeField] private TMP_Text tcpValueText;

    [Header("Focus Header")]
    [SerializeField] private GameObject focusHeaderRoot;
    [SerializeField] private TMP_Text focusTitleText;
    [SerializeField] private TMP_Text focusRobotStateText;
    [SerializeField] private TMP_Text focusSourceText;
    [SerializeField] private TMP_Text focusValidText;

    [Header("Toast")]
    [SerializeField] private GameObject toastAreaRoot;
    [SerializeField] private TMP_Text toastMessageText;
    [SerializeField] private bool showToastOnlyInCameraMode = true;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private Color crosshairColor = new Color(0.15f, 0.76f, 1.0f, 0.85f);

    [Header("Display")]
    [SerializeField] private string currentCameraName = "PERSPECTIVE";
    [SerializeField] private string tcpDisplayName = "TCP SHADOW";
    [SerializeField] private bool autoRefreshInUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.05f;

    private float nextRefreshTime = 0f;

    public string CurrentCameraName => currentCameraName;
    public string TcpDisplayName => tcpDisplayName;

    private void Start()
    {
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

    public void SetCurrentCameraName(string cameraName)
    {
        currentCameraName = string.IsNullOrWhiteSpace(cameraName)
            ? "PERSPECTIVE"
            : cameraName.ToUpperInvariant();

        RefreshUI();
    }

    public void SetTcpDisplayName(string displayName)
    {
        tcpDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? "TCP SHADOW"
            : displayName.ToUpperInvariant();

        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshInfoGroup();
        RefreshCrosshair();
        RefreshFocusHeader();
        RefreshToast();
    }

    private void RefreshInfoGroup()
    {
        if (currentCameraLabelText != null)
        {
            currentCameraLabelText.text = "CAM";
        }

        if (currentCameraValueText != null)
        {
            currentCameraValueText.text = currentCameraName;
        }

        if (tcpLabelText != null)
        {
            tcpLabelText.text = tcpDisplayName;
        }

        if (tcpValueText != null)
        {
            if (robotController == null)
            {
                tcpValueText.text = "X 0.000\nY 0.000\nZ 0.000";
            }
            else
            {
                Vector3 tcpLocal = robotController.GetCurrentUnityTCPLocalPosition();
                tcpValueText.text =
                    $"X {tcpLocal.x:+0.000;-0.000;0.000}\n" +
                    $"Y {tcpLocal.y:+0.000;-0.000;0.000}\n" +
                    $"Z {tcpLocal.z:+0.000;-0.000;0.000}";
            }
        }
    }

    private void RefreshCrosshair()
    {
        if (crosshairRoot != null)
        {
            crosshairRoot.SetActive(showCrosshair);
        }

        if (crosshairImage != null)
        {
            crosshairImage.color = crosshairColor;
        }
    }

    private void RefreshFocusHeader()
    {
        if (focusHeaderRoot == null)
        {
            return;
        }

        bool isCameraMode = IsCameraMode();

        // LayoutModeController가 이미 Camera 모드에서 FocusHeader를 켜도록 되어 있어도
        // 여기서 한 번 더 안전하게 맞춘다.
        focusHeaderRoot.SetActive(isCameraMode);

        if (!isCameraMode)
        {
            return;
        }

        if (focusTitleText != null)
        {
            focusTitleText.text = $"CAM : {currentCameraName}";
        }

        if (runtimeSyncManager == null)
        {
            if (focusRobotStateText != null)
            {
                focusRobotStateText.text = "ROBOT : N/A";
            }

            if (focusSourceText != null)
            {
                focusSourceText.text = "SOURCE : N/A";
            }

            if (focusValidText != null)
            {
                focusValidText.text = "VALID : N/A";
            }

            return;
        }

        if (focusRobotStateText != null)
        {
            focusRobotStateText.text = $"ROBOT : {runtimeSyncManager.GetSelectedSourceRobotStateLabel()}";
        }

        if (focusSourceText != null)
        {
            focusSourceText.text = $"SOURCE : {runtimeSyncManager.GetFormattedSourceLabel()}";
        }

        if (focusValidText != null)
        {
            focusValidText.text = $"VALID : {(runtimeSyncManager.LastSampleValid ? "TRUE" : "FALSE")}";
        }
    }

    private void RefreshToast()
    {
        if (toastAreaRoot == null || toastMessageText == null)
        {
            return;
        }

        string message = string.Empty;

        if (runtimeSyncManager != null)
        {
            message = runtimeSyncManager.GetEffectiveMessageLabel();
        }

        bool hasMessage = !string.IsNullOrWhiteSpace(message) &&
                          !string.Equals(message, "Idle", System.StringComparison.OrdinalIgnoreCase);

        bool showToast = hasMessage;

        if (showToastOnlyInCameraMode)
        {
            showToast = hasMessage && IsCameraMode();
        }

        toastAreaRoot.SetActive(showToast);

        if (showToast)
        {
            toastMessageText.text = message;
        }
    }

    private bool IsCameraMode()
    {
        if (layoutModeController == null)
        {
            return false;
        }

        return layoutModeController.CurrentMode ==
               scr_FR5UILayoutModeController.LayoutMode.Camera;
    }
}