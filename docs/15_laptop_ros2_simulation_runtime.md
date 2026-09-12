# 15. Laptop ROS2 Simulation Runtime

## 목적

개발 PC에서 확정한 FR5 Simulation Motion 기준을 노트북 Ubuntu 환경으로 이관한 뒤,
실제로 재구성·빌드·실행하고 Final TAKE1~TAKE7까지 다시 검증한 Runtime 구조를 정리합니다.

노트북은 단순한 Source 복사 대상이 아니라 ROS2 / Gazebo / MoveIt2 Simulation과
향후 Windows Unity 연결을 담당하는 독립 Runtime 환경입니다.

실제 FAIRINO FR5 Hardware 검증은 별도 단계이며,
이 문서의 PASS는 Laptop Simulation Runtime PASS를 의미합니다.

## Runtime Environment

| 항목 | 기준 |
|:---|:---|
| OS | Ubuntu 24.04.4 LTS |
| ROS2 | Jazzy |
| Gazebo | Gazebo Sim 8 |
| MoveIt | MoveIt2 |
| Control | ros2_control |
| Visualization | RViz2 |
| ROS Domain | `ROS_DOMAIN_ID=90` |
| Workspace | `~/fr5_ros2_ws` |
| Repository | `git@github.com:seongyeop-dev/fr5_ros2_ws.git` |
| Branch | `feat/fr5-gazebo-jig-attach-detach` |
| Laptop Final HEAD | `f02799cfd3126210ef72238990861c9c027c84af` |
| Remote parity | Local HEAD = origin branch HEAD |

## Laptop Runtime Architecture

```text
Laptop Ubuntu 24.04 / ROS2 Jazzy
│
├─ Gazebo Sim 8
│  ├─ FR5 Workcell
│  ├─ Source Magazine
│  │  ├─ Slot01 : Jig
│  │  ├─ Slot02 : Jig
│  │  ├─ Slot03 : Jig
│  │  ├─ Slot04 : Jig
│  │  ├─ Slot05 : Jig
│  │  ├─ Slot06 : Jig
│  │  ├─ Slot07 : Jig
│  │  └─ Slot08 : EMPTY
│  ├─ Magazine Conveyor
│  ├─ Jig Place Conveyor
│  └─ ros2_control
│
├─ MoveIt2
│  ├─ Motion Planning
│  ├─ Planning Scene
│  ├─ Collision Check
│  └─ Cartesian Path
│
├─ RViz2
│  └─ Robot / Planning Scene Visualization
│
├─ Final Motion Master
│  └─ TAKE1 → TAKE7
│
└─ ROS-TCP Endpoint
       │
       │ Network
       ▼
Windows Development PC
└─ Unity Digital Twin
```

ROS-TCP를 통한 Windows Unity 실시간 연결과 동시 촬영은 다음 Integration 단계입니다.
Laptop 쪽 ROS-TCP package와 실행 경로는 준비되어 있지만,
이 문서 작성 시점에는 Windows Development PC와의 최종 Live 연결 결과를 PASS로 기록하지 않습니다.

## Source / Build Migration

개발 PC의 `build/`, `install/`, `log/`를 복사하지 않고 노트북에서 Source 기준으로 fresh build했습니다.

노트북 기존 Workspace는 삭제하거나 reset하지 않고,
Git lineage와 dirty 상태를 먼저 확인한 뒤 fast-forward 가능한 상태에서만 최신 기준으로 동기화했습니다.

기존 local backup과 untracked 파일도 삭제하지 않고 보존했습니다.

최종 Laptop Runtime commit:

```text
f02799cfd3126210ef72238990861c9c027c84af
```

이 commit에는 노트북 headless Runtime 검증 과정에서 확인된 Gazebo launch argument 전달 수정이 포함됩니다.

## Gazebo Python Runtime Dependency

Final Master의 Simulation `--execute` 경로는 아래 Python binding을 사용합니다.

```python
from gz.msgs10.boolean_pb2 import Boolean
from gz.msgs10.pose_pb2 import Pose
from gz.transport13 import Node
```

노트북에는 초기 상태에서 ROS vendor C++ library는 존재했지만 Python `gz` module은 설치되어 있지 않았습니다.

Gazebo 공식 OSRF repository를 통해 다음 binding을 추가했습니다.

```text
python3-gz-msgs10
python3-gz-transport13
```

설치 후 Master가 사용하는 세 import와 `gz.transport13.Node()` 생성까지 PASS를 확인했습니다.

## True Headless Runtime

노트북에서 Gazebo GUI와 RViz를 동시에 실행했을 때 Simulation Real Time Factor가 크게 낮아졌고,
Master의 wall-clock 기반 execution timeout보다 Simulation 진행이 느려지는 현상을 확인했습니다.

Motion Pose나 Trajectory를 다시 튜닝하지 않고,
Gazebo를 server-only로 실행할 수 있도록 `gz_args` 전달 경로를 정리했습니다.

관련 Launch:

```text
src/fr5_gazebo/launch/fr5_workcell.launch.py
src/fr5_gazebo/launch/fr5_gazebo_control.launch.py
```

Headless 실행:

```bash
ros2 launch fr5_gazebo fr5_workcell.launch.py \
  gz_args:="-s -r"
```

