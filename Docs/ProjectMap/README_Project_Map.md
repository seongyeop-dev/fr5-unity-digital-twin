# FR5 Digital Twin Project Map

이 문서는 프로젝트 파일을 삭제하거나 이동하지 않고, 현재 구조를 사람이 빠르게 구분하기 위한 안내 문서입니다.

## Common Tags

- `ACTIVE`: 현재 실행 흐름에서 직접 사용
- `SUPPORT`: 실행 흐름을 보조하거나 향후 연결 후보
- `VALIDATION`: 수학 검증, Ground Truth, 비교 검증
- `DEBUG`: 디버그, 시각화, 점검 도구
- `STANDBY`: 현재 직접 흐름은 아니지만 다음 단계 후보
- `LEGACY_CANDIDATE`: 과거 실험/비활성/복구 후보, 삭제 전 확인 필요
- `DO_NOT_DELETE`: 지금 삭제하면 위험

## Top-Level Structure

| Path | Group | Status | Role |
|---|---|---|---|
| `Python` | PYTHON | `VALIDATION` / `SUPPORT` | FK, MDH, Ground Truth, Unity validation 자료 |
| `BridgeTools` | BRIDGE_TOOLS | `STANDBY` | C# Bridge source/executable, SDK 연결 전 단계 |
| `ThirdParty` | THIRD_PARTY | `SUPPORT` / `DO_NOT_DELETE` | Fairino C# SDK, external DLL/source |
| `Unity/FAIRINO_FR5_DigitalTwin` | UNITY | `ACTIVE` | Unity Digital Twin project |
| `FR Robots DH Transformation.xlsx` | PYTHON / VALIDATION | `DO_NOT_DELETE` | DH/MDH 기준 자료 |

## Group Roles

### PYTHON

`Python`은 FR5 수학 검증의 기준 계층입니다. Unity와 직접 연결되지 않는 파일도 FK, MDH, Ground Truth 검증에 필요할 수 있으므로 삭제하지 않습니다.

### ROS2

실제 ROS2 workspace는 Windows 프로젝트 루트가 아니라 Ubuntu의 `~/fr5_ros2_ws` 기준입니다. 이 Windows 프로젝트에는 ROS2 문서, Unity ROS2 client/publisher, replay data, `ROSConnectionPrefab`이 있습니다.

### UNITY

`Unity/FAIRINO_FR5_DigitalTwin`은 현재 Digital Twin 실행 프로젝트입니다. Scene, Prefab, `.meta`, model, material, texture, scripts는 Unity 참조 관계가 있으므로 임의 이동/삭제하지 않습니다.

### BRIDGE_TOOLS

`BridgeTools`는 Fairino SDK를 직접 붙이기 전 C# Bridge를 구성하기 위한 단계입니다. `bin`/DLL 산출물도 실행 의존 가능성이 있으므로 확인 없이 삭제하지 않습니다.

### THIRD_PARTY

`ThirdParty`는 외부 SDK와 예제 코드입니다. 직접 수정하지 않는 것을 원칙으로 하며, `bin`, `obj`, DLL도 의존성 확인 전에는 보존합니다.

## Active Flow 1: ROS2 to Unity

```text
/joint_states
→ scr_FR5Ros2JointStateClient
→ scr_FR5RuntimeSyncManager
→ scr_VirtualJointController
→ Unity Robot Visual
```

## Active Flow 2: Unity to ROS2

```text
Unity Button
→ scr_FR5UICommandRouter
→ scr_FR5Ros2CommandPublisher
→ /fr5/unity_command
→ fr5_ros2_bridge/fr5_unity_command_listener.py
→ dry-run logging
```

## Important Boundary

- Windows project root: `PROJECT_ROOT_260531/PROJECT_ROOT_260531`
- Unity project root: `Unity/FAIRINO_FR5_DigitalTwin`
- Ubuntu ROS2 workspace: `~/fr5_ros2_ws`

이 세 영역은 역할이 다릅니다. Windows 루트에 `fr5_ros2_ws`가 없더라도, Unity ROS2 연동 코드는 Unity project 안에 존재합니다.
