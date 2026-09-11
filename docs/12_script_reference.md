# 12. Script Reference

## 문서 목적

이 문서는 저장소 전체를 파일 목록처럼 나열하기보다 실제 구현에서 중심 역할을 한 Python, C#, Launch, YAML, World 파일의 책임과 연결 관계를 정리합니다.

## Ubuntu / ROS2 / MoveIt2

### `slot01_to_slot08_final_one_take.py`

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

| 항목 | 내용 |
|:---|:---|
| 역할 | Magazine Slot01~07 최종 Pick & Place Motion Master |
| 입력 | `FR5_TAKE`, 현재 Joint State, Slot/Jig 설정, Planning Scene |
| 출력 | MoveIt Trajectory, Gripper/Jig/Conveyor 동작 |
| 연결 | MoveIt2, Gazebo, TF, Joint State |
| 검증 | TAKE1~07 최종 PASS, Negative J6 Guard |

주요 내부 책임:

- Planning Scene / Joint State 사전 확인
- Slot별 PREGRASP / Pick
- Cartesian Extract / Insert
- Gripper Open/Close
- LIVE TF Jig Follower
- Trajectory Negative J6 검사
- Release / Retreat / Conveyor Sequence

### Slot Motion YAML

```text
src/fr5_moveit_config/config/slot01_to_slot08_final_one_take_v1.yaml
```

Python 로직과 Slot별 데이터를 분리합니다.

주요 데이터:

- Slot/Jig Label
- Slot Height / Pose 기준
- PREGRASP / Insert 설정
- Negative-J6 Seed Family
- Conveyor / Release 기준

### `fr5_workcell.launch.py`

Gazebo FR5 Workcell을 실행하는 Launch 기준입니다. Robot, Table, Magazine, Conveyor, Jig가 포함된 Simulation Environment를 시작합니다.

### `move_group.launch.py`

MoveIt2 Planning/Execution 계층을 실행합니다. Planning Scene과 Robot Model을 기준으로 Joint/Cartesian Motion을 생성합니다.

### `moveit_rviz.launch.py`

MoveIt/RViz 시각 검증에 사용합니다. RViz 표시는 Runtime 상태와 차이가 날 수 있으므로 `/joint_states`와 실제 Gazebo 상태를 함께 확인합니다.

### Gazebo World / SDF

```text
src/fr5_gazebo/worlds/fr5_workcell.sdf
```

Robot Table, Magazine, Conveyor, Jig Inventory 등 Workcell Simulation 배치를 정의합니다.

## Unity Runtime Sync / ROS2

### `scr_FR5Ros2JointStateClient.cs`

| 항목 | 내용 |
|:---|:---|
| 역할 | ROS2 `/joint_states` 수신 |
| 입력 | JointState Message |
| 출력 | FR5 J1~J6 Joint 값 |
| 연결 | Runtime Sync Manager |

Joint Name을 기준으로 FR5 Joint 값을 추출하고 Unity Runtime 계층으로 전달합니다.

### `scr_FR5RuntimeSyncManager.cs`

Manual/Test와 External Feedback 중 현재 Joint Pose 소유자를 관리합니다. 두 입력이 동시에 FR5 Transform을 갱신하지 않도록 Runtime Mode를 분리합니다.

### `scr_VirtualJointController.cs`

수신한 Joint 값을 Unity FR5 J1~J6 Transform의 Local Rotation으로 적용합니다.

### `scr_FR5Ros2CommandPublisher.cs`

Unity에서 생성된 Robot Command를 ROS2 Command Topic으로 Publish합니다. 실제 SDK 실행은 Ubuntu Listener/Robot Interface 계층과 분리합니다.

### `scr_FR5UICommandRouter.cs`

UI Button 입력을 직접 Robot API로 보내지 않고 Command 구조로 변환해 Publisher에 전달합니다.

### `scr_FR5RobotManualController.cs`

외부 Feedback 연결 전 Unity 내부에서 Robot Joint/UI 동작을 확인하기 위한 Manual/Test Controller입니다. Actual Robot Command 경로와 분리합니다.

## Unity Workcell

### `MagazineConveyorController.cs`

Source Magazine 공급 및 Magazine Conveyor 흐름을 관리합니다. Active Magazine과 공정 공급 상태를 제어합니다.

### `FR5JigTransferCoordinator.cs`

Jig Visual Ownership의 핵심 Coordinator입니다.

```text
Source
→ Carried
→ Runtime SMT
→ Finish
```

`PICK_DONE`, `PLACE_DONE` 등 Workcell Event에 따라 이전 소유자를 비활성화하고 다음 소유자로 넘깁니다.

### `EquipmentProcessSequenceController.cs`

| 항목 | 내용 |
|:---|:---|
| 역할 | SMT Jig Process Sequence |
| 범위 | Conveyor01 → Mounter → Inspection → Conveyor02 → Unloader → Finish |
| 이동 | 공통 0.15 m/s World-space Translation |
| Finish | Unloader Rotation 유지 Straight Insert |
| 상태 | Static Compile/Offline Contract 완료, Play Mode 최종 확인 예정 |

Process Dwell과 Jig Translation을 분리해, 이동 거리에 따라 `duration = distance / speed`로 계산합니다.

### `FR5InsertedJigTestTrigger.cs`

External FR5 Insert Test와 Finish 결과 검증을 담당합니다.

검증 항목:

- Source 7 invariant
- Slot08 EMPTY
- Finish Position / Height
- Runtime/Placeholder overlap
- Ownership Handoff
- Finish Slot Count
- Insertion Rotation Drift `<= 0.01°`

검증 결과는 JSON/Report 형태로 남길 수 있도록 구성합니다.

## Unity Editor / Validation

### `FR5SourceFinishSetup.cs`

Source/Finish 상태를 Scene Save 없이 구성/검증하는 Editor Utility입니다.

주요 기준:

- Source Slot01~07 유지
- Slot08 EMPTY
- Legacy Jig Visual 숨김
- Finish Placeholder Orientation 기준
- Robot/Table/설비 보호 Transform 확인
- 보호 위반 시 STOP/Undo

### `FR5LiveSceneAlignmentAudit.cs`

Robot/Workcell 정렬과 보호 기준을 확인하는 Audit Script입니다. Unity 시각 보정을 위해 검증된 Robot/설비 Transform이 함께 바뀌는 것을 막기 위한 확인 도구로 사용합니다.

## 주요 연결 관계

```text
ROS2 /joint_states
  ↓
scr_FR5Ros2JointStateClient
  ↓
scr_FR5RuntimeSyncManager
  ↓
scr_VirtualJointController
  ↓
FR5 Joint Hierarchy
```

```text
Unity UI
  ↓
scr_FR5UICommandRouter
  ↓
scr_FR5Ros2CommandPublisher
  ↓
Ubuntu ROS2 Listener
  ↓
FR5 SDK
```

```text
PICK_DONE / PLACE_DONE
  ↓
FR5JigTransferCoordinator
  ↓
EquipmentProcessSequenceController
  ↓
FR5InsertedJigTestTrigger
  ↓
Finish Magazine
```

---

[문서 목차](README.md) · [07. Project Structure](07_project_structure.md) · [프로젝트 README](../README.md)
