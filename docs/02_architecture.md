# 02. 시스템 아키텍처

## 전체 연결 구조

```text
FAIRINO FR5
    │
    │ FR5 SDK
    ▼
Robot Interface
    │
    ▼
   ROS2
    │
 ┌──┴────────────┐
 ▼               ▼
MoveIt2        Gazebo
Motion         Workcell / Physics
Planning           │
 └───────┬─────────┘
         ▼
  Joint State / Event
         │
         ▼
       Unity
 Digital Twin / UI / Process
```

## 계층별 역할

| 계층 | 역할 |
|:---|:---|
| FAIRINO FR5 / SDK | 실제 Robot State 및 Command Interface |
| ROS2 | Joint State, Command, Status, Workcell Event 전달 |
| MoveIt2 | IK, Joint/Cartesian Planning, Trajectory 실행 |
| Gazebo | Robot과 Workcell의 물리 상태 및 Jig 이동 검증 |
| Unity | Robot 상태 시각화, UI, Workcell Runtime, SMT Process |

## ROS2 구조

ROS2는 Simulation, Unity, Robot Interface 사이를 연결하는 중간 계층으로 사용했습니다.

주요 데이터는 다음과 같습니다.

- `/joint_states`
- Unity Command Topic
- Command Status Topic
- Workcell Event
- TF

로컬 FR5 Simulation은 주로 `ROS_DOMAIN_ID=90`에서 운영하고, Unity 또는 외부 PC 연결 시 Network Mode를 별도로 사용했습니다.

## MoveIt2 / Gazebo 책임 경계

Gazebo에 설비가 존재하는 것만으로 MoveIt Collision이 구성되지 않기 때문에 Planning Scene에 별도의 Collision Object를 생성했습니다.

주요 Planning Scene Object:

```text
gazebo_fr5_robot_table_v2
gazebo_fr5_magazine_visual_probe
gazebo_fr5_magazine_conveyor_probe
gazebo_fr5_jig_place_conveyor_probe
```

Gazebo와 MoveIt의 Z 기준 차이는 Planning Scene 변환에서 보정하고 FR5 Robot Base는 고정했습니다.

## Unity 내부 계층

```text
ROS2 / SDK Feedback
        ↓
scr_FR5Ros2JointStateClient
        ↓
scr_FR5RuntimeSyncManager
        ↓
scr_VirtualJointController
        ↓
Unity FR5 J1~J6
```

UI Command는 별도 경로로 분리했습니다.

```text
UI
 ↓
scr_FR5UICommandRouter
 ↓
scr_FR5Ros2CommandPublisher
 ↓
ROS2 Command
 ↓
Ubuntu Listener / Controller
```

Unity UI가 SDK 함수나 MoveIt 실행 세부 구현에 직접 의존하지 않도록 Router/Publisher 계층을 분리했습니다.

## Laptop Simulation Runtime

최종 Simulation Runtime은 노트북 Ubuntu에서 독립 실행할 수 있도록 구성했습니다.

```text
Laptop Ubuntu / ROS2 Jazzy
├─ Gazebo Sim 8 + ros2_control
├─ MoveIt2
├─ RViz2
├─ Final TAKE Master
└─ ROS-TCP Endpoint
       ↓
Windows Development PC
└─ Unity Digital Twin
```

Gazebo physics / controller 실행과 MoveIt Planning은 노트북에서 담당하고,
Unity는 ROS2 JointState를 받아 Digital Twin을 시각화하는 Runtime Source로 연결합니다.

실제 FR5 SDK Source와 ROS2 Simulation Source가 동시에 같은 Unity Joint를
구동하지 않도록 Runtime Source ownership을 분리합니다.

노트북 Simulation Runtime의 설치·headless·RTF·TAKE 재검증은
[15. Laptop ROS2 Simulation Runtime](15_laptop_ros2_simulation_runtime.md)에 정리했습니다.

## Simulation / Actual Robot 분리

```text
Simulation
Gazebo / MoveIt2
      ↓
ROS2 Joint State
      ↓
Unity
```

```text
Actual Robot
FR5
 ↓
FR5 SDK
 ↓
Bridge / ROS2
 ↓
Unity
```

Simulation PASS와 Actual Robot PASS를 같은 상태로 기록하지 않고, 실제 장비 검증이 필요한 항목은 별도로 관리했습니다.

## Workcell 책임 분리

Unity Workcell에서는 Robot Motion을 다시 계산하지 않고 FR5 이후 공정과 Visual State를 관리합니다.

```text
FR5 Place Event
      ↓
Jig Ownership Handoff
      ↓
SMT Process Controller
      ↓
Finish Magazine
```

이 구조로 Robot Motion, Physics, Process Visualization의 책임을 분리했습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
