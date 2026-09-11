# 07. 프로젝트 구조

## 전체 구조

프로젝트는 Ubuntu ROS2 Workspace와 Windows Unity Project를 분리해 개발했습니다.

```text
Ubuntu
~/fr5_ros2_ws
└─ src/
   ├─ fr5_gazebo/
   ├─ fr5_moveit_config/
   └─ ROS2 bridge / interface packages

Windows
FAIRINO_FR5_DigitalTwin/
├─ Assets/
│  ├─ Model/
│  └─ Project/
│     ├─ Art/
│     ├─ Prefabs/
│     ├─ Scenes/
│     └─ Scripts/
├─ Packages/
└─ ProjectSettings/
```

## Ubuntu / Python / ROS2

### `slot01_to_slot08_final_one_take.py`

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

최종 Magazine Slot Motion Master입니다.

주요 역할:

- Slot/Jig Model 선택
- Planning Scene 확인
- Current Joint State 확인
- PREGRASP / PICK
- Gripper Close/Open
- Cartesian Extract
- Carry
- Final Pre-Insert / Straight Insert
- LIVE TF Jig Follower
- Negative J6 Trajectory Check
- Retreat
- Conveyor Release / Jig Hide

실행 예:

```bash
FR5_TAKE=1 python3 -u \
  src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py \
  --execute
```

Plan-only와 실제 Gazebo Movement를 `--execute`로 구분합니다.

### `slot01_to_slot08_final_one_take_v1.yaml`

```text
src/fr5_moveit_config/config/slot01_to_slot08_final_one_take_v1.yaml
```

Slot별 Motion 기준과 설정을 Python Logic에서 분리해 관리하는 파일입니다.

주요 항목:

- Jig Model Name / Slot Label
- Slot별 Height / Pose 기준
- Negative-J6 Seed Family
- Validated Place Route
- Insert / Conveyor Release 설정

### `moveit_rviz.launch.py`

```text
src/fr5_moveit_config/launch/moveit_rviz.launch.py
```

MoveIt/RViz 실행 및 Motion 관련 Node/Executable 구성에 사용합니다.

### `fr5_workcell.sdf`

```text
src/fr5_gazebo/worlds/fr5_workcell.sdf
```

FR5 Workcell World 기준 파일입니다. Robot Table, Magazine, Conveyor, Jig Inventory 등의 Simulation 배치를 구성합니다.

### ROS2 Bridge / Listener

Python ROS2 Node는 다음 역할로 사용했습니다.

- Unity Command Listener
- Workcell Event Bridge
- Command Status Publish
- Joint/State 확인
- Gazebo Model Pose 조회/제어
- Validation Utility

## Unity C# Script 구조

Unity Script는 크게 Runtime Sync, Robot Control, Workcell Process, Editor/Validation으로 나눴습니다.

### Runtime Sync / ROS2

| Script | 역할 |
|:---|:---|
| `scr_FR5Ros2JointStateClient.cs` | `/joint_states` 수신, J1~J6 값 추출 |
| `scr_FR5RuntimeSyncManager.cs` | Manual/Test/External Feedback 상태 관리 |
| `scr_VirtualJointController.cs` | Unity J1~J6 Local Rotation 적용 |
| `scr_FR5Ros2CommandPublisher.cs` | Unity Command를 ROS2 Topic으로 Publish |
| `scr_FR5UICommandRouter.cs` | UI 요청을 Command 구조로 전달 |

### Robot Control

| Script | 역할 |
|:---|:---|
| `scr_FR5RobotManualController.cs` | 연동 전 Joint/Robot Manual Test |
| `scr_VirtualJointController.cs` | FR5 Virtual Joint Pose 적용 |

### Workcell Process

| Script | 역할 |
|:---|:---|
| `MagazineConveyorController.cs` | Raw Magazine 공급 Path 및 Active Magazine 관리 |
| `FR5JigTransferCoordinator.cs` | Source/Carried/Runtime Jig Ownership 전환 |
| `EquipmentProcessSequenceController.cs` | Conveyor → Mounter → Inspection → Unloader → Finish 공정 제어 |
| `FR5InsertedJigTestTrigger.cs` | External/Inserted Jig Test 및 Finish 검증 |

### Editor / Validation

| Script | 역할 |
|:---|:---|
| `FR5SourceFinishSetup.cs` | Source/Finish 상태 구성 및 No-Save 검증 메뉴 |
| `FR5LiveSceneAlignmentAudit.cs` | Scene 정렬/보호 기준 확인 |

## 주요 Script 연결

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
PICK_DONE / PLACE_DONE
        ↓
FR5JigTransferCoordinator
        ↓
EquipmentProcessSequenceController
        ↓
Finish Magazine
```

```text
Unity UI
   ↓
scr_FR5UICommandRouter
   ↓
scr_FR5Ros2CommandPublisher
   ↓
Ubuntu ROS2 Listener
```

## 공개 저장소 기준

- Unity Generated Folder(`Library`, `Temp`, `Logs`, `UserSettings` 등)는 Git에서 제외
- 내부 Backup/Recovery Artifact는 공개 저장소 기준에서 제외
- 실제 장비 내부 정보, 사설 IP, 계정/식별정보는 문서에 기록하지 않음
- 대표 이미지와 Demo Video는 최종 연동 검증 후 추가 예정

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
