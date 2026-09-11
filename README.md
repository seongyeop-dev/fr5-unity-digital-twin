# FAIRINO FR5 Digital Twin

Unity, ROS2, Gazebo, MoveIt2, FAIRINO FR5 SDK를 연결해 FR5 협동로봇의 상태·모션·공정 흐름을 검증하고 시각화한 디지털 트윈 프로젝트입니다.

로봇 모델 표시에서 시작해 ROS2 Joint State 동기화, Gazebo Workcell, MoveIt2 Pick & Place, Magazine Slot 자동화, Unity SMT 공정, FR5 SDK 연동 구조까지 단계적으로 확장했습니다. 현재 시뮬레이션과 Unity 공정의 핵심 구조는 정리되어 있으며, 실제 FR5와의 최종 종단 연동과 일부 Unity 마무리 검증이 남아 있습니다.

## 프로젝트 개요

| 항목 | 내용 |
|:---|:---|
| 프로젝트 | FAIRINO FR5 Digital Twin |
| 개발 형태 | 개인 개발 프로젝트 |
| 개발 기간 | 약 6개월 |
| 주요 환경 | Ubuntu 24.04, ROS2 Jazzy, Gazebo 8, MoveIt2, Unity 6000.3.x |
| 주요 언어 | Python, C# |
| 대상 | FAIRINO FR5 협동로봇 |
| 핵심 범위 | 로봇 상태 동기화, 모션 플래닝, Workcell 시뮬레이션, Unity 디지털 트윈, SDK 연동 |

## 시스템 구성

```text
FAIRINO FR5 / FR5 SDK
          │
          ▼
         ROS2
          │
   ┌──────┴──────┐
   ▼             ▼
MoveIt2        Gazebo
Motion         Workcell / Physics
Planning          │
   └──────┬──────┘
          ▼
     Joint / Event
          │
          ▼
        Unity
 Digital Twin / UI / SMT Process
```

각 계층의 역할을 분리해 운영했습니다.

- **ROS2**: 상태·명령·이벤트 전달
- **MoveIt2**: Joint/Cartesian Motion Planning과 Trajectory 실행
- **Gazebo**: FR5와 Workcell 물리 시뮬레이션
- **Unity**: Joint State 반영, Workcell 시각화, UI, SMT 공정 표현
- **FR5 SDK**: 실제 FR5 상태와 명령을 연결하는 Robot Interface
- **Python**: ROS2 Node, MoveIt 실행, Gazebo 제어, 검증 도구
- **C#**: Unity Runtime Sync, ROS2 Bridge, Robot/Workcell Process

## 주요 구현

### ROS2 / Gazebo / MoveIt2

- FR5 Robot/Table/Magazine/Conveyor/Jig를 포함한 Gazebo Workcell 구성
- Gazebo 설비와 MoveIt Planning Scene Collision Object 정합
- Slot별 PREGRASP, PICK, Extract, Carry, Pre-Insert, Straight Insert, Retreat 구성
- Jig Attach/Detach 및 LIVE TF 기반 rigid follower 적용
- 모든 Trajectory Point에 대해 Negative J6 Branch 검증
- Slot01~07 Pick & Place One-Take 최종 검증
- Slot08은 최상단 간섭 위험으로 최종 운영 범위에서 제외

최종 Motion Master:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

### Unity Digital Twin

- ROS2 `/joint_states`를 Unity FR5 J1~J6 Joint Transform에 반영
- Robot/Table/설비의 Edit Mode 기준과 Runtime 상태 분리
- Source Magazine Slot01~07 / Slot08 EMPTY 구성
- Source Jig → Carried Jig → SMT Runtime Jig → Finish Jig의 Visual Ownership 관리
- Conveyor01 → Mounter → Inspection → Conveyor02 → Unloader → Finish Magazine 공정 구성
- External FR5 Input과 Unity 단독 공정 검증 경로 분리
- Source/Finish Magazine의 역할과 Runtime Handoff 분리

### FR5 SDK / Robot Interface

- Simulation Path와 Actual Robot Path 분리
- 실제 장비 연결 전 Read-only Feedback을 우선 검증
- Unity Manual/Test Mode와 External Feedback Mode 분리
- Unity UI → ROS2 Command Publisher → Ubuntu Listener 구조 구성
- Robot State/Command 경계를 분리해 시뮬레이션 테스트가 실제 Robot Command로 바로 연결되지 않도록 구성

