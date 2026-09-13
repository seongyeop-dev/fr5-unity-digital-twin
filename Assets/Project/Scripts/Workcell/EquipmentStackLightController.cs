using UnityEngine;

namespace FR5DigitalTwin.Workcell
{
    [DisallowMultipleComponent]
    public sealed class EquipmentStackLightController : MonoBehaviour
    {
        public enum StackLightState
        {
            Off,
            Ready,
            Processing,
            Fault
        }

        [Header("Lens Renderers")]
        [SerializeField] private Renderer yellowLens;
        [SerializeField] private Renderer greenLens;
        [SerializeField] private Renderer redLens;

        [Header("Runtime")]
        [SerializeField]
        private StackLightState initialState =
            StackLightState.Ready;

        private StackLightState currentState;

        public StackLightState CurrentState => currentState;

        private void Awake()
        {
            SetState(initialState);
        }

        private void OnValidate()
        {
            // Edit Mode에서는 렌즈 상태를 변경하지 않는다.
            // Play Mode에서 Inspector 값을 바꿀 때만 즉시 반영한다.
            if (!Application.isPlaying)
            {
                return;
            }

            SetState(initialState);
        }

        public void SetReady()
        {
            SetState(StackLightState.Ready);
        }

        public void SetProcessing()
        {
            SetState(StackLightState.Processing);
        }

        public void SetFault()
        {
            SetState(StackLightState.Fault);
        }

        public void SetOff()
        {
            SetState(StackLightState.Off);
        }

        public void SetState(StackLightState state)
        {
            currentState = state;

            SetLensVisible(
                yellowLens,
                state == StackLightState.Processing);

            SetLensVisible(
                greenLens,
                state == StackLightState.Ready);

            SetLensVisible(
                redLens,
                state == StackLightState.Fault);
        }

        private static void SetLensVisible(
            Renderer targetRenderer,
            bool visible)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.enabled = visible;
        }
    }
}