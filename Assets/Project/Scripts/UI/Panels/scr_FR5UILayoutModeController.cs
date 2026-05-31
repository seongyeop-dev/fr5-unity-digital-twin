using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 UI 레이아웃 모드 컨트롤러
///
/// 역할:
/// 1. LeftDock의 Operate / Runtime / Validation / IO / Camera 페이지를 전환한다.
/// 2. Camera 페이지와 전체 카메라 모드를 분리한다.
/// 3. Panel_CenterViewport는 전체 카메라 모드에서만 켠다.
/// 4. RectTransform, Anchor, 위치, 크기는 절대 수정하지 않는다.
/// 5. 이 스크립트는 SetActive 표시 전환만 담당한다.
/// </summary>
public class scr_FR5UILayoutModeController : MonoBehaviour
{
    public enum LayoutMode
    {
        Operate,
        Runtime,
        Validation,
        IO,
        Camera
    }

    [System.Serializable]
    private class TabBinding
    {
        public LayoutMode mode = LayoutMode.Operate;
        public Button button;
        public Graphic buttonGraphic;
        public TMP_Text buttonLabelText;
    }

    [Header("탭 연결")]
    [SerializeField] private TabBinding[] tabBindings;

    [Header("탭 색상 옵션")]
    [Tooltip("체크하면 코드가 탭 색상을 변경한다. 체크 해제하면 Inspector에서 설정한 색상을 유지한다.")]
    [SerializeField] private bool useScriptTabColors = false;

    [SerializeField] private Color activeTabColor = new Color(0.15f, 0.76f, 1.0f, 0.95f);
    [SerializeField] private Color inactiveTabColor = new Color(1.0f, 1.0f, 1.0f, 0.18f);
    [SerializeField] private Color activeLabelColor = Color.white;
    [SerializeField] private Color inactiveLabelColor = new Color(1.0f, 1.0f, 1.0f, 0.72f);

    [Header("메인 패널 루트")]
    [SerializeField] private GameObject panelBackground;
    [SerializeField] private GameObject panelTopBar;
    [SerializeField] private GameObject panelLeftDock;
    [SerializeField] private GameObject panelCenterViewport;
    [SerializeField] private GameObject panelRightDock;
    [SerializeField] private GameObject panelBottomBar;

    [Header("LeftDock 페이지")]
    [SerializeField] private GameObject panelOperatePage;
    [SerializeField] private GameObject panelRuntimePage;
    [SerializeField] private GameObject panelValidationPage;
    [SerializeField] private GameObject panelIOPage;
    [SerializeField] private GameObject panelCameraPage;

    [Header("카메라 View 내부 루트")]
    [SerializeField] private GameObject panelRobotView;
    [SerializeField] private GameObject panelCenterOverlay;

    [Header("TopBar 상세 표시 블록")]
    [SerializeField] private GameObject blockLastPoll;
    [SerializeField] private GameObject blockLastMessage;

    [Header("Center Focus 표시")]
    [SerializeField] private GameObject focusHeader;
    [SerializeField] private GameObject focusBackdrop;

    [Header("하단 콘솔")]
    [SerializeField] private GameObject panelBottomConsole;

    [Header("전체 카메라 모드 옵션")]
    [SerializeField] private bool keepTopBarVisibleInFullscreenCameraMode = true;
    [SerializeField] private bool keepCenterOverlayVisibleInFullscreenCameraMode = true;
    [SerializeField] private bool allowEscapeToExitFullscreenCameraMode = true;

    [Header("기본 모드")]
    [SerializeField] private LayoutMode defaultMode = LayoutMode.Operate;
    [SerializeField] private bool applyDefaultModeOnStart = true;

    [Header("현재 상태")]
    [SerializeField] private LayoutMode currentMode = LayoutMode.Operate;
    [SerializeField] private bool isFullscreenCameraMode = false;

    private LayoutMode lastNonCameraMode = LayoutMode.Operate;

    public LayoutMode CurrentMode => currentMode;
    public bool IsFullscreenCameraMode => isFullscreenCameraMode;

    private void Start()
    {
        if (applyDefaultModeOnStart)
        {
            SetMode(defaultMode, true);
            return;
        }

        RefreshLayout();
    }

    private void Update()
    {
        if (!allowEscapeToExitFullscreenCameraMode)
        {
            return;
        }

        if (isFullscreenCameraMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitCameraMode();
        }
    }

    public void SetOperateMode()
    {
        SetMode(LayoutMode.Operate, true);
    }

    public void SetRuntimeMode()
    {
        SetMode(LayoutMode.Runtime, true);
    }

    public void SetValidationMode()
    {
        SetMode(LayoutMode.Validation, true);
    }

    public void SetIOMode()
    {
        SetMode(LayoutMode.IO, true);
    }

    public void SetCameraMode()
    {
        // 여기서는 전체 카메라 모드가 아니라 LeftDock의 CameraPage만 표시한다.
        SetMode(LayoutMode.Camera, true);
    }

    public void EnterCameraFullscreenMode()
    {
        // 전체 카메라 화면으로 진입한다.
        currentMode = LayoutMode.Camera;
        isFullscreenCameraMode = true;
        ApplyLayoutVisibility();
        RefreshTabVisuals();
    }

