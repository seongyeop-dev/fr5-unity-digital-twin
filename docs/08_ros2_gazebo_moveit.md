<a id="top"></a>

# 08. ROS2 / Gazebo / MoveIt2

> Simulation 계층에서 FR5 Motion, Workcell Physics, Planning Scene을 어떻게 분리해 검증했는지 정리합니다.

[문서 목차](README.md) · [프로젝트 README](../README.md) · [Motion Validation](11_motion_and_slot_validation.md) · [Laptop Runtime](15_laptop_ros2_simulation_runtime.md)

## 역할

| 계층 | 담당 |
|:---|:---|
| ROS2 | State / Command / TF |
| Gazebo | Physics / Controller / Workcell |
| MoveIt2 | Planning / Collision / Trajectory |
| RViz2 | Planning Scene / Robot visualization |
| 통합 모션 시퀀스 | Slot sequence / guards / execution |

## Workcell

```mermaid
flowchart LR
    FR5["FR5"] --> TABLE["Robot Table"]
    FR5 --> MAG["Source Magazine"]
    MAG --> MC["Magazine Conveyor"]
    FR5 --> JC["Jig Place Conveyor"]
    JC -. "명시적 Jig 입력 경계" .-> SMT["Unity SMT Process"]
```

FR5 Base는 고정하고 설비 정합 문제를 Robot Base 이동으로 해결하지 않습니다.

## Planning Scene

주요 Collision Object:

- `gazebo_fr5_robot_table_v2`
- `gazebo_fr5_magazine_visual_probe`
- `gazebo_fr5_magazine_conveyor_probe`
- `gazebo_fr5_jig_place_conveyor_probe`

Gazebo와 MoveIt의 기준 차이는 Planning Scene 변환에서 처리합니다.

## Slot Motion

### Slot01

초기 검증 기준을 유지하는 Direct Pick 계열입니다.

### Slot02 이상

높이가 증가하는 Magazine에서 Frame 간섭 여유를 확보하기 위해 PREGRASP 후 짧은 Cartesian Approach를 사용합니다.

```mermaid
flowchart LR
    P["PREGRASP"] --> A["Cartesian Approach"]
    A --> G["GRASP"]
    G --> E["Extract"]
    E --> C["Carry"]
    C --> PI["Pre-Insert"]
    PI --> I["Straight Insert"]
    I --> R["Release / Retreat"]
```

## Cartesian 구간

- Pick 직전 접근
- Extract
- Final Pre-Insert → Insert
- Release 후 Retreat

Full path가 만들어지지 않는 branch는 실행하지 않습니다.

## Negative-J6 Guard

```text
require_negative_j6_trajectory
→ every trajectory point: J6 < 0
```

끝점뿐 아니라 전체 trajectory를 검사합니다.

## Jig Follower

```mermaid
flowchart LR
    AT["현재 Tool / Jig pose 읽기"] --> REL["고정 Tool-to-Jig Relative Pose"]
    REL --> TF["LIVE TF Follower"]
    TF --> CAR["Carry / Insert"]
    CAR --> DET["follower 중단 / Release"]
    DET --> CONV["Conveyor"]
```

LIVE Tool TF에 고정 상대변환을 합성해 Gazebo Jig pose를 갱신합니다. 최종 TAKE의 추종은 physics attach/detach만으로 이루어지는 방식과 다릅니다.

Gazebo release 이후 conveyor flow와 Unity의 외부 Jig 입력 API는 각각의 실행 경계입니다. JointState만으로 Unity SMT를 시작하지 않습니다.

## ACTION 단계 설명 원칙

ACTION05는 TAKE별 실행 시퀀스에서 다르게 적용되므로 Slot 범위 전체에 동일한 규칙으로 일반화하지 않았습니다. 실제 동작은 통합 모션 스크립트의 TAKE별 sequence를 기준으로 설명합니다.


## 통합 모션 실행 구조

