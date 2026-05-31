using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FR5 가상 로봇 Pivot 설정 보조 스크립트입니다.
/// 
/// 목적:
/// J1_Root ~ J6_Root의 위치를 실제 관절 중심 위치로 이동시키되,
/// 현재 화면에 보이는 Mesh 위치와 회전은 그대로 유지합니다.
/// 
/// 사용 순서:
/// 1. 이 스크립트를 FR5_MeshRoot 오브젝트에 추가합니다.
/// 2. Mesh Root, J1_Root ~ J6_Root를 Inspector에 연결합니다.
/// 3. 컴포넌트 우측 메뉴에서
///    "FR5 Pivot 위치 적용 / 화면 유지" 를 실행합니다.
/// 
/// 주의:
/// 이 스크립트는 SDK, Python, Shadow 검증과 무관합니다.
/// 현재 단계에서는 Unity 가상 FR5 모델의 관절 Pivot 위치를 잡기 위한 임시 설정 도구입니다.
/// </summary>
[ExecuteAlways]
public class scr_FR5VirtualPivotSetupTool : MonoBehaviour
{
    [Header("기준 Root")]
    [Tooltip("FR5_MeshRoot를 연결합니다. 비워두면 이 스크립트가 붙은 Transform을 자동 사용합니다.")]
    [SerializeField] private Transform meshRoot;

    [Header("관절 Root 연결")]
    [Tooltip("1번 관절 Root입니다.")]
    [SerializeField] private Transform j1Root;

    [Tooltip("2번 관절 Root입니다.")]
    [SerializeField] private Transform j2Root;

    [Tooltip("3번 관절 Root입니다.")]
    [SerializeField] private Transform j3Root;

    [Tooltip("4번 관절 Root입니다.")]
    [SerializeField] private Transform j4Root;

    [Tooltip("5번 관절 Root입니다.")]
    [SerializeField] private Transform j5Root;

    [Tooltip("6번 관절 Root입니다.")]
    [SerializeField] private Transform j6Root;

    [Header("관절 Pivot 위치 / FR5_MeshRoot 로컬 좌표")]
    [Tooltip("J1 관절 중심 위치입니다.")]
    [SerializeField] private Vector3 j1PivotLocal = new Vector3(0f, 0f, 0f);

    [Tooltip("J2 관절 중심 위치입니다. Mesh_Link1 위치를 기준으로 추정했습니다.")]
    [SerializeField] private Vector3 j2PivotLocal = new Vector3(0f, -0.0006055334f, 0.001520001f);

    [Tooltip("J3 관절 중심 위치입니다. Mesh_Link2 위치를 기준으로 추정했습니다.")]
    [SerializeField] private Vector3 j3PivotLocal = new Vector3(0.004249339f, -0.002023906f, 0.001510259f);

    [Tooltip("J4 관절 중심 위치입니다. Mesh_Link3 위치를 기준으로 추정했습니다.")]
    [SerializeField] private Vector3 j4PivotLocal = new Vector3(0.0082f, -0.0005805464f, 0.00152f);

    [Tooltip("J5 관절 중심 위치입니다. Mesh_Link4 위치를 기준으로 추정했습니다.")]
    [SerializeField] private Vector3 j5PivotLocal = new Vector3(0.008196579f, -0.001003408f, 0.002002222f);

    [Tooltip("J6 관절 중심 위치입니다. Gripper_Root / 플랜지 위치를 기준으로 추정했습니다.")]
    [SerializeField] private Vector3 j6PivotLocal = new Vector3(0.008207091f, -0.00202762f, 0.0004884619f);

    /// <summary>
    /// J1~J6 Root의 Pivot 위치를 적용합니다.
    /// 적용 과정에서 하위 Mesh들의 월드 위치와 회전은 유지합니다.
    /// </summary>
    [ContextMenu("FR5 Pivot 위치 적용 / 화면 유지")]
    public void ApplyPivotPositionsPreserveVisual()
    {
        if (meshRoot == null)
        {
            meshRoot = transform;
        }

        ApplySinglePivot(j1Root, j1PivotLocal);
        ApplySinglePivot(j2Root, j2PivotLocal);
        ApplySinglePivot(j3Root, j3PivotLocal);
        ApplySinglePivot(j4Root, j4PivotLocal);
        ApplySinglePivot(j5Root, j5PivotLocal);
        ApplySinglePivot(j6Root, j6PivotLocal);

        Debug.Log("[FR5 Pivot 설정] 관절 Pivot 위치 적용 완료. 화면상의 Mesh 위치는 유지되었습니다.");
    }

    /// <summary>
    /// 단일 관절 Root의 Pivot 위치를 이동합니다.
    /// 이동 전 하위 오브젝트의 월드 Transform을 저장하고,
    /// 이동 후 다시 복원하여 화면상 모델이 흐트러지지 않게 합니다.
    /// </summary>
    /// <param name="jointRoot">이동할 관절 Root</param>
    /// <param name="pivotLocalInMeshRoot">FR5_MeshRoot 기준 로컬 Pivot 위치</param>
    private void ApplySinglePivot(Transform jointRoot, Vector3 pivotLocalInMeshRoot)
    {
        if (jointRoot == null || meshRoot == null)
        {
            Debug.LogWarning("[FR5 Pivot 설정] Joint Root 또는 Mesh Root 연결이 비어 있습니다.");
            return;
        }

        List<TransformPose> childPoses = CaptureDescendantWorldPoses(jointRoot);

        Vector3 targetWorldPosition = meshRoot.TransformPoint(pivotLocalInMeshRoot);
        jointRoot.position = targetWorldPosition;

        RestoreDescendantWorldPoses(childPoses);
    }

    /// <summary>
    /// 특정 Root 아래에 있는 모든 하위 Transform의 월드 위치와 회전을 저장합니다.
    /// </summary>
    /// <param name="root">저장 기준 Root</param>
    /// <returns>하위 Transform들의 월드 Transform 목록</returns>
    private List<TransformPose> CaptureDescendantWorldPoses(Transform root)
    {
        List<TransformPose> poses = new List<TransformPose>();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root)
            {
                continue;
            }

            poses.Add(new TransformPose(child, child.position, child.rotation));
        }

        return poses;
    }

    /// <summary>
    /// 저장해둔 월드 위치와 회전을 다시 적용합니다.
    /// </summary>
    /// <param name="poses">복원할 Transform 목록</param>
    private void RestoreDescendantWorldPoses(List<TransformPose> poses)
    {
        foreach (TransformPose pose in poses)
        {
            if (pose.target == null)
            {
                continue;
            }

            pose.target.SetPositionAndRotation(pose.worldPosition, pose.worldRotation);
        }
    }

    /// <summary>
    /// Transform의 월드 위치와 회전을 임시 저장하기 위한 구조체입니다.
    /// </summary>
    private struct TransformPose
    {
        public Transform target;
        public Vector3 worldPosition;
        public Quaternion worldRotation;

        public TransformPose(Transform target, Vector3 worldPosition, Quaternion worldRotation)
        {
            this.target = target;
            this.worldPosition = worldPosition;
            this.worldRotation = worldRotation;
        }
    }
}