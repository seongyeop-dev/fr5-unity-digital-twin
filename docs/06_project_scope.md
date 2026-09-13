<a id="top"></a>

# 06. 프로젝트 범위

## 결과를 설명하는 기준

본 프로젝트는 FR5 motion 시뮬레이션, Unity Runtime/Interface,
독립 FK 비교, SMT 공정과 촬영 구성을 다룹니다.
실제 FR5 SDK 제어 경험은 [Cocktail Robot Demo](../demos/README.md)에 정리했습니다.

| 영역 | 포함 기능 |
|---|---|
| Simulation | ROS2 Jazzy, Gazebo Sim 8, MoveIt2, ros2_control |
| Motion | TAKE1~TAKE7, Planning Scene, negative J6, LIVE TF follower |
| Joint Interface | JointState와 Runtime Source, 기존 J1~J6 mapping |
| Workcell | 외부 Jig 입력, Source/Finish 구분, dwell, 공통 이송, Finish 삽입 |
| Mathematics | Python MDH, C# FK, 좌표·축·오차·허용치 |
| Presentation | 운영 UI, Camera Director/Follow, FHD 1080p30 Recording |
| SDK Interface | 별도 C# read-only feedback / mock Bridge |

## 설계 경계

- TAKE8은 UNUSED이며 Source Slot08은 EMPTY입니다.
- Source Magazine과 Finish Magazine을 합치지 않습니다.
- ROS2 JointState는 관절 feedback이지 SMT 시작 event가 아닙니다.
- Unity external API는 호출자가 전달한 Jig를 사용하며 자체 복제하지 않습니다.
- Camera는 관찰만 수행합니다.
- Simulation STOP hold와 실제 장비 비상정지는 같은 기능이 아닙니다.
- Python canonical MDH와 C# FK의 차이는 비교 대상이며 완전 정합 결과로 일반화하지 않습니다.

## 공개 소스 범위

Unity 핵심 누락 Runtime과 대응 .meta, Python validation subset,
최소 Bridge source/project/config를 제공합니다.
기존 tracked 파일·Scene과 [Cocktail Demo](../demos/README.md)는 보존합니다.
ROS2 source는 별도 저장소에 두고 이 저장소에 중복 수록하지 않습니다.

[Project Structure](07_project_structure.md) · [Validation](05_validation.md)

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