[최종 Master](https://github.com/seongyeop-dev/fr5_ros2_ws/blob/feat/fr5-gazebo-jig-attach-detach/src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py)
TAKE1~TAKE7 Final Simulation PASS, TAKE8은 운영 제외입니다.

## Laptop Performance 문제 해결

GUI + RViz 동시 실행 시 RTF 저하로 Master wall-clock timeout이 먼저 발생했습니다. Robot pose를 재튜닝하지 않고 true headless Gazebo를 추가했습니다.

```bash
ros2 launch fr5_gazebo fr5_workcell.launch.py \
  gz_args:="-s -r"
```

| Runtime | RTF |
|:---|---:|
| Gazebo true headless | `0.998` |
| headless + MoveIt2 | `0.997` |

Motion 문제가 아니라 Runtime performance 문제로 분리해 해결했습니다.

## 운영 확인 항목

- `/joint_states`
- Controller active
- Planning Scene
- Current Joint State
- Cartesian fraction
- Negative J6
- Jig follower
- Insert / Retreat
- Conveyor release

---

## 문서 목차

### 기본 문서

| 번호 | 문서 | 내용 |
| ---: | --- | --- |
| **01** | [Overview](01_overview.md) | 프로젝트 배경과 핵심 결과 |
| **02** | [Architecture](02_architecture.md) | Simulation, Integration, Unity, 수학 검증 계층 |
| **03** | [Features](03_features.md) | 구현 기능과 책임 |
| **04** | [Data Flow](04_data_flow.md) | Joint, Command, Jig, Camera 흐름 |
| **05** | [Validation](05_validation.md) | 수학·시뮬레이션·Unity 검증 |
| **06** | [Project Scope](06_project_scope.md) | 프로젝트별 실행 환경과 설계 경계 |
| **07** | [Project Structure](07_project_structure.md) | 공개 소스와 별도 ROS2 저장소 |

### 상세 기술 문서

| 번호 | 문서 | 내용 |
| ---: | --- | --- |
| **08** | [ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) | Workcell, Planning, TF follower |
| **09** | [Unity Digital Twin](09_unity_digital_twin.md) | 핵심 Runtime Script 구조 |
| **10** | [FR5 SDK Interface](10_fr5_sdk_integration.md) | 실제 제어 경험과 Read-only Bridge |
| **11** | [Motion & Slot Validation](11_motion_and_slot_validation.md) | TAKE1~TAKE7 및 Trajectory 검사 |
| **12** | [Script Reference](12_script_reference.md) | 실제 코드 경로·역할·의존 관계 |
| **13** | [Design Decisions](13_design_decisions_and_issues.md) | 주요 문제와 해결 |
| **14** | [Deployment](14_deployment_and_handoff.md) | Unity·Python·Bridge·ROS2 실행 환경 |
| **15** | [Simulation Runtime](15_laptop_ros2_simulation_runtime.md) | Headless, RTF, 노트북 실행 결과 |
| **16** | [Camera & Recording](16_unity_camera_and_recording.md) | 시점 전환·관찰·녹화 설정 |

### 추가 자료

| 구분 | 바로가기 |
| --- | --- |
| **프로젝트** | [프로젝트 README](../README.md) · [전체 문서 인덱스](README.md) |
| **수학 · 기구학 검증** | [Validation](05_validation.md) · [Motion Validation](11_motion_and_slot_validation.md) · [MDH Parameters](../Python/Phase1_Kinematics/Python_MDH/fr5_mdh_params.py) · [Python FK Solver](../Python/Phase1_Kinematics/Python_MDH/fr5_fk_solver.py) · [Kinematics Tests](../Python/Phase1_Kinematics/Tests/) |
| **실제 FR5 · Cocktail Demo** | [Demo Hub](../demos/README.md) · [FR5 SDK Cocktail Robot Demo](../demos/01_fr5_sdk_cocktail_robot_demo/README.md) |
| **ROS2 Simulation** | [ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) · [fr5_ros2_ws Repository](https://github.com/seongyeop-dev/fr5_ros2_ws) |

[문서 목록으로 이동](README.md)
