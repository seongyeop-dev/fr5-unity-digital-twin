using UnityEngine;

/// <summary>
/// FR5 Mesh Attachment Preset Applier
///
/// 역할:
/// - Visual Anchor 아래의 Mesh Attachment 오브젝트 local 보정값을 일괄 적용
/// - FK 계산과 분리된 상태로 mesh pivot / rotation mismatch 를 관리
/// - Attach 오브젝트만 수정하고 실제 Mesh 본체는 0,0,0 / 0,0,0 유지하는 구조를 권장
///
/// 사용 방식:
/// 1) VisualRoot 또는 별도 AttachmentManager 오브젝트에 부착
/// 2) bindings 에 Mesh_BaseAttach ~ Mesh_TCPAttach 등록
/// 3) localPosition / localRotation / localScale 값을 입력
/// 4) OnValidate / ContextMenu 로 즉시 반영
/// </summary>
[ExecuteAlways]
public class scr_FR5MeshAttachmentPresetApplier : MonoBehaviour
{
    [System.Serializable]
    public class MeshAttachmentBinding
    {
        [Header("Info")]
        public string name;

        [Header("Target Attach Transform")]
        public Transform target;

        [Header("Local Offset")]
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localRotation = Vector3.zero;
        public Vector3 localScale = Vector3.one;

        [Header("Apply Options")]
        public bool applyPosition = true;
        public bool applyRotation = true;
        public bool applyScale = false;
    }

    [Header("Bindings")]
    [SerializeField] private MeshAttachmentBinding[] bindings;

    [Header("Options")]
    [SerializeField] private bool applyInEditMode = true;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyContinuouslyInPlayMode = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyAll();
        }
    }

    private void Start()
    {
        if (applyOnEnable)
        {
            ApplyAll();
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (applyContinuouslyInPlayMode)
        {
            ApplyAll();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (applyInEditMode)
        {
            ApplyAll();
        }
    }
#endif

    [ContextMenu("Apply All")]
    public void ApplyAll()
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            ApplyBinding(bindings[i]);
        }

        if (verboseLog)
        {
            Debug.Log("[FR5MeshAttachmentPresetApplier] ApplyAll completed.");
        }
    }

    [ContextMenu("Capture Current As Preset")]
    public void CaptureCurrentAsPreset()
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            CaptureBinding(bindings[i]);
        }

        if (verboseLog)
        {
            Debug.Log("[FR5MeshAttachmentPresetApplier] CaptureCurrentAsPreset completed.");
        }
    }

    [ContextMenu("Reset Targets To Identity")]
    public void ResetTargetsToIdentity()
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            ResetBinding(bindings[i]);
        }

        if (verboseLog)
        {
            Debug.Log("[FR5MeshAttachmentPresetApplier] ResetTargetsToIdentity completed.");
        }
    }

    private void ApplyBinding(MeshAttachmentBinding binding)
    {
        if (binding == null || binding.target == null)
        {
            return;
        }

        if (binding.applyPosition)
        {
            binding.target.localPosition = binding.localPosition;
        }

        if (binding.applyRotation)
        {
            binding.target.localRotation = Quaternion.Euler(binding.localRotation);
        }

        if (binding.applyScale)
        {
            binding.target.localScale = binding.localScale;
        }
    }

    private void CaptureBinding(MeshAttachmentBinding binding)
    {
        if (binding == null || binding.target == null)
        {
            return;
        }

        binding.localPosition = binding.target.localPosition;
        binding.localRotation = NormalizeEuler(binding.target.localEulerAngles);
        binding.localScale = binding.target.localScale;
    }

    private void ResetBinding(MeshAttachmentBinding binding)
    {
        if (binding == null || binding.target == null)
        {
            return;
        }

        binding.target.localPosition = Vector3.zero;
        binding.target.localRotation = Quaternion.identity;
        binding.target.localScale = Vector3.one;
    }

    private Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z)
        );
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}