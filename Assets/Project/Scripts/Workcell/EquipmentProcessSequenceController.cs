using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FR5DigitalTwin.Workcell
{
    [DisallowMultipleComponent]
    public sealed class EquipmentProcessSequenceController : MonoBehaviour
    {
        public enum JigInputMode { LegacyGenerated, ExternalFr5 }

        [Header("Conveyor 01 Input")]
        [SerializeField] private JigInputMode jigInputMode = JigInputMode.LegacyGenerated;
        // Preserve the existing serialized 0.15 m/s External Conveyor setting for every Jig route.
        [Header("Common Jig Transport (world meters/second)")]
        [FormerlySerializedAs("externalConveyorMetersPerSecond")]
        [SerializeField, Min(0.001f)] private float commonJigMetersPerSecond = 0.15f;
        public float CommonJigMetersPerSecond => commonJigMetersPerSecond;
        private const float Fr5DwellSeconds = 3f;
        private readonly HashSet<int> acceptedFr5Jigs = new HashSet<int>();
        private readonly HashSet<int> acceptedSourceSlots = new HashSet<int>();
        private bool started;
        private bool externalBusy;
        private Transform externalJig;
        private string externalPhase = "Idle";
        public JigInputMode InputMode => jigInputMode;
        public string ExternalPhase => externalPhase;
        public string LastExternalError { get; private set; } = "";
        public int LastExternalJigId { get; private set; }
        public int LastSourceSlot { get; private set; }
        public int ExternalCompletedCount { get; private set; }
        public int LegacyInstantiationCount { get; private set; }
        public float LastDwellSeconds { get; private set; }
        public Vector3 AcceptedWorldPosition { get; private set; }
        public Vector3 ConveyorBeginWorldPosition { get; private set; }
        public int FilledSlotCount
        {
            get
            {
                var ids = new HashSet<int>();
                if (filledSlots != null)
                    foreach (GameObject slot in filledSlots)
                        if (slot != null && slot.activeSelf) ids.Add(slot.GetInstanceID());
                return ids.Count;
            }
        }

        [Header("Runtime Jig Template")]
        [SerializeField] private Transform runtimeJig;
        [SerializeField] private GameObject runtimeCameraModuleArray;

        [Header("Conveyor 01 Route")]
        [SerializeField] private Transform conveyorStart;
        [SerializeField] private Transform conveyorEnd;

        [Header("Mounter Route")]
        [SerializeField] private Transform mounterEntry;
        [SerializeField] private Transform mounterProcess;

        [Header("Inspection Route")]
        [SerializeField] private Transform inspectionEntry;
        [SerializeField] private Transform inspectionProcess;

        [Header("Unloader Route")]
        [SerializeField] private Transform unloaderStart;
        [SerializeField] private Transform unloaderEnd;

        [Header("Magazine Lift")]
        [SerializeField] private Transform runtimeMagazineLift;
        [SerializeField] private Transform magazineLowerPose;
        [SerializeField] private Transform magazineUpperPose;

        [Header("External FR5 Finish Insert")]
        [SerializeField, Min(0.001f)] private float magazineLiftSpeed = 0.15f;

        public int LastFinishSlotIndex { get; private set; } = -1;
        public string LastFinishSlotPath { get; private set; } = "";
        public Vector3 LastLiftStart { get; private set; }
        public Vector3 LastLiftTarget { get; private set; }
        public Vector3 LastFinishSlotPosition { get; private set; }
        public Vector3 LastFinishApproach { get; private set; }
        public bool LastLiftVerticalOnly { get; private set; }
        public float LastFinishHeightErrorMm { get; private set; }
        public float LastFinishPositionErrorMm { get; private set; }
        public Quaternion LastFinishInsertionRotation { get; private set; }
        public Quaternion LastFinishCompletedRotation { get; private set; }
        public float LastFinishRotationDriftDegrees { get; private set; }
        // Compatibility accessor: now measures insertion drift, NOT error against slot.rotation.
        public float LastFinishRotationErrorDegrees => LastFinishRotationDriftDegrees;
        public bool LastFinishSwapWithoutOverlap { get; private set; }

        [Header("Magazine Filled Slots")]
        [SerializeField]
        private GameObject[] filledSlots = new GameObject[15];

        [Header("Stack Lights")]
        [SerializeField]
        private EquipmentStackLightController conveyor01Light;

        [SerializeField]
        private EquipmentStackLightController mounterLight;

        [SerializeField]
        private EquipmentStackLightController inspectionLight;

        [SerializeField]
        private EquipmentStackLightController conveyor02Light;

        [SerializeField]
        private EquipmentStackLightController unloaderLight;

        [Header("Process Timing")]
        [SerializeField, Min(0.01f)]
        private float moveDuration = 1.5f;

        [SerializeField, Min(0f)]
        private float mounterDuration = 6f;

        [SerializeField, Min(0f)]
        private float inspectionDuration = 3f;

        [SerializeField, Min(0f)]
        private float interStageDelay = 0.25f;

        [Header("Startup")]
        [SerializeField]
        private bool playOnStart = true;

        [SerializeField, Min(0f)]
        private float startDelay = 1f;

        private readonly List<GameObject> activeRuntimeJigs =
            new List<GameObject>();

        private Coroutine runningRoutine;
        private string cameraModuleRelativePath;

        private int nextSlotIndex;
        private int launchedJigCount;
        private int completedJigCount;
        private int productionTargetCount;

        private bool inspectionBusy;
        private bool unloaderBusy;

        public bool IsRunning => runningRoutine != null || externalBusy;

        private void Start()
        {
            cameraModuleRelativePath = GetRelativePath(
                runtimeJig,
                runtimeCameraModuleArray != null
                    ? runtimeCameraModuleArray.transform
                    : null);

            started = true;
            if (jigInputMode == JigInputMode.ExternalFr5)
            {
                // No template spawn, Finish reset, lift move, or automatic start.
                SetAllLightsReady();
                return;
            }
            ResetSequenceVisuals();

            if (playOnStart)
            {
                StartCoroutine(StartAfterDelay());
            }
        }

        private IEnumerator StartAfterDelay()
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            StartSequence();
        }

        [ContextMenu("Start Sequence")]
        public void StartSequence()
        {
            if (jigInputMode == JigInputMode.ExternalFr5)
            {
                Debug.Log("[EquipmentProcessSequenceController] External FR5 input: waiting for an inserted Jig.", this);
                return;
            }
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[EquipmentProcessSequenceController] " +
                    "Sequence can only run in Play Mode.",
                    this);

                return;
            }

            if (runningRoutine != null)
            {
                Debug.LogWarning(
                    "[EquipmentProcessSequenceController] " +
                    "Sequence is already running.",
                    this);

                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "[EquipmentProcessSequenceController] " +
                    "Required Inspector references are missing.",
                    this);

                return;
            }

            productionTargetCount = CountAssignedFilledSlots();

            if (productionTargetCount <= 0)
            {
                Debug.LogError(
                    "[EquipmentProcessSequenceController] " +
                    "No magazine filled slots are assigned.",
                    this);

                return;
            }

            runningRoutine = StartCoroutine(RunProductionLine());
        }

        [ContextMenu("Reset Sequence Visuals")]
        public void ResetSequenceVisuals()
        {
            if (jigInputMode == JigInputMode.ExternalFr5)
            {
                if (Application.isPlaying) AbortExternalJig("External input stopped. Jig and Finish contents retained.");
                return;
            }
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[EquipmentProcessSequenceController] " +
                    "Reset is only available in Play Mode.",
                    this);

                return;
            }

            StopAllCoroutines();
            runningRoutine = null;

            nextSlotIndex = 0;
            launchedJigCount = 0;
            completedJigCount = 0;
            productionTargetCount = 0;

            inspectionBusy = false;
            unloaderBusy = false;

            for (int index = 0; index < activeRuntimeJigs.Count; index++)
            {
                GameObject activeJig = activeRuntimeJigs[index];

                if (activeJig != null)
                {
                    Destroy(activeJig);
                }
            }

            activeRuntimeJigs.Clear();

            // Scene에 배치한 Runtime Jig는 복제용 템플릿으로만 사용한다.
            if (runtimeJig != null)
            {
                runtimeJig.gameObject.SetActive(false);
            }

            if (runtimeCameraModuleArray != null)
            {
                runtimeCameraModuleArray.SetActive(false);
            }

            if (
                runtimeMagazineLift != null &&
                magazineLowerPose != null)
            {
                runtimeMagazineLift.SetPositionAndRotation(
                    magazineLowerPose.position,
                    magazineLowerPose.rotation);
            }

            if (filledSlots != null)
            {
                foreach (GameObject filledSlot in filledSlots)
                {
                    if (filledSlot != null)
                    {
                        filledSlot.SetActive(false);
                    }
                }
            }

            SetAllLightsReady();
        }

        private IEnumerator RunProductionLine()
        {
            if (jigInputMode != JigInputMode.LegacyGenerated) yield break;
            // 첫 지그만 즉시 투입한다.
            LaunchNextJig();

            // 이후 지그는 각 지그의 Mounter 작업 완료 시점에 투입된다.
            while (completedJigCount < productionTargetCount)
            {
                yield return null;
            }

            SetAllLightsReady();
            runningRoutine = null;

            Debug.Log(
                "[EquipmentProcessSequenceController] " +
                "All magazine slots are filled. " +
                "Pipelined production sequence completed.",
                this);
        }

        private void LaunchNextJig()
        {
            if (jigInputMode != JigInputMode.LegacyGenerated) return;
            if (launchedJigCount >= productionTargetCount)
            {
                return;
            }

            GameObject jigObject = Instantiate(
                runtimeJig.gameObject,
                runtimeJig.parent);
            LegacyInstantiationCount++;

            launchedJigCount++;

            jigObject.name =
                $"Runtime_Jig_{launchedJigCount:00}";

            Transform jigTransform = jigObject.transform;
            GameObject cameraModuleArray = FindCameraModuleArray(
                jigTransform);

            if (cameraModuleArray == null)
            {
                Debug.LogError(
                    "[EquipmentProcessSequenceController] " +
                    $"Camera_Module_Array was not found in " +
                    $"{jigObject.name}.",
                    this);

                Destroy(jigObject);
                return;
            }

            activeRuntimeJigs.Add(jigObject);

            cameraModuleArray.SetActive(false);
            jigObject.SetActive(true);

            StartCoroutine(
                RunJigCycle(
                    jigTransform,
                    cameraModuleArray,
                    launchedJigCount));
        }

        private IEnumerator RunJigCycle(
            Transform jig,
            GameObject cameraModuleArray,
            int jigNumber,
            bool external = false)
        {
            if (!external)
            {
                SnapTo(jig, conveyorStart);
                yield return WaitForInterStageDelay();
            }

            // Conveyor 01
            conveyor01Light.SetProcessing();

            if (external)
            {
                externalPhase = "Conveyor01";
                ConveyorBeginWorldPosition = jig.position;
            }
            yield return MoveTransform(jig, conveyorEnd);

            conveyor01Light.SetReady();

            yield return WaitForInterStageDelay();

            // Mounter
            if (external) externalPhase = "ExistingSMT";
            mounterLight.SetProcessing();

            yield return MoveTransform(jig, mounterEntry);
            yield return MoveTransform(jig, mounterProcess);

            if (mounterDuration > 0f)
            {
                yield return new WaitForSeconds(mounterDuration);
            }

            // Mounter 완료 후 해당 지그의 Camera Module Array 활성화
            cameraModuleArray.SetActive(true);

            // Inspection이 비어야 Mounter의 완성 지그를 내보낼 수 있다.
            while (inspectionBusy)
            {
                yield return null;
            }

            inspectionBusy = true;
            mounterLight.SetReady();

            // 핵심 Lead Time 규칙:
            // Mounter 작업을 끝낸 지그가 Inspection으로 이동을 시작할 때
            // 다음 지그를 Conveyor 01에 추가 투입한다.
            if (!external) LaunchNextJig();

            yield return WaitForInterStageDelay();

            // Inspection
            inspectionLight.SetProcessing();

            yield return MoveTransform(jig, inspectionEntry);
            yield return MoveTransform(jig, inspectionProcess);

            if (inspectionDuration > 0f)
            {
                yield return new WaitForSeconds(inspectionDuration);
            }

            inspectionLight.SetReady();
            inspectionBusy = false;

            yield return WaitForInterStageDelay();

            // Conveyor 02
            conveyor02Light.SetProcessing();

            yield return MoveTransform(jig, unloaderStart);

            conveyor02Light.SetReady();

            yield return WaitForInterStageDelay();

            // 이전 지그의 Unloader 작업이 끝날 때까지 대기한다.
            while (unloaderBusy)
            {
                yield return null;
            }

            unloaderBusy = true;
            unloaderLight.SetProcessing();

            if (external)
            {
                yield return InsertExternalJigIntoFinishSlot(jig);
            }
            else
            {
                // Preserve the original generated-Jig demonstration mode.
                yield return MoveTransform(runtimeMagazineLift, magazineUpperPose);
                yield return MoveTransform(jig, unloaderEnd);
                jig.gameObject.SetActive(false);
                ActivateNextFilledSlot();
                yield return MoveTransform(runtimeMagazineLift, magazineLowerPose);
            }

            unloaderLight.SetReady();
            unloaderBusy = false;

            activeRuntimeJigs.Remove(jig.gameObject);
            Destroy(jig.gameObject);

            completedJigCount++;

            Debug.Log(
                "[EquipmentProcessSequenceController] " +
                $"Jig {jigNumber:00} completed. " +
                $"Completed {completedJigCount}/{productionTargetCount}.",
                this);
        }

        private string ValidateExternalFinishSlot(int index)
        {
            if (index < 0 || filledSlots == null || index >= filledSlots.Length || filledSlots[index] == null)
                return "No available Finish slot.";
            if (runtimeMagazineLift == null || runtimeMagazineLift.name != "Runtime_Magazine_Lift" ||
                runtimeMagazineLift.parent == null || runtimeMagazineLift.parent.name != "EQ_Unloader" ||
                unloaderEnd == null || !unloaderEnd.IsChildOf(runtimeMagazineLift.parent) ||
                unloaderEnd.IsChildOf(runtimeMagazineLift))
                return "Finish lift/output must belong to EQ_Unloader, not Source or Robot.";
            Transform slot = filledSlots[index].transform;
            if (slot == runtimeMagazineLift || !slot.IsChildOf(runtimeMagazineLift) ||
                filledSlots[index].activeSelf || slot.parent == null || !slot.parent.gameObject.activeInHierarchy)
                return "Selected Finish placeholder must be inactive under the active Finish lift.";
            if (!(magazineLiftSpeed > 0f) || float.IsInfinity(magazineLiftSpeed) ||
                !(commonJigMetersPerSecond > 0f) || float.IsInfinity(commonJigMetersPerSecond))
                return "Finish lift/common Jig speeds must be finite and positive.";
            // Visual swap guard only: a placeholder never supplies the Jig's motion rotation/axis.
            if (RotationDriftDegrees(slot.rotation, unloaderEnd.rotation) > 0.01f)
                return "Finish placeholder orientation differs from Unloader output. Run Configure Source and Finish in Edit Mode.";
            float targetY = runtimeMagazineLift.position.y + unloaderEnd.position.y - slot.position.y;
            if (float.IsNaN(targetY) || float.IsInfinity(targetY) || magazineLowerPose == null || magazineUpperPose == null ||
                targetY < Mathf.Min(magazineLowerPose.position.y, magazineUpperPose.position.y) - 0.0001f ||
                targetY > Mathf.Max(magazineLowerPose.position.y, magazineUpperPose.position.y) + 0.0001f)
                return "Slot alignment is outside the existing lower/upper lift travel.";
            return null;
        }

        private IEnumerator InsertExternalJigIntoFinishSlot(Transform jig)
        {
            // nextSlotIndex was reserved by the existing external-input API. Never reorder the array.
            int index = nextSlotIndex;
            string error = ValidateExternalFinishSlot(index);
            if (error != null) throw new System.InvalidOperationException(error);
            GameObject filled = filledSlots[index];
            Transform slot = filled.transform;
            LastFinishSlotIndex = index;
            LastFinishSlotPath = slot.name;
            for (Transform p = slot.parent; p != null; p = p.parent)
                LastFinishSlotPath = p.name + "/" + LastFinishSlotPath;
            LastFinishSwapWithoutOverlap = false;

            externalPhase = "UnloaderOutput";
            yield return MoveTransform(jig, unloaderEnd);
            Quaternion insertionRotation = jig.rotation;
            LastFinishInsertionRotation = insertionRotation;
            LastFinishCompletedRotation = insertionRotation;
            LastFinishRotationDriftDegrees = 0f;
            LastFinishApproach = jig.position;
            error = ValidateExternalFinishSlot(index);
            if (error != null) throw new System.InvalidOperationException(error);
            LastLiftStart = runtimeMagazineLift.position;
            Quaternion liftRotation = runtimeMagazineLift.rotation;
            Vector3 liftTarget = LastLiftStart;
            liftTarget.y += unloaderEnd.position.y - slot.position.y;
            LastLiftTarget = liftTarget;
            externalPhase = "FinishLift";
            yield return MoveFinishPose(runtimeMagazineLift, liftTarget, magazineLiftSpeed, true);
            LastLiftVerticalOnly = Mathf.Abs(runtimeMagazineLift.position.x - LastLiftStart.x) < 0.00001f &&
                Mathf.Abs(runtimeMagazineLift.position.z - LastLiftStart.z) < 0.00001f &&
                Quaternion.Angle(runtimeMagazineLift.rotation, liftRotation) < 0.01f;

            // Read the real slot again after the lift moved; never move a slot/reference Transform.
            LastFinishSlotPosition = slot.position;
            LastFinishHeightErrorMm = Mathf.Abs(slot.position.y - unloaderEnd.position.y) * 1000f;
            if (LastFinishHeightErrorMm > 0.1f)
                throw new System.InvalidOperationException("Finish slot height did not align with Unloader output.");
            // One horizontal segment from the Unloader output to the lifted slot POSITION.
            // No sideways approach, slot-axis projection, or rotation write at any Finish stage.
            if (Vector3.Distance(jig.position, LastFinishApproach) > 0.00001f ||
                RotationDriftDegrees(insertionRotation, jig.rotation) > 0.01f)
                throw new System.InvalidOperationException("Released Jig moved/rotated during Finish lift alignment.");
            Vector3 insertPosition = LastFinishSlotPosition;
            insertPosition.y = LastFinishApproach.y; // Exactly horizontal; height error stays a separate gate.
            externalPhase = "FinishInsert";
            yield return MoveFinishPose(jig, insertPosition, commonJigMetersPerSecond);
            LastFinishPositionErrorMm = Vector3.Distance(jig.position, slot.position) * 1000f;
            LastFinishCompletedRotation = jig.rotation;
            LastFinishRotationDriftDegrees = RotationDriftDegrees(insertionRotation, jig.rotation);
            if (filled.activeSelf || nextSlotIndex != index || filledSlots[index] != filled ||
                LastFinishPositionErrorMm > 0.1f || LastFinishRotationDriftDegrees > 0.01f ||
                RotationDriftDegrees(filled.transform.rotation, insertionRotation) > 0.01f)
                throw new System.InvalidOperationException("Finish pose/visual changed during insertion; no placeholder activated.");

            // No yield between these operations: the renderer swap is visible in a single frame.
            jig.gameObject.SetActive(false);
            ActivateNextFilledSlot();
            LastFinishSwapWithoutOverlap = !jig.gameObject.activeSelf && filled.activeInHierarchy;
            if (!LastFinishSwapWithoutOverlap)
                throw new System.InvalidOperationException("Finish visual hand-off failed.");
            // Hold the aligned lift while waiting for the next explicit external Jig.
        }

        private IEnumerator MoveFinishPose(Transform target, Vector3 position, float speed, bool verticalOnly = false)
        {
            if (target == null || (verticalOnly ? target != runtimeMagazineLift : target != externalJig))
                throw new System.InvalidOperationException("Only the Finish lift or the accepted Jig may animate.");
            if (!(speed > 0f) || float.IsInfinity(speed))
                throw new System.InvalidOperationException("Finish animation speed is invalid.");
            Vector3 start = target.position;
            Quaternion initialRotation = target.rotation;
            Transform parent = target.parent;
            Vector3 scale = target.localScale;
            if (verticalOnly && (position.x != start.x || position.z != start.z))
                throw new System.InvalidOperationException("Finish lift movement must be world-Y only.");
            if (!verticalOnly && position.y != start.y)
                throw new System.InvalidOperationException("Finish Jig insertion must be world-horizontal.");
            float duration = Vector3.Distance(start, position) / speed;
            if (verticalOnly) duration = Mathf.Max(0.2f, duration); // Existing lift timing/easing unchanged.
            float elapsed = 0f;
            Vector3 expectedPosition = start;

            while (elapsed < duration)
            {
                if (target == null || !target.gameObject.activeInHierarchy || target.parent != parent ||
                    !target.localScale.Equals(scale) || Vector3.Distance(target.position, expectedPosition) > 0.00001f ||
                    RotationDriftDegrees(initialRotation, target.rotation) > 0.01f ||
                    (verticalOnly && (externalJig == null ||
                        Vector3.Distance(externalJig.position, LastFinishApproach) > 0.00001f ||
                        RotationDriftDegrees(LastFinishInsertionRotation, externalJig.rotation) > 0.01f)))
                    throw new System.InvalidOperationException("Finish animation target ownership changed.");
                float t = Mathf.Clamp01(elapsed / duration);
                if (verticalOnly) t = t * t * (3f - 2f * t);
                target.position = Vector3.Lerp(start, position, t);
                // Never write rotation: detect drift instead of correcting it.
                expectedPosition = target.position;
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (target == null || !target.gameObject.activeInHierarchy || target.parent != parent ||
                !target.localScale.Equals(scale) || Vector3.Distance(target.position, expectedPosition) > 0.00001f ||
                RotationDriftDegrees(initialRotation, target.rotation) > 0.01f)
                throw new System.InvalidOperationException("Finish target changed at the animation boundary.");
            target.position = position;

        }

        // Double-precision relative quaternion angle resolves the 0.01-degree gate without
        // Quaternion.Angle's near-identity float-dot dead zone. q and -q are equivalent.
        public static float RotationDriftDegrees(Quaternion start, Quaternion end)
        {
            double x = (double)start.w * end.x - (double)start.x * end.w - (double)start.y * end.z + (double)start.z * end.y;
            double y = (double)start.w * end.y + (double)start.x * end.z - (double)start.y * end.w - (double)start.z * end.x;
            double z = (double)start.w * end.z - (double)start.x * end.y + (double)start.y * end.x - (double)start.z * end.w;
            double w = (double)start.w * end.w + (double)start.x * end.x + (double)start.y * end.y + (double)start.z * end.z;
            double norm = x * x + y * y + z * z + w * w;
            if (!(norm > 0d) || double.IsInfinity(norm)) return float.PositiveInfinity;
            return (float)(2d * System.Math.Atan2(System.Math.Sqrt(x * x + y * y + z * z), System.Math.Abs(w)) * 180d / System.Math.PI);
        }

        private IEnumerator MoveTransform(Transform target, Transform destination)
        {
            if (target == null || destination == null) yield break;
            bool lift = target == runtimeMagazineLift;
            if (!lift && (!(commonJigMetersPerSecond > 0f) || float.IsInfinity(commonJigMetersPerSecond)))
                throw new System.InvalidOperationException("Common Jig speed must be finite and positive.");
            Vector3 startPosition = target.position;
            Quaternion startRotation = target.rotation;
            Vector3 targetPosition = destination.position;
            Quaternion targetRotation = destination.rotation;
            // Legacy lift retains its original duration/easing. Every Jig uses distance / speed.
            float duration = lift ? moveDuration : Vector3.Distance(startPosition, targetPosition) / commonJigMetersPerSecond;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (lift) elapsed += Time.deltaTime; // Preserve the legacy lift's frame timing.
                float progress = Mathf.Clamp01(elapsed / duration);
                float translationProgress = lift ? progress * progress * (3f - 2f * progress) : progress;
                target.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, translationProgress),
                    Quaternion.Slerp(startRotation, targetRotation, progress * progress * (3f - 2f * progress)));
                yield return null;
                if (!lift) elapsed += Time.deltaTime;
            }
            target.SetPositionAndRotation(targetPosition, targetRotation);
        }

        private IEnumerator WaitForInterStageDelay()
        {
            if (interStageDelay > 0f)
            {
                yield return new WaitForSeconds(interStageDelay);
            }
        }

        private static void SnapTo(
            Transform target,
            Transform destination)
        {
            if (target == null || destination == null)
            {
                return;
            }

            target.SetPositionAndRotation(
                destination.position,
                destination.rotation);
        }

        private GameObject FindCameraModuleArray(Transform jigRoot)
        {
            if (jigRoot == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(cameraModuleRelativePath))
            {
                Transform found = jigRoot.Find(
                    cameraModuleRelativePath);

                if (found != null)
                {
                    return found.gameObject;
                }
            }

            Transform[] children =
                jigRoot.GetComponentsInChildren<Transform>(true);

            string expectedName =
                runtimeCameraModuleArray != null
                    ? runtimeCameraModuleArray.name
                    : "Camera_Module_Array";

            foreach (Transform child in children)
            {
                if (child.name == expectedName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static string GetRelativePath(
            Transform root,
            Transform child)
        {
            if (root == null || child == null)
            {
                return string.Empty;
            }

            if (root == child)
            {
                return string.Empty;
            }

            List<string> pathParts = new List<string>();
            Transform current = child;

            while (current != null && current != root)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return string.Empty;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts.ToArray());
        }

        private void ActivateNextFilledSlot()
        {
            if (filledSlots == null || filledSlots.Length == 0)
            {
                Debug.LogWarning(
                    "[EquipmentProcessSequenceController] " +
                    "No filled magazine slots are assigned.",
                    this);

                return;
            }

            while (
                nextSlotIndex < filledSlots.Length &&
                filledSlots[nextSlotIndex] == null)
            {
                nextSlotIndex++;
            }

            if (nextSlotIndex >= filledSlots.Length)
            {
                Debug.LogWarning(
                    "[EquipmentProcessSequenceController] " +
                    "All magazine slots are already filled.",
                    this);

                return;
            }

            filledSlots[nextSlotIndex].SetActive(true);

            Debug.Log(
                "[EquipmentProcessSequenceController] " +
                $"Magazine slot {nextSlotIndex + 1:00} filled.",
                this);

            nextSlotIndex++;
        }

        private int CountAssignedFilledSlots()
        {
            if (filledSlots == null)
            {
                return 0;
            }

            int count = 0;

            foreach (GameObject filledSlot in filledSlots)
            {
                if (filledSlot != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetAllLightsReady()
        {
            if (conveyor01Light != null)
            {
                conveyor01Light.SetReady();
            }

            if (mounterLight != null)
            {
                mounterLight.SetReady();
            }

            if (inspectionLight != null)
            {
                inspectionLight.SetReady();
            }

            if (conveyor02Light != null)
            {
                conveyor02Light.SetReady();
            }

            if (unloaderLight != null)
            {
                unloaderLight.SetReady();
            }
        }

        // Release must already be complete. This API never spawns, snaps, or destroys on rejection.
        public bool TryAcceptFr5InsertedJig(Transform jig, int sourceSlot)
        {
            if (!CanAcceptFr5Input(out string reason)) return RejectExternal(reason);
            if (sourceSlot < 1 || sourceSlot > 7) return RejectExternal("Source Slot must be 01..07; Slot08 is EMPTY.");
            if (jig == null || !jig.gameObject.activeInHierarchy) return RejectExternal("An active released Jig is required.");
            if (jig == runtimeJig || jig.Find("Jig_Visual") == null)
                return RejectExternal("Pass the released Jig prefab root, not a template or equipment root.");
            if (acceptedFr5Jigs.Contains(jig.GetInstanceID()) || acceptedSourceSlots.Contains(sourceSlot))
                return RejectExternal("Duplicate Jig or Source Slot.");
            GameObject camera = FindCameraModuleArray(jig);
            if (camera == null) return RejectExternal("Camera_Module_Array is missing.");
            foreach (Rigidbody body in jig.GetComponentsInChildren<Rigidbody>(true))
                if (!body.isKinematic) return RejectExternal("Released Jig still has dynamic physics ownership.");
            int available = FindExternalFinishSlot();
            if (available < 0) return RejectExternal("No unused Finish slot.");

            nextSlotIndex = available;
            externalJig = jig;
            externalBusy = true;
            LastExternalJigId = jig.GetInstanceID();
            LastSourceSlot = sourceSlot;
            AcceptedWorldPosition = jig.position;
            LastExternalError = "";
            LastDwellSeconds = 0;
            LastFinishSlotIndex = -1;
            LastFinishSlotPath = "";
            LastFinishSwapWithoutOverlap = false;
            LastLiftVerticalOnly = false;
            externalPhase = "Dwell";
            acceptedFr5Jigs.Add(LastExternalJigId);
            acceptedSourceSlots.Add(sourceSlot);
            activeRuntimeJigs.Add(jig.gameObject);
            launchedJigCount++;
            productionTargetCount = launchedJigCount;
            runningRoutine = StartCoroutine(RunExternalJig(jig, camera, sourceSlot));
            return true;
        }

        public bool CanAcceptFr5Input(out string reason)
        {
            reason = "";
            if (!Application.isPlaying || !isActiveAndEnabled || !started) reason = "Controller is not ready in Play Mode.";
            else if (jigInputMode != JigInputMode.ExternalFr5) reason = "External FR5 input mode is required.";
            else if (IsRunning) reason = "SMT process is busy.";
            else if (runtimeJig != null && runtimeJig.gameObject.activeSelf)
                reason = "The legacy Runtime_Jig_Transit template is still visible. Use Source and Finish setup in Edit Mode.";
            else if (!HasProcessReferences()) reason = "SMT route/lift/light references are missing.";
            else if (!(commonJigMetersPerSecond > 0f) || float.IsInfinity(commonJigMetersPerSecond))
                reason = "Common Jig transport speed is invalid.";
            else if (FindExternalFinishSlot() < 0) reason = "Finish slots are full, missing, or duplicate.";
            else reason = ValidateExternalFinishSlot(FindExternalFinishSlot()) ?? "";
            return reason.Length == 0;
        }

        private int FindExternalFinishSlot()
        {
            if (filledSlots == null) return -1;
            var ids = new HashSet<int>();
            int available = -1;
            for (int i = 0; i < filledSlots.Length; i++)
            {
                GameObject slot = filledSlots[i];
                if (slot == null) continue;
                if (!ids.Add(slot.GetInstanceID())) return -1;
                if (available < 0 && !slot.activeSelf) available = i;
            }
            return available;
        }

        private bool RejectExternal(string reason)
        {
            LastExternalError = reason;
            Debug.LogWarning("[FR5 External Input] Rejected: " + reason, this);
            return false;
        }

        private IEnumerator RunExternalJig(Transform jig, GameObject camera, int sourceSlot)
        {
            Quaternion releaseRotation = jig.rotation;
            float began = Time.time;
            while (Time.time - began < Fr5DwellSeconds)
            {
                if (jig == null || !jig.gameObject.activeInHierarchy || Vector3.Distance(jig.position, AcceptedWorldPosition) > 0.00001f ||
                    Quaternion.Angle(jig.rotation, releaseRotation) > 0.01f)
                {
                    FailExternalJig("Jig changed during dwell; external follow/physics may still be active.");
                    yield break;
                }
                yield return null;
            }
            if (jig == null || !jig.gameObject.activeInHierarchy || Vector3.Distance(jig.position, AcceptedWorldPosition) > 0.00001f ||
                Quaternion.Angle(jig.rotation, releaseRotation) > 0.01f)
            {
                FailExternalJig("Jig changed at the dwell boundary.");
                yield break;
            }
            LastDwellSeconds = Time.time - began;
            int filledBefore = FilledSlotCount;
            int completedBefore = completedJigCount;
            camera.SetActive(false);
            // Drive nested iterators here so an exception cannot leave an unreported busy hand-off.
            var stack = new Stack<IEnumerator>();
            stack.Push(RunJigCycle(jig, camera, sourceSlot, true));
            while (stack.Count > 0)
            {
                object yielded = null;
                bool moved = false;
                string failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) yielded = stack.Peek().Current; }
                catch (System.Exception ex) { failure = ex.Message; }
                if (failure != null) { FailExternalJig(failure); yield break; }
                if (!moved) { stack.Pop(); continue; }
                if (yielded is IEnumerator nested) stack.Push(nested);
                else yield return yielded;
            }
            if (completedJigCount != completedBefore + 1 || FilledSlotCount != filledBefore + 1)
            {
                FailExternalJig("Existing SMT cycle did not complete exactly one Finish slot.");
                yield break;
            }
            ExternalCompletedCount++;
            externalPhase = "Completed";
            externalBusy = false;
            externalJig = null;
            runningRoutine = null;
            Debug.Log("[FR5 External Input] Completed Slot " + sourceSlot + ", Jig instance " +
                LastExternalJigId + ", dwell=" + LastDwellSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                "s; no automatic next Jig.", this);
        }

        private void FailExternalJig(string reason)
        {
            LastExternalError = reason;
            externalPhase = "Failed";
            if (externalJig != null) activeRuntimeJigs.Remove(externalJig.gameObject);
            externalBusy = false;
            externalJig = null;
            runningRoutine = null;
            inspectionBusy = false;
            unloaderBusy = false;
            Debug.LogError("[FR5 External Input] STOP: " + reason + ". No replacement Jig or layout adjustment.", this);
        }

        public void AbortExternalJig(string reason)
        {
            if (!externalBusy) return;
            if (runningRoutine != null) StopCoroutine(runningRoutine);
            FailExternalJig(reason);
        }

        private void OnDisable()
        {
            if (externalBusy) AbortExternalJig("Controller disabled before hand-off completion.");
        }

        private bool HasRequiredReferences()
        {
            return runtimeJig != null && runtimeCameraModuleArray != null &&
                conveyorStart != null && HasProcessReferences();
        }

        private bool HasProcessReferences()
        {
            return
                conveyorEnd != null &&
                mounterEntry != null &&
                mounterProcess != null &&
                inspectionEntry != null &&
                inspectionProcess != null &&
                unloaderStart != null &&
                unloaderEnd != null &&
                runtimeMagazineLift != null &&
                magazineLowerPose != null &&
                magazineUpperPose != null &&
                conveyor01Light != null &&
                mounterLight != null &&
                inspectionLight != null &&
                conveyor02Light != null &&
                unloaderLight != null;
        }
    }
}
