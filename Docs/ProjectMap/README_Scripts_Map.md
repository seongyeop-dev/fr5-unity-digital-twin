# FR5 Unity Scripts Map

이 문서는 `Unity/FAIRINO_FR5_DigitalTwin/Assets/Project/Scripts` 내부 구조를 구분하기 위한 문서입니다. 파일 이동 없이 문서상으로만 역할을 분류합니다.

## Common Tags

- `ACTIVE`
- `SUPPORT`
- `VALIDATION`
- `DEBUG`
- `STANDBY`
- `LEGACY_CANDIDATE`
- `DO_NOT_DELETE`

## Script Folder Map

| Path | Sub Group | Status | Role |
|---|---|---|---|
| `Assets/Project/Scripts/Core/Kinematics` | Core/Kinematics | `ACTIVE` / `VALIDATION` | C# FK, pose result, coordinate mapping |
| `Assets/Project/Scripts/Core/Runtime` | Core/Runtime | `ACTIVE` / `STANDBY` | Runtime source selection, SDK/Bridge abstraction |
| `Assets/Project/Scripts/RuntimeSources/ROS2` | RuntimeSources/ROS2 | `ACTIVE` | ROS2 `/joint_states` subscribe, `/fr5/unity_command` publish |
| `Assets/Project/Scripts/RuntimeSources/Replay` | RuntimeSources/Replay | `ACTIVE` / `SUPPORT` | ROS2 bag-derived replay JSON playback |
| `Assets/Project/Scripts/Robot/Control` | Robot/Control | `ACTIVE` | Virtual robot joint control and manual controller |
| `Assets/Project/Scripts/Robot/Debug` | Robot/Debug | `DEBUG` | Axis compare/debug inspection |
| `Assets/Project/Scripts/Robot/Visualization` | Robot/Visualization | `ACTIVE` / `DEBUG` | Shadow pose, visual chain, mesh attachment |
| `Assets/Project/Scripts/UI/Actions` | UI/Actions | `ACTIVE` | UI button command routing |
| `Assets/Project/Scripts/UI/Panels` | UI/Panels | `ACTIVE` | Runtime status, joint panel, bottom bar, camera/top UI |
| `Assets/Project/Scripts/UI/Bindings` | UI/Bindings | `SUPPORT` | UI data binding helpers |
| `Assets/Project/Scripts/Core/Validation` | Core/Validation | `VALIDATION` / `DEBUG` | Python runner, FK validator, TCP compare, reports |
| `Assets/Project/Scripts/Editor` | Editor | `SUPPORT` | Missing script finder, material repair tools |
| `Assets/Project/Scripts/New` | New | `LEGACY_CANDIDATE` | 과거 staging/잔여 후보, 확인 전 삭제 금지 |

## ACTIVE Core Files

아래 파일은 현재 Runtime active flow와 직접 관련되므로 `DO_NOT_DELETE`입니다.

- `Assets/Project/Scripts/Core/Runtime/scr_FR5RuntimeSyncManager.cs`
- `Assets/Project/Scripts/RuntimeSources/ROS2/scr_FR5Ros2JointStateClient.cs`
- `Assets/Project/Scripts/RuntimeSources/ROS2/scr_FR5Ros2CommandPublisher.cs`
- `Assets/Project/Scripts/UI/Actions/scr_FR5UICommandRouter.cs`
- `Assets/Project/Scripts/Robot/Control/scr_VirtualJointController.cs`
- `Assets/Project/Scripts/Robot/Control/scr_FR5RobotManualController.cs`
- `Assets/Project/Scripts/RuntimeSources/Replay/scr_FR5UnityReplayJointStateSource.cs`
- `Assets/Project/Scripts/UI/Panels/scr_FR5RuntimeStatusPanelUI.cs`
- `Assets/Project/Scripts/UI/Panels/scr_FR5JointPanelUI.cs`
- `Assets/Project/Scripts/UI/Panels/scr_FR5BottomBarUI.cs`

## Notes

- Scene 참조 가능성이 있는 `.cs` 파일은 `.meta`와 함께 관리되어야 합니다.
- Unity Editor 밖에서 스크립트를 이동하면 GUID/Scene 참조 문제가 생길 수 있습니다.
- `New` 폴더는 이름상 legacy 후보지만 삭제 대상이 아닙니다. Scene 참조 여부 확인 전까지 `Keep for now`입니다.