    public void ToggleCameraFullscreenMode()
    {
        if (isFullscreenCameraMode)
        {
            ExitCameraMode();
            return;
        }

        EnterCameraFullscreenMode();
    }

    public void ExitCameraMode()
    {
        if (isFullscreenCameraMode)
        {
            // 전체 카메라 모드에서 빠져나오면 CameraPage로 돌아간다.
            isFullscreenCameraMode = false;
            currentMode = LayoutMode.Camera;
            ApplyLayoutVisibility();
            RefreshTabVisuals();
            return;
        }

        // 일반 CameraPage에서 Exit을 누르면 이전 작업 페이지로 돌아간다.
        SetMode(lastNonCameraMode, true);
    }

    public void RefreshLayout()
    {
        ApplyLayoutVisibility();
        RefreshTabVisuals();
    }

    private void SetMode(LayoutMode mode, bool refreshVisuals)
    {
        if (mode != LayoutMode.Camera)
        {
            lastNonCameraMode = mode;
        }

        currentMode = mode;
        isFullscreenCameraMode = false;

        ApplyLayoutVisibility();

        if (refreshVisuals)
        {
            RefreshTabVisuals();
        }
    }

    private void ApplyLayoutVisibility()
    {
        ResetOptionalBlocks();

        if (isFullscreenCameraMode)
        {
            ApplyFullscreenCameraMode();
            return;
        }

        ApplyNormalPageMode();
    }

    private void ResetOptionalBlocks()
    {
        SetActiveSafe(panelBottomConsole, false);
        SetActiveSafe(blockLastPoll, false);
        SetActiveSafe(blockLastMessage, false);
        SetActiveSafe(focusHeader, false);
        SetActiveSafe(focusBackdrop, false);
    }

    private void ApplyNormalPageMode()
    {
        // 일반 UI 모드에서는 기본 UI 패널을 모두 유지한다.
        SetActiveSafe(panelBackground, true);
        SetActiveSafe(panelTopBar, true);
        SetActiveSafe(panelLeftDock, true);
        SetActiveSafe(panelRightDock, true);
        SetActiveSafe(panelBottomBar, true);

        // Panel_CenterViewport는 전체 카메라 모드 전용이므로 일반 모드에서는 끈다.
        SetActiveSafe(panelCenterViewport, false);

        // 부모가 꺼져 있어도, 카메라 모드 진입 시 정상 표시되도록 자식은 켜둔다.
        SetActiveSafe(panelRobotView, true);
        SetActiveSafe(panelCenterOverlay, true);

        ApplyLeftPageVisibility(currentMode);

        if (currentMode == LayoutMode.Runtime)
        {
            SetActiveSafe(blockLastPoll, true);
            SetActiveSafe(blockLastMessage, true);
        }
    }

    private void ApplyFullscreenCameraMode()
    {
        // 전체 카메라 모드에서는 작업 패널을 숨기고 Camera View만 표시한다.
        SetActiveSafe(panelBackground, false);
        SetActiveSafe(panelTopBar, keepTopBarVisibleInFullscreenCameraMode);
        SetActiveSafe(panelLeftDock, false);
        SetActiveSafe(panelRightDock, false);
        SetActiveSafe(panelBottomBar, false);

        SetActiveSafe(panelCenterViewport, true);
        SetActiveSafe(panelRobotView, true);
        SetActiveSafe(panelCenterOverlay, keepCenterOverlayVisibleInFullscreenCameraMode);

        SetActiveSafe(focusHeader, true);
        SetActiveSafe(focusBackdrop, false);

        // LeftDock은 숨겨지지만 상태 일관성을 위해 CameraPage를 활성 상태로 맞춘다.
        ApplyLeftPageVisibility(LayoutMode.Camera);
    }

    private void ApplyLeftPageVisibility(LayoutMode mode)
    {
        SetActiveSafe(panelOperatePage, mode == LayoutMode.Operate);
        SetActiveSafe(panelRuntimePage, mode == LayoutMode.Runtime);
        SetActiveSafe(panelValidationPage, mode == LayoutMode.Validation);
        SetActiveSafe(panelIOPage, mode == LayoutMode.IO);
        SetActiveSafe(panelCameraPage, mode == LayoutMode.Camera);
    }

    private void RefreshTabVisuals()
    {
        if (!useScriptTabColors)
        {
            return;
        }

        if (tabBindings == null)
        {
            return;
        }

        for (int i = 0; i < tabBindings.Length; i++)
        {
            TabBinding binding = tabBindings[i];

            if (binding == null)
            {
                continue;
            }

            bool isActive = binding.mode == currentMode;

            Graphic graphic = binding.buttonGraphic;

            if (graphic == null && binding.button != null)
            {
                graphic = binding.button.targetGraphic as Graphic;
            }

            if (graphic != null)
            {
                graphic.color = isActive ? activeTabColor : inactiveTabColor;
            }

            if (binding.buttonLabelText != null)
            {
                binding.buttonLabelText.color = isActive ? activeLabelColor : inactiveLabelColor;
            }
        }
    }

    private void SetActiveSafe(GameObject target, bool state)
    {
        if (target == null)
        {
            return;
        }

        if (target.activeSelf == state)
        {
            return;
        }

        target.SetActive(state);
    }
}