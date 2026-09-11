# 문서 목록

이 저장소의 문서는 공통 포트폴리오 구조와 FR5 프로젝트 전용 기술 문서를 함께 사용합니다. 01~07은 프로젝트 전체를 빠르게 파악하기 위한 기본 문서이고, 08 이후는 실제 구현과 검증을 더 깊게 확인할 때 사용하는 상세 문서입니다.

## 기본 문서

| 문서 | 내용 |
|:---|:---|
| [01. Overview](01_overview.md) | 개발 배경, 목표, 구현 범위와 현재 상태 |
| [02. Architecture](02_architecture.md) | ROS2·Gazebo·MoveIt2·Unity·SDK 연결 구조와 책임 경계 |
| [03. Features](03_features.md) | 모션 자동화, Unity Digital Twin, SDK 연동 주요 기능 |
| [04. Data Flow](04_data_flow.md) | Joint State, Command, Workcell Event와 Jig Ownership 흐름 |
| [05. Validation](05_validation.md) | Motion, Planning Scene, Unity, SDK 검증 기준과 결과 |
| [06. Project Scope](06_project_scope.md) | 문제 해결, 완료 범위, 제한과 남은 연동 |
| [07. Project Structure](07_project_structure.md) | Ubuntu Python/ROS2와 Unity C# 스크립트 구조 및 역할 |

## FR5 상세 기술 문서

| 문서 | 내용 |
|:---|:---|
| [08. ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) | Workcell, Planning Scene, Slot Motion, Jig Follower 상세 |
| [09. Unity Digital Twin](09_unity_digital_twin.md) | Joint Sync, Workcell Process, Jig Ownership, Source/Finish 구조 |
| [10. FR5 SDK Integration](10_fr5_sdk_integration.md) | Read-only Feedback, Command Path, Simulation/Actual 경계 |
| [11. Motion & Slot Validation](11_motion_and_slot_validation.md) | TAKE1~07, Negative J6, ACTION05, Motion Master 검증 |
| [12. Script Reference](12_script_reference.md) | Python/C#/Launch/YAML/World 주요 파일 역할과 연결 관계 |
| [13. Design Decisions & Issues](13_design_decisions_and_issues.md) | 주요 문제, 원인, 설계 변경과 선택 기준 |

## 미디어

- [images/README.md](images/README.md): 최종 대표 이미지와 검증 영상 정리 기준

---

[프로젝트 README](../README.md)
