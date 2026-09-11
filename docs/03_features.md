# 03. 주요 기능

## ROS2 / Gazebo / MoveIt2

### Gazebo Workcell

FR5 Robot을 기준으로 Robot Table, Magazine, Magazine Conveyor, Jig Place Conveyor, Jig Inventory를 배치해 Pick & Place 공정을 검증할 수 있는 Workcell을 구성했습니다.

FR5 Base는 고정 기준으로 유지하고 설비 배치나 좌표 정합을 위해 Robot Geometry/Link 구조를 변경하지 않았습니다.

### MoveIt Planning Scene

Gazebo의 주요 설비를 MoveIt Collision Object로 대응시켜 Motion Planning 중 설비 간섭을 확인할 수 있도록 구성했습니다.

Allowed Collision Matrix는 필요한 예외만 허용하고 Table/설비 전체를 일괄 Ignore하지 않았습니다.

### Slot01~07 One-Take

최종 Master Script:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

주요 흐름:

```text
Gripper Open
→ PREGRASP / PICK
→ Gripper Close
→ Extract
→ Carry
→ Final Pre-Insert
→ Straight Insert
→ Gripper Open
→ Retreat
→ Conveyor Release
```

Slot01은 검증된 Direct Pick 계열을 유지하고 Slot02 이상은 PREGRASP 후 짧은 Cartesian Approach를 적용했습니다.

### Cartesian Motion

직선성이 필요한 다음 구간은 Cartesian Path를 우선 적용했습니다.

- Pick 직전 접근
- Magazine Extract
- Final Pre-Insert → Insert
- Release 후 Retreat

실행 전 Cartesian Fraction을 확인하고 Full Path가 나오지 않는 Branch는 사용하지 않았습니다.

### Tool-to-Jig / Rigid Follower

높은 Slot에서는 Tool Pose 단순 복사 대신 검증된 Tool-to-Jig Relative Transform과 Target Jig Pose를 이용해 Target Tool Pose를 계산했습니다.

Jig를 최종 위치로 Teleport하지 않고 LIVE TF 기반 rigid follower로 Tool 이동을 따라가도록 구성했습니다.

### Negative J6 Constraint

높은 Slot에서 IK Wrist Branch가 변경되는 문제를 방지하기 위해 Trajectory 마지막 Point뿐 아니라 모든 Point에 대해 J6 Negative 여부를 검사했습니다.

```text
J6 < 0
```

최종 Take1~Take7에서 이 조건을 유지했습니다.

## Unity Digital Twin

### ROS2 Joint State Sync

`/joint_states`를 받아 FR5 J1~J6 Joint Transform의 Local Rotation에 적용합니다.

Robot Root를 매 Frame 이동시키지 않고 실제 Joint Hierarchy를 유지한 상태에서 Pose를 구성했습니다.

### Source / Finish Magazine

Source Magazine과 Finish Magazine의 역할을 분리했습니다.

Source 기준:

```text
Slot01~Slot07 : Jig 사용
Slot08        : EMPTY
```

Finish Magazine은 SMT Process 종료 후 Runtime Jig를 최종 Slot Visual로 Handoff하는 구조입니다.

### Jig Visual Ownership

```text
Source Slot Jig
   ↓ PICK_DONE
Carried Jig
   ↓ PLACE_DONE
SMT Runtime Jig
   ↓ Finish Handoff
Finish Jig
```

상태 전환 시 이전 Visual을 비활성화하고 다음 Visual을 활성화해 동일 Jig가 여러 위치에 동시에 보이는 문제를 방지했습니다.

### Magazine Conveyor

`MagazineConveyorController.cs`에서 Magazine 공급을 다음 Path로 관리합니다.

```text
Path_00_Start
→ Path_01_Middle
→ Path_02_Active
→ Path_03_Exit
```

### SMT Process

`EquipmentProcessSequenceController.cs`에서 다음 공정 흐름을 관리합니다.

```text
EQ_Conveyor_01
→ Mounter
→ Inspection
→ EQ_Conveyor_02
→ Unloader
→ Finish Magazine
```

Jig Transfer, Process Dwell, Unloader, Finish Handoff를 하나의 공정 순서 안에서 관리하되 Robot Joint Motion은 Unity에서 재계산하지 않습니다.

### External FR5 Input

실제 ROS/FR5가 연결되지 않은 상태에서도 FR5 Place 이후 Unity SMT Process만 독립적으로 검증할 수 있도록 External Input Mode를 분리했습니다.

## FR5 SDK / Robot Interface

### Read-only Feedback

실제 장비 연결에서는 Command보다 Joint/Robot State 확인을 먼저 구성했습니다.

- Joint Value
- Robot State
- Connection State
- Unity Runtime Feedback

### Command Routing

```text
Unity UI
→ Command Router
→ ROS2 Command Publisher
→ Ubuntu Listener
→ Controller / MoveIt / Robot Interface
```

MOVE_J, HOME, RESET, STOP 등의 Command를 Listener에서 구분하고 Command Status를 별도로 반환하도록 구성했습니다.

### Mode Separation

Manual/Test, Simulation Feedback, Actual Robot Feedback가 서로 값을 덮어쓰지 않도록 Runtime Mode를 분리했습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
