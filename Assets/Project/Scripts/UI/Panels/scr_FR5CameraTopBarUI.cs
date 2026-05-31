using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 Camera Top Bar UI
///
/// Role:
/// 1. Manage TopBar camera view selection.
/// 2. Change RawImage texture based on fixed camera RenderTextures.
/// 3. Update current camera name in CenterOverlay.
/// 4. Keep inspector-defined button colors without runtime overwrite.
/// </summary>
public class scr_FR5CameraTopBarUI : MonoBehaviour
{
    public enum CameraViewType
    {
        Perspective,
        Front,
        Tcp,
        EndEffector
    }

    [System.Serializable]
    private class CameraViewBinding
    {
        public CameraViewType viewType = CameraViewType.Perspective;
        public string displayName = "PERSPECTIVE";

        [Header("Optional Camera Object")]
        public GameObject targetCameraObject;

        [Header("Render Target")]
        public RenderTexture renderTexture;

        [Header("Button")]
        public Button button;
        public Graphic buttonGraphic;
        public TMP_Text buttonLabelText;
    }

    [Header("Camera Bindings")]
    [SerializeField] private CameraViewBinding[] cameraViews;

    [Header("Render Output")]
    [SerializeField] private RawImage targetRawImage;

    [Header("Linked UI")]
    [SerializeField] private scr_FR5CenterOverlayUI centerOverlayUI;
    [SerializeField] private scr_FR5BottomBarUI bottomBarUI;

    [Header("Default")]
    [SerializeField] private CameraViewType defaultView = CameraViewType.Perspective;
    [SerializeField] private bool applyDefaultViewOnStart = true;

    [Header("Runtime Display")]
    [SerializeField] private CameraViewType currentView = CameraViewType.Perspective;
    [SerializeField] private string currentCameraName = "PERSPECTIVE";

    public string CurrentCameraName => currentCameraName;
    public CameraViewType CurrentView => currentView;

    private void Start()
    {
        if (applyDefaultViewOnStart)
        {
            SetCameraView(defaultView, false);
            return;
        }

        RefreshUI();
    }

    public void SetPerspectiveView()
    {
        SetCameraView(CameraViewType.Perspective);
    }

    public void SetFrontView()
    {
        SetCameraView(CameraViewType.Front);
    }

    public void SetTcpView()
    {
        SetCameraView(CameraViewType.Tcp);
    }

    public void SetEndEffectorView()
    {
        SetCameraView(CameraViewType.EndEffector);
    }

    public void SetCameraView(CameraViewType viewType)
    {
        SetCameraView(viewType, true);
    }

    public void RefreshUI()
    {
        if (centerOverlayUI != null)
        {
            centerOverlayUI.SetCurrentCameraName(currentCameraName);
        }
    }

    private void SetCameraView(CameraViewType viewType, bool writeLog)
    {
        CameraViewBinding selectedBinding = FindBinding(viewType);

        currentView = viewType;
        currentCameraName = selectedBinding != null
            ? NormalizeDisplayName(selectedBinding.displayName, viewType)
            : NormalizeDisplayName(viewType.ToString(), viewType);

        // Keep camera objects active if they are already used as fixed RT sources.
        // Do not toggle colors at runtime. Only switch RawImage texture.
        if (targetRawImage != null && selectedBinding != null && selectedBinding.renderTexture != null)
        {
            targetRawImage.texture = selectedBinding.renderTexture;
        }

        RefreshUI();

        if (writeLog && bottomBarUI != null)
        {
            bottomBarUI.SetLogMessage($"Camera changed: {currentCameraName}");
        }
    }

    private CameraViewBinding FindBinding(CameraViewType viewType)
    {
        if (cameraViews == null)
        {
            return null;
        }

        for (int i = 0; i < cameraViews.Length; i++)
        {
            if (cameraViews[i] != null && cameraViews[i].viewType == viewType)
            {
                return cameraViews[i];
            }
        }

        return null;
    }

    private string NormalizeDisplayName(string rawName, CameraViewType fallbackView)
    {
        if (!string.IsNullOrWhiteSpace(rawName))
        {
            return rawName.ToUpperInvariant();
        }

        switch (fallbackView)
        {
            case CameraViewType.Perspective:
                return "PERSPECTIVE";
            case CameraViewType.Front:
                return "FRONT";
            case CameraViewType.Tcp:
                return "TCP";
            case CameraViewType.EndEffector:
                return "EE";
            default:
                return fallbackView.ToString().ToUpperInvariant();
        }
    }
}