실제 Process 기준:

```text
gz sim -s -r .../fr5_workcell.sdf
```

검증 결과:

| Runtime 구성 | RTF |
|:---|---:|
| Gazebo true headless | `0.998` |
| Gazebo true headless + MoveIt2 | `0.997` |

GUI 없는 실행에서도 `/clock`, `/joint_states`,
arm/gripper controller와 Gazebo physics가 정상 동작하는 것을 확인했습니다.

RViz와 Gazebo GUI는 별도로 정상 표시를 확인했으며,
포트폴리오 촬영 시에는 GUI/RViz/Unity viewer 부하와 Simulation 실행 검증을 구분합니다.

## Planning Scene Validation

MoveIt Planning Scene에 다음 네 개 Facility Collision Object만 존재하는 것을 확인했습니다.

| Object | Elements |
|:---|---:|
| `gazebo_fr5_robot_table_v2` | 6 |
| `gazebo_fr5_magazine_visual_probe` | 46 |
| `gazebo_fr5_magazine_conveyor_probe` | 7 |
| `gazebo_fr5_jig_place_conveyor_probe` | 200 |

검증 결과:

```text
OBJECT_COUNT=4
UNEXPECTED_WORLD_OBJECTS=NONE
```

Allowed Collision Matrix는 Robot Table과 `link1` 사이만 허용하고,
다른 주요 Robot Link에는 허용하지 않는 최종 정책을 유지했습니다.

```text
table <-> link1 = True
table <-> base_link/link2~link6/tool0 = False
```

## Source Magazine Inventory

Gazebo Runtime model list에서 다음 구조를 확인했습니다.

```text
Slot01 : fr5_jig_slot01_inventory
Slot02 : fr5_jig_slot02_inventory
Slot03 : fr5_jig_slot03_inventory
Slot04 : fr5_jig_slot04_inventory
Slot05 : fr5_jig_slot05_inventory
Slot06 : fr5_jig_slot06_inventory
Slot07 : fr5_jig_slot07_inventory
Slot08 : EMPTY
```

Magazine에는 Slot08 bracket geometry가 존재하지만,
최종 Workcell에서는 Slot08 Jig model을 spawn하지 않습니다.

Final Master도 `FR5_TAKE=1..7` 또는 `ALL`만 허용하며 TAKE8은 운영 대상이 아닙니다.

## Final TAKE1 → TAKE7 Revalidation

Laptop headless Gazebo + MoveIt2 환경에서 Final Master를 실제 Simulation execution으로 다시 실행했습니다.

Master:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

최종 실행 범위:

```text
FR5_TAKE=ALL
TAKE1 → TAKE7
TAKE8 = UNUSED
```

최종 결과:

```text
FINAL ONE-TAKE TAKE1 -> TAKE7 PASS
FINAL_MASTER_RETURN_CODE=0
TAKE1_TO_TAKE7_FINAL_SIMULATION=PASS
```

TAKE7에서도 Pick, Jig follower, Cartesian Extract, Final Pre-Insert,
Straight Insert, Release, Retreat, Conveyor까지 실행 PASS를 확인했습니다.

Motion 기준은 개발 PC에서 검증한 Master를 유지했으며,
노트북 성능 문제 해결을 위해 Pick Pose, IK Branch, Slot Motion,
Negative-J6 정책을 다시 튜닝하지 않았습니다.

## Locked Runtime Assets

| Asset | SHA256 |
|:---|:---|
| Final Motion Master | `80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be` |
| Slot YAML | `b823401d6c77037ec35502a8e11ac35692f6f4a86ff7bf6c8efb8825a3f486e6` |
| Workcell World | `dac1c53f068aa56dd497cf3f66e64559dda1af010584566c71d96bf95104be65` |
| Workcell Launch | `a6b8c094d9653ae3bc65fcd56df2714d912f5fee78bec51dd1e7c56b50daead6` |
| Gazebo Control Launch | `d2fd8e715b99ea1d65e1519b1cb8f198dfb09f8f48e61e31f13ded0f7edb907f` |

## Validation Boundary

현재 PASS:

- Laptop Source migration / Git parity
- Laptop fresh ROS2 build
- Gazebo Workcell Runtime
- ros2_control controllers
- `/clock`
- `/joint_states`
- MoveIt2
- Planning Scene
- RViz 표시
- Slot01~07 inventory
- Slot08 EMPTY
- Gazebo true headless
- Final TAKE1~TAKE7 Simulation execution

다음 Integration:

```text
Laptop Gazebo / MoveIt / RViz
        ↓ ROS-TCP
Windows Unity Digital Twin
        ↓
동시 화면 / 동작 촬영
```

그 이후 실제 Hardware 단계:

```text
Actual FAIRINO FR5
        ↓
SDK / Hardware Feedback & Command
        ↓
Unity Digital Twin
```

Simulation PASS, Unity Live Integration PASS,
Actual Robot PASS는 서로 다른 검증 상태로 관리합니다.

---

[문서 목차](README.md) ·
[02. Architecture](02_architecture.md) ·
[05. Validation](05_validation.md) ·
[14. Deployment & Laptop Handoff](14_deployment_and_handoff.md) ·
[프로젝트 README](../README.md)
