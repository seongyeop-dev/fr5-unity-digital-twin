using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Observation only. Never calls a robot, process, ROS, SDK or TAKE API.</summary>
[DisallowMultipleComponent]
public sealed class FR5PortfolioCameraDirector : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera[] shotCameras = new Camera[10];
    [SerializeField] private AudioListener sceneAudioListener;
    [SerializeField] private TMP_Text currentShotText;
    [SerializeField] private RawImage renderTexturePreview;
    [Tooltip("Opt-in only: temporarily hide the central RT image for a clean shot, then restore it. RT cameras keep rendering.")]
    [SerializeField] private bool hideRenderTexturePreviewDuringShots = false;
    [SerializeField] private bool keyboardShortcuts = true;
    [SerializeField] private KeyCode returnToMainKey = KeyCode.BackQuote;
    [SerializeField] private bool autoSequenceOnStart = false;
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private int[] sequence = { 1, 2, 9, 10, 3, 4, 5, 6, 7, 8 };
    [SerializeField] private float[] shotDurations = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
    [SerializeField] private int currentShotIndex;
    public int CurrentShotIndex => currentShotIndex; // 0 = Main; shots are 1..10.
    public bool SequenceRunning { get; private set; }
    public string LastConfigurationError { get; private set; } = "";
    public string CurrentShotName => ShotName(currentShotIndex);

    private static readonly KeyCode[] Keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
    private static readonly string[] Names = { "기본 화면", "전체 공정", "공급 매거진", "지그 삽입",
        "마운터", "검사", "컨베이어 02", "언로더", "전체 상단", "FR5 근접", "지그 추적" };
    private readonly List<Camera> gameViewCameras = new List<Camera>();
    private readonly List<AudioListener> listeners = new List<AudioListener>();
    private Camera activeCamera;
    private RawImage overriddenPreview;
    private bool previewWasEnabled;
    private int sequenceCursor;
    private float nextShotTime;

    public static string ShotName(int index) => index >= 0 && index < Names.Length ? Names[index] : "미확인";

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        SwitchCamera(0);
        if (autoSequenceOnStart) StartSequence();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        SequenceRunning = false;
        SwitchCamera(0); // Main fallback, not restoration of the old duplicate outputs.
        ApplyPreviewPolicy(false);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        ApplyPreviewPolicy(currentShotIndex > 0);
        if (activeCamera != null && (!activeCamera.gameObject.activeInHierarchy || activeCamera.targetTexture != null))
            ReturnToMainCamera();
        else if (activeCamera == null && currentShotIndex != 0)
            ReturnToMainCamera();

        if (keyboardShortcuts && !IsEditingInput())
        {
            if (Input.GetKeyDown(returnToMainKey)) { ReturnToMainCamera(); return; }
            for (int i = 0; i < Keys.Length; i++)
                if (Input.GetKeyDown(Keys[i])) { SelectCamera(i + 1); return; }
        }
        if (!SequenceRunning || Time.unscaledTime < nextShotTime) return;
        sequenceCursor++;
        if (sequence == null || sequenceCursor >= sequence.Length)
        {
            if (!loopSequence) { SequenceRunning = false; return; }
            sequenceCursor = 0;
        }
        PlaySequenceShot();
    }

    private static bool IsEditingInput()
    {
        GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
        return selected != null && (selected.GetComponentInParent<TMP_InputField>() != null ||
            selected.GetComponentInParent<InputField>() != null);
    }

    // Manual switching always cancels the camera-only sequence. It does not cancel any process.
    public void SelectCamera(int index)
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        SequenceRunning = false;
        if (index < 1 || index > 10) { SwitchCamera(0); return; }
        SwitchCamera(index);
    }

    public void ReturnToMainCamera()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        SequenceRunning = false;
        SwitchCamera(0);
    }

    [ContextMenu("Start Camera-Only Sequence (Play Mode)")]
    public void StartSequence()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        if (sequence == null || sequence.Length == 0) return;
        sequenceCursor = 0;
        SequenceRunning = true;
        PlaySequenceShot();
    }

    [ContextMenu("Stop Camera-Only Sequence")]
    public void StopSequence() { SequenceRunning = false; }

    private void PlaySequenceShot()
    {
        int shot = sequence[sequenceCursor];
        if (shot < 1 || shot > 10 || !SwitchCamera(shot))
        {
            SequenceRunning = false;
            return;
        }
        float seconds = shotDurations != null && shot <= shotDurations.Length ? shotDurations[shot - 1] : 5f;
        if (float.IsNaN(seconds) || float.IsInfinity(seconds)) seconds = 5f;
        nextShotTime = Time.unscaledTime + Mathf.Max(0.1f, seconds);
    }

    private bool IsGameViewCamera(Camera camera)
    {
        return camera != null && camera.gameObject.scene == gameObject.scene &&
            camera.gameObject.activeInHierarchy && camera.targetTexture == null;
    }

    private void RefreshOutputScope()
    {
        gameViewCameras.Clear();
        listeners.Clear();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                if (camera.targetTexture == null) gameViewCameras.Add(camera);
            listeners.AddRange(root.GetComponentsInChildren<AudioListener>(true));
        }
    }

    private bool SwitchCamera(int requestedIndex)
    {
        Camera desired = requestedIndex == 0 ? mainCamera :
            shotCameras != null && requestedIndex <= shotCameras.Length ? shotCameras[requestedIndex - 1] : null;
        bool requestedAvailable = IsGameViewCamera(desired);
        if (!requestedAvailable) desired = IsGameViewCamera(mainCamera) ? mainCamera : null;
        if (desired == null)
        {
            ReportError("Main Camera fallback is unavailable. No camera outputs changed.");
            SequenceRunning = false;
            return false;
        }
        if (sceneAudioListener == null || sceneAudioListener.gameObject.scene != gameObject.scene ||
            !sceneAudioListener.gameObject.activeInHierarchy)
        {
            ReportError("One active scene AudioListener must be assigned. No camera outputs changed.");
            SequenceRunning = false;
            return false;
        }

        RefreshOutputScope(); // Only on a switch; no per-frame scene search or repeated enable writes.
        foreach (Camera camera in gameViewCameras)
            if (camera.enabled != (camera == desired)) camera.enabled = camera == desired;
        foreach (AudioListener listener in listeners)
            if (listener.enabled != (listener == sceneAudioListener)) listener.enabled = listener == sceneAudioListener;
        activeCamera = desired;
        currentShotIndex = requestedAvailable ? requestedIndex : 0;
        ApplyPreviewPolicy(currentShotIndex > 0);
        LastConfigurationError = requestedAvailable ? "" : "Requested shot unavailable; using Main Camera.";
        string label = "촬영 화면 : " + ShotName(currentShotIndex);
        if (currentShotText != null && currentShotText.text != label) currentShotText.text = label;
        return requestedAvailable;
    }

    private void ReportError(string message)
    {
        if (LastConfigurationError != message) Debug.LogWarning("[FR5 Portfolio] " + message, this);
        LastConfigurationError = message;
    }

    private void ApplyPreviewPolicy(bool portfolioShot)
    {
        bool hide = portfolioShot && hideRenderTexturePreviewDuringShots && renderTexturePreview != null &&
            renderTexturePreview.gameObject.scene == gameObject.scene;
        if (overriddenPreview != null && (!hide || overriddenPreview != renderTexturePreview))
        {
            if (overriddenPreview.enabled != previewWasEnabled) overriddenPreview.enabled = previewWasEnabled;
            overriddenPreview = null;
        }
        if (!hide || overriddenPreview != null) return;
        overriddenPreview = renderTexturePreview;
        previewWasEnabled = overriddenPreview.enabled;
        if (overriddenPreview.enabled) overriddenPreview.enabled = false;
    }

    public bool ValidateConfiguration(out string reason)
    {
        if (!IsGameViewCamera(mainCamera)) { reason = "Main Camera unavailable or renders to a texture."; return false; }
        if (shotCameras == null || shotCameras.Length != 10) { reason = "Exactly ten shot references required."; return false; }
        var unique = new HashSet<Camera> { mainCamera };
        foreach (Camera camera in shotCameras)
            if (!IsGameViewCamera(camera) || !unique.Add(camera))
            { reason = "Missing, duplicate, inactive or RenderTexture shot reference."; return false; }
        if (sceneAudioListener == null || !sceneAudioListener.gameObject.activeInHierarchy ||
            sceneAudioListener.gameObject.scene != gameObject.scene)
        { reason = "An active scene AudioListener must be assigned."; return false; }
        if (sequence == null || sequence.Length == 0) { reason = "Empty camera sequence."; return false; }
        foreach (int index in sequence)
            if (index < 1 || index > 10) { reason = "Camera sequence indices must be 1..10."; return false; }
        if (shotDurations == null || shotDurations.Length != 10) { reason = "Ten shot durations required."; return false; }
        foreach (float duration in shotDurations)
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < .1f)
            { reason = "Every camera duration must be finite and at least 0.1 second."; return false; }
        reason = "PASS";
        return true;
    }
}
