<a id="top"></a>

# 문서 목록

> 이 저장소는 **01~07 빠른 이해용 문서**, **08~16 구현·검증 상세 문서**로 구성합니다. README에서 프로젝트 전체를 본 뒤 관심 영역만 깊게 확인할 수 있도록 역할을 분리했습니다.

[프로젝트 README](../README.md) · [Architecture](02_architecture.md) · [Validation](05_validation.md)

## 추천 읽기 경로

| 독자 | 추천 순서 |
|:---|:---|
| 면접관 / 빠른 검토 | `README → 02 → 05 → 13` |
| ROS2 / Motion 중심 | `08 → 11 → 15 → 05` |
| Unity / Digital Twin 중심 | `09 → 16 → 04 → 05` |
| 실제 Robot Interface 중심 | `10 → 14 → 05` |
| Source 구조 확인 | `07 → 12` |

## 기본 문서

| 문서 | 목적 |
|:---|:---|
| [01. Overview](01_overview.md) | 프로젝트 배경, 목표, 핵심 결과 |
| [02. Architecture](02_architecture.md) | ROS2·Gazebo·MoveIt2·Unity·SDK 전체 구조 |
| [03. Features](03_features.md) | 기능별 구현/검증 Matrix |
| [04. Data Flow](04_data_flow.md) | JointState, Command, Jig, Process 데이터 흐름 |
| [05. Validation](05_validation.md) | Simulation / Unity / Integration / Hardware 검증 경계 |
| [06. Project Scope](06_project_scope.md) | 포함 범위, 제외 범위, 남은 통합 |
| [07. Project Structure](07_project_structure.md) | ROS2 Workspace와 Unity Source Tree |

## 상세 기술 문서

| 문서 | 목적 |
|:---|:---|
| [08. ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) | Workcell, Planning Scene, Motion Planning |
| [09. Unity Digital Twin](09_unity_digital_twin.md) | Runtime Sync, GUI, Workcell Process, Camera |
| [10. FR5 SDK Integration](10_fr5_sdk_integration.md) | Read-only Feedback, Command Path, Hardware 경계 |
| [11. Motion & Slot Validation](11_motion_and_slot_validation.md) | TAKE1~07, Slot, Cartesian, J6 검증 |
| [12. Script Reference](12_script_reference.md) | Python/C#/Launch/YAML/World 핵심 파일 |
| [13. Design Decisions & Issues](13_design_decisions_and_issues.md) | 문제, 원인, 선택, 결과 |
| [14. Deployment & Laptop Handoff](14_deployment_and_handoff.md) | Source of Truth, Runtime 복원, Hardware 전 단계 |
| [15. Laptop ROS2 Simulation Runtime](15_laptop_ros2_simulation_runtime.md) | fresh build, headless, RTF, Final TAKE 재검증 |
| [16. Unity Camera & Recording](16_unity_camera_and_recording.md) | 10-shot Camera, Follow, Recorder, UI Button Audit |

## 검증 상태 표기

| 상태 | 의미 |
|:---|:---|
| `PASS` | 해당 계층에서 실제 실행/검증 완료 |
| `IMPLEMENTED` | 구현은 완료했으나 상위 Runtime 검증이 남음 |
| `AUDITED` | Read-only/static structure and connection audit completed; Runtime source trace may remain |
| `PENDING` | 통합 또는 실제 실행 검증 전 |
| `OUT OF SCOPE` | 현재 운영 범위에서 의도적으로 제외 |

`Simulation PASS`, `Unity Live Integration PASS`, `Actual Robot PASS`는 서로 대체하지 않습니다.

## 미디어

- [images/README.md](images/README.md): 대표 이미지, GIF, 최종 영상 정리 기준
- Unity clean B-roll: Unity Recorder
- Gazebo / RViz / Unity 동시 화면: 외부 Screen Capture
- Actual FR5 영상: Hardware 검증 완료 후 추가

---

[↑ 맨 위로](#top) · [프로젝트 README](../README.md)