## 주요 문제 해결

### Magazine Slot 높이에 따른 Pick 경로 간섭

Slot01의 Direct Pick을 기준으로 시작했지만 Slot이 높아질수록 Magazine Frame과 간섭 위험이 증가했습니다. Slot02 이상에는 PREGRASP → Short Cartesian Approach 구조를 적용하고, 이미 검증된 Slot01 동작은 별도로 유지했습니다.

### IK Wrist Branch 변경

높은 Slot에서 IK가 다른 Wrist Branch로 전환되는 문제가 있어 끝점 Joint만 확인하지 않고 전체 Trajectory Point에서 `J6 < 0`을 확인하는 검증을 추가했습니다.

### Gazebo / MoveIt / Unity의 역할 혼재

Gazebo는 물리·충돌·모션 검증, Unity는 상태 시각화·UI·공정 표현으로 역할을 구분했습니다. Unity 표현을 맞추기 위해 Robot Base나 Gazebo Motion 기준을 변경하지 않는 원칙을 유지했습니다.

### Jig Visual 중복 표시

Source, Robot Tool, Conveyor, Finish Magazine에 동일 Jig가 동시에 보이지 않도록 `PICK_DONE`, `PLACE_DONE`, Finish Handoff 기준으로 Visual Ownership을 전환하도록 구성했습니다.

## 검증 현황

| 영역 | 검증 내용 | 상태 |
|:---|:---|:---:|
| Gazebo / MoveIt2 | Slot01~Slot07 One-Take | PASS |
| Gazebo / MoveIt2 | Slot08 | 운영 제외 |
| Trajectory | 전체 Point Negative J6 | PASS |
| Planning Scene | 주요 Workcell Collision Object | PASS |
| Jig Follower | Tool-to-Jig Relative Pose 유지 | PASS |
| Unity | ROS2 Joint State → Joint Transform | 구현/검증 |
| Unity | Source Slot01~07 / Slot08 EMPTY | PASS |
| Unity | SMT Process / External FR5 Input | 구현/검증 |
| FR5 SDK | Read-only Feedback / Command Path 분리 | 구현 |
| Actual FR5 | 최종 End-to-End Motion / Feedback | 최종 검증 예정 |

ROS2/Gazebo 최종 Motion Master SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

## 기술 스택

| 구분 | 기술 |
|:---|:---|
| Robot | FAIRINO FR5 |
| Robotics | ROS2 Jazzy, MoveIt2, TF, JointState |
| Simulation | Gazebo 8 |
| Digital Twin | Unity 6000.3.x, C# |
| Robot Interface | FAIRINO FR5 SDK |
| Programming | Python, C# |
| Validation | Python, ROS2 CLI, Gazebo CLI, Unity Edit/Play Mode |
| Collaboration / Version | Git, GitHub |

## 상세 문서

| 문서 | 내용 |
|:---|:---|
| [문서 목차](docs/README.md) | 상세 문서 전체 목차 |
| [01. Overview](docs/01_overview.md) | 개발 배경, 목표, 구현 범위와 현재 상태 |
| [02. Architecture](docs/02_architecture.md) | ROS2·Gazebo·MoveIt2·Unity·SDK 연결 구조와 책임 경계 |
| [03. Features](docs/03_features.md) | 모션 자동화, Unity Digital Twin, SDK 연동 주요 기능 |
| [04. Data Flow](docs/04_data_flow.md) | Joint State, Command, Workcell Event와 Jig Ownership 흐름 |
| [05. Validation](docs/05_validation.md) | Motion, Planning Scene, Unity, SDK 검증 기준과 결과 |
| [06. Project Scope](docs/06_project_scope.md) | 문제 해결, 완료 범위, 제한과 남은 연동 |
| [07. Project Structure](docs/07_project_structure.md) | Ubuntu Python/ROS2와 Unity C# 스크립트 구조 및 역할 |

## 현재 남은 작업

- Unity SMT Jig Transfer Speed 최종 통일
- Finish Magazine Straight Insert 최종 Play Mode 확인
- Unity ↔ ROS2 ↔ 실제 FR5 End-to-End 연동 검증
- 실제 FR5 Motion/Feedback 최종 검증
- 대표 이미지와 검증 영상 추가

대표 이미지와 검증 영상은 최종 연동 완료 후 추가할 예정입니다.
