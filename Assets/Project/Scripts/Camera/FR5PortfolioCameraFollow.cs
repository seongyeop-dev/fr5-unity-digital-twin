using UnityEngine;

/// <summary>World-space observational follow. The only write target is this Camera's Transform.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class FR5PortfolioCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 positionOffset = new Vector3(.4f, .3f, -.4f);
    [SerializeField] private Vector3 lookOffset = Vector3.zero;
    [SerializeField, Min(0)] private float followDamping = 4f;
    [SerializeField, Min(0)] private float rotationDamping = 6f;
    [SerializeField] private bool followEnabled = true;
    private Camera ownCamera;
    public Transform Target => target;
    public bool FollowEnabled { get => followEnabled; set => followEnabled = value; }

    public void SetTarget(Transform newTarget)
    {
        // Null detaches safely. Refuse a self/child target: moving the camera must never move its target.
        target = newTarget != null && (newTarget == transform || newTarget.IsChildOf(transform)) ? null : newTarget;
    }

    private void Awake() { ownCamera = GetComponent<Camera>(); }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !followEnabled || target == null) return;
        if (ownCamera == null) ownCamera = GetComponent<Camera>();
        if (ownCamera == null || !ownCamera.enabled || ownCamera.targetTexture != null) return;
        if (target == transform || target.IsChildOf(transform)) return;
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
        Vector3 desired = target.position + positionOffset;
        Vector3 look = target.position + lookOffset;
        if (!Finite(desired) || !Finite(look)) return;
        Vector3 position = Vector3.Lerp(transform.position, desired, Blend(followDamping, dt));
        Vector3 direction = look - position;
        Quaternion rotation = transform.rotation;
        if (direction.sqrMagnitude > .000001f)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > .999f ? Vector3.forward : Vector3.up;
            rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(direction, up), Blend(rotationDamping, dt));
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    private static float Blend(float damping, float dt)
    {
        if (float.IsNaN(damping) || float.IsInfinity(damping)) damping = 4f;
        return damping <= 0 ? 1f : 1f - Mathf.Exp(-damping * dt);
    }
    private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
        float.IsNaN(v.y) || float.IsInfinity(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.z));
}
