# 08. ROS2 / Gazebo / MoveIt2 상세

## 역할 분리

이 프로젝트에서 ROS2, Gazebo, MoveIt2는 같은 일을 중복해서 수행하지 않도록 역할을 분리했습니다.

| 계층 | 담당 범위 |
|:---|:---|
| ROS2 | Joint State, Command, Event, TF 전달 |
| Gazebo | Robot/Workcell 물리 상태와 Jig/Conveyor 표현 |
| MoveIt2 | Joint/Cartesian Motion Planning, Planning Scene, Trajectory 실행 |
| Unity | 시각화, UI, 공정 상태, 외부 입력 표현 |

Unity의 화면을 맞추기 위해 Gazebo Robot Base나 검증된 Motion을 바꾸지 않는 것을 기본 원칙으로 두었습니다.

## Workcell 기준

Gazebo Workcell에는 FR5, Robot Table, Magazine, Magazine Conveyor, Jig Place Conveyor와 Jig Model이 포함됩니다. MoveIt Planning Scene에는 같은 설비를 Collision Object로 구성하고, Gazebo와 MoveIt 사이의 기준 차이는 Planning Scene 변환에서 보정했습니다.

FR5 Base는 Workcell 기준으로 고정하며, 설비 정합 문제를 Robot Base 이동으로 해결하지 않습니다.

주요 실행 구성은 다음과 같습니다.

```text
Gazebo
└─ fr5_workcell.launch.py

MoveIt2
├─ move_group.launch.py
└─ moveit_rviz.launch.py
```

## Unity Command Bridge와 Workcell Launch 구분

`fr5_workcell.launch.py`는 최종 Workcell 실행 기준이며, `src/fr5_gazebo/launch/fr5_gazebo_control.launch.py`에는 기존 Unity Command Listener가 포함됩니다. Listener parameter는 `command_topic=/fr5/unity_command`, `command_status_topic=/fr5/command_status`입니다.

`scripts/run_fr5_gazebo_command_bridge.sh`는 `${HOME}/fr5_ros2_ws` 환경을 source하고 bridge launch를 실행하며 `EXECUTE_UNITY_TRAJECTORY`를 지원합니다. 기존 명령의 Gazebo/controller 경로와 Simulation Master의 TAKE 실행은 별개입니다. 현재 Listener에는 `RUN_TAKE` dispatch가 없습니다.

Source commit, 보호 SHA 및 실행 경계는 [14. Deployment & Laptop Handoff](14_deployment_and_handoff.md)를 기준으로 합니다.

## Planning Scene

최종 Motion Master는 실행 전 Planning Scene과 현재 Joint State를 확인합니다. 주요 Workcell Geometry가 MoveIt Collision Object에 반영되어 있는지 확인한 뒤 Motion을 계획합니다.

Planning Scene에서 관리한 주요 대상은 다음 범주입니다.

- Robot Table
- Magazine / Magazine Conveyor
- Jig Place Conveyor
- Jig / Slot 접근 영역

Gazebo와 MoveIt의 Z 기준 차이는 MoveIt Collision Object 구성에서 보정했으며, Robot Link/Collision/Inertial Geometry 자체를 임의 수정하는 방식은 사용하지 않았습니다.

## Magazine Slot Motion

Slot Motion은 한 번에 Slot01~08을 동일한 Pose로 복사하지 않고, Slot 높이와 접근 위험을 기준으로 구분했습니다.

### Slot01

Slot01은 이미 검증된 Direct Pick 기준을 유지합니다. 이후 Slot 동작을 만들 때도 Slot01의 확정 Motion은 다시 튜닝하지 않았습니다.

### Slot02 이상

Magazine 상부로 갈수록 Frame 간섭 가능성이 증가해 다음 구조를 사용했습니다.

```text
PREGRASP
  ↓
짧은 Cartesian Approach
  ↓
GRASP
  ↓
Extract
  ↓
Carry
  ↓
Final Pre-Insert
  ↓
Straight Insert
  ↓
Release / Retreat
```

Slot별 최종 Pose를 단순히 Z Offset만 적용하는 것이 아니라, 접근 방향과 IK Branch까지 함께 확인했습니다.

## Jig Attach / Detach와 Follower

Pick 이후 Jig를 순간 이동시키는 방식 대신 Tool과 Jig 사이의 상대 Pose를 유지하는 rigid follower를 사용했습니다.

```text
Attach
→ Tool-to-Jig Relative Pose 저장
→ LIVE TF 기준 Jig Follower
→ Carry / Insert
→ Detach
→ Conveyor 이동
```

이 방식은 Robot Motion과 Jig 시각/물리 표현이 분리되는 문제를 줄이고, Carry 중 Jig가 Gripper를 따라가는 상태를 일관되게 유지하기 위한 구조입니다.

## Negative J6 정책

IK 끝점만 정상이어도 중간 Trajectory에서 Wrist Branch가 바뀔 수 있어 모든 Trajectory Point에 대해 J6 값을 확인합니다.

최종 운영 정책은 다음과 같습니다.

```text
require_negative_j6_trajectory
→ every trajectory point: J6 < 0
```

Slot02~상위 Slot은 Negative-J6 Seed Family를 사용하고, 검증된 Wrist Branch를 유지하도록 했습니다.

## ACTION05 사용 범위

ACTION05는 모든 Slot에서 공통으로 사용하지 않습니다.

| Slot | ACTION05 |
|:---|:---:|
| Slot01 | 사용 |
| Slot02 | 사용 |
| Slot03~07 | 미사용 |
| Slot08 | 운영 제외 |

Slot03~07은 최종 One-Take에서 ACTION05가 없어도 안정적으로 동작하는 경로를 사용합니다.

## Motion Master

최종 Slot Motion은 신규 Runner를 계속 추가하는 방식이 아니라 하나의 Master 파일에 누적했습니다.

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

최종 검증 SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

TAKE1~TAKE7은 최종 재실행까지 완료했고, TAKE8은 최상단 간섭 위험 때문에 운영 범위에서 제외했습니다.

## 운영 시 확인 항목

- `/joint_states` 존재 여부
- Planning Scene 주요 Collision Object
- Current Joint State와 Seed Branch
- PREGRASP / Cartesian Path 성공 여부
- 모든 Trajectory Point의 Negative J6
- Jig Follower 상대 Pose
- Insert/Retreat 방향
- Release 후 Conveyor 이동

---

[문서 목차](README.md) · [11. Motion & Slot Validation](11_motion_and_slot_validation.md) · [프로젝트 README](../README.md)
