using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 오른쪽 상태 패널 탭 컨트롤러
///
/// 역할:
/// 1. RightDock 안의 상세 상태 패널을 전환한다.
/// 2. Panel_Right_Summary는 고정 Summary 영역으로 유지한다.
/// 3. RightTabBar는 고정 탭 영역으로 유지한다.
/// 4. Runtime / Validation / Alarm 상세 패널 중 하나만 표시한다.
/// 5. RectTransform, Anchor, 위치, 크기는 절대 수정하지 않는다.
/// 6. 기본 설정에서는 버튼 색상을 코드로 바꾸지 않는다.
/// </summary>
public class scr_FR5RightStatusTabController : MonoBehaviour
{
    public enum RightStatusView
    {
        Summary,
        Runtime,
        Validation,
        Alarm
    }

    [System.Serializable]
    private class RightTabBinding
    {
        public RightStatusView view = RightStatusView.Runtime;
        public Button button;
        public Graphic buttonGraphic;
        public TMP_Text buttonLabelText;
    }

    [Header("오른쪽 상태 탭 버튼")]
    [SerializeField] private RightTabBinding[] rightTabBindings;

    [Header("오른쪽 상태 패널")]
    [SerializeField] private GameObject panelRightSummary;
    [SerializeField] private GameObject panelRightRuntime;
    [SerializeField] private GameObject panelRightValidation;
    [SerializeField] private GameObject panelRightAlarm;
    [SerializeField] private GameObject panelRightStatus;

    [Header("표시 옵션")]
    [SerializeField] private bool keepSummaryAlwaysVisible = true;
    [SerializeField] private bool hideRightStatusPanel = true;

    [Header("기본 상세 탭")]
    [SerializeField] private RightStatusView defaultView = RightStatusView.Runtime;
    [SerializeField] private bool applyDefaultViewOnStart = true;

    [Header("탭 색상 옵션")]
    [Tooltip("체크하면 코드가 탭 색상을 변경한다. 체크 해제하면 Inspector에서 설정한 버튼 색상을 유지한다.")]
    [SerializeField] private bool useScriptTabColors = false;

    [SerializeField] private Color activeTabColor = new Color(0.15f, 0.76f, 1.0f, 0.95f);
    [SerializeField] private Color inactiveTabColor = new Color(1.0f, 1.0f, 1.0f, 0.16f);
    [SerializeField] private Color activeLabelColor = Color.white;
    [SerializeField] private Color inactiveLabelColor = new Color(1.0f, 1.0f, 1.0f, 0.72f);

    [Header("현재 상태")]
    [SerializeField] private RightStatusView currentView = RightStatusView.Runtime;

    public RightStatusView CurrentView => currentView;

    private void Start()
    {
        if (applyDefaultViewOnStart)
        {
            SetView(defaultView);
            return;
        }

        RefreshView();
    }

    public void ShowSummary()
    {
        SetView(RightStatusView.Summary);
    }

    public void ShowRuntime()
    {
        SetView(RightStatusView.Runtime);
    }

    public void ShowValidation()
    {
        SetView(RightStatusView.Validation);
    }

    public void ShowAlarm()
    {
        SetView(RightStatusView.Alarm);
    }

    public void RefreshView()
    {
        ApplyPanelVisibility();
        RefreshTabVisuals();
    }

    private void SetView(RightStatusView view)
    {
        currentView = view;
        ApplyPanelVisibility();
        RefreshTabVisuals();
    }

    private void ApplyPanelVisibility()
    {
        // Summary는 고정 영역이다.
        // keepSummaryAlwaysVisible이 켜져 있으면 어떤 탭에서도 항상 보인다.
        if (keepSummaryAlwaysVisible)
        {
            SetActiveSafe(panelRightSummary, true);
        }
        else
        {
            SetActiveSafe(panelRightSummary, currentView == RightStatusView.Summary);
        }

        // Summary 탭을 누르면 아래 상세 패널은 모두 숨긴다.
        bool showRuntime = currentView == RightStatusView.Runtime;
        bool showValidation = currentView == RightStatusView.Validation;
        bool showAlarm = currentView == RightStatusView.Alarm;

        SetActiveSafe(panelRightRuntime, showRuntime);
        SetActiveSafe(panelRightValidation, showValidation);
        SetActiveSafe(panelRightAlarm, showAlarm);

        // Panel_RightStatus는 현재 BottomBar와 역할이 겹치므로 기본 비활성화한다.
        if (hideRightStatusPanel)
        {
            SetActiveSafe(panelRightStatus, false);
        }
    }

    private void RefreshTabVisuals()
    {
        // 버튼 색상은 기본적으로 Inspector에서 설정한다.
        // useScriptTabColors가 꺼져 있으면 코드가 버튼 색상을 절대 바꾸지 않는다.
        if (!useScriptTabColors)
        {
            return;
        }

        if (rightTabBindings == null)
        {
            return;
        }

        for (int i = 0; i < rightTabBindings.Length; i++)
        {
            RightTabBinding binding = rightTabBindings[i];

            if (binding == null)
            {
                continue;
            }

            bool isActive = binding.view == currentView;

            Graphic targetGraphic = binding.buttonGraphic;

            if (targetGraphic == null && binding.button != null)
            {
                targetGraphic = binding.button.targetGraphic as Graphic;
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = isActive ? activeTabColor : inactiveTabColor;
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