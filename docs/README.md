<a id="top"></a>

# 문서 목록

## 기본 문서

| 문서 | 내용 |
|---|---|
| [01 프로젝트 개요](01_overview.md) | 배경과 핵심 결과 |
| [02 Architecture](02_architecture.md) | Simulation, Integration, Unity, 수학 검증 계층 |
| [03 Features](03_features.md) | 구현 기능과 책임 |
| [04 Data Flow](04_data_flow.md) | joint, command, Jig, Camera 흐름 |
| [05 Validation](05_validation.md) | 수학·시뮬레이션·Unity 검증 |
| [06 Project Scope](06_project_scope.md) | 프로젝트별 실행 환경과 설계 경계 |
| [07 Project Structure](07_project_structure.md) | 공개 소스와 별도 ROS2 저장소 |

## 상세 기술 문서

| 문서 | 내용 |
|---|---|
| [08 ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) | Workcell, planning, TF follower |
| [09 Unity Digital Twin](09_unity_digital_twin.md) | 핵심 Runtime Script 구조 |
| [10 FR5 SDK Interface](10_fr5_sdk_integration.md) | 실제 제어 경험과 read-only Bridge |
| [11 Motion & Slot](11_motion_and_slot_validation.md) | TAKE1~TAKE7과 trajectory 검사 |
| [12 Script Reference](12_script_reference.md) | 실제 코드 경로·역할·의존 관계 |
| [13 Design Decisions](13_design_decisions_and_issues.md) | 문제와 해결 |
| [14 Deployment](14_deployment_and_handoff.md) | Unity·Python·Bridge·ROS2 실행 환경 |
| [15 Laptop Runtime](15_laptop_ros2_simulation_runtime.md) | headless, RTF, 노트북 실행 결과 |
| [16 Camera & Recording](16_unity_camera_and_recording.md) | 시점 전환·관찰·녹화 설정 |

## 추천 읽기 경로

- 전체 구성: [프로젝트 README](../README.md) → [Architecture](02_architecture.md) → [Data Flow](04_data_flow.md).
- 로봇 시뮬레이션: [ROS2](08_ros2_gazebo_moveit.md) → [Motion](11_motion_and_slot_validation.md) → [Laptop Runtime](15_laptop_ros2_simulation_runtime.md).
- Unity 구현: [Unity](09_unity_digital_twin.md) → [Script Reference](12_script_reference.md) → [Camera](16_unity_camera_and_recording.md).
- 수학 검증: [Validation](05_validation.md) → [Script Reference](12_script_reference.md).
- 실제 제어 경험: [Cocktail Robot Demo](../demos/README.md) → [Digital Twin SDK Interface](10_fr5_sdk_integration.md).

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
