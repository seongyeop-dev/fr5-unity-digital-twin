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

[프로젝트 README](../README.md) · [문서 목록](README.md) · [맨 위로](#top)

**기본 문서**
[01 Overview](01_overview.md) · [02 Architecture](02_architecture.md) · [03 Features](03_features.md) · [04 Data Flow](04_data_flow.md) · [05 Validation](05_validation.md) · [06 Scope](06_project_scope.md) · [07 Structure](07_project_structure.md)

**상세 기술 문서**
[08 ROS2/Gazebo/MoveIt2](08_ros2_gazebo_moveit.md) · [09 Unity](09_unity_digital_twin.md) · [10 FR5 SDK](10_fr5_sdk_integration.md) · [11 Motion](11_motion_and_slot_validation.md) · [12 Scripts](12_script_reference.md) · [13 Decisions](13_design_decisions_and_issues.md) · [14 Deployment](14_deployment_and_handoff.md) · [15 Simulation](15_laptop_ros2_simulation_runtime.md) · [16 Camera](16_unity_camera_and_recording.md)
