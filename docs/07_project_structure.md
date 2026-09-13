<a id="top"></a>

# 07. 프로젝트 구조

## 공개 저장소

```text
FAIRINO_FR5_DigitalTwin/
├─ Assets/Project/Scripts/
│  ├─ Core/Runtime/
│  ├─ Core/Kinematics/
│  ├─ Core/Validation/
│  ├─ Robot/Control/
│  ├─ RuntimeSources/ROS2/
│  ├─ Workcell/
│  ├─ Camera/
│  └─ UI/
├─ Packages/
├─ ProjectSettings/
├─ Python/
│  ├─ requirements.txt
│  └─ Phase1_Kinematics/
│     ├─ Python_MDH/
│     ├─ Common/
│     ├─ GroundTruth/
│     └─ Tests/
├─ BridgeTools/FR5_CSharp_Bridge/
│  ├─ config/fr5_bridge_config.json
│  └─ FR5_CSharp_Bridge/
│     ├─ Program.cs
│     └─ FR5_CSharp_Bridge.csproj
├─ demos/
├─ docs/
└─ README.md
```

## 소스 묶음과 개발 Scene

이 저장소는 기존 tracked Unity Runtime과 Scene을 보존하고,
누락된 command status, Workcell, Camera Runtime 소스를 원본 .meta와 함께 수록합니다.
개발 프로젝트의 Scene·Prefab·UI 및 기존 Runtime 전체를 덮어쓰는 배포 방식은 사용하지 않습니다.

따라서 개발 Scene에서 연결한 Workcell Status binding, Camera 배열, 런타임 status 구독 구성은
클래스 파일이 존재한다는 사실과 구별합니다.
특히 이 저장소의 기존 Publisher/Router/StatusPanel은 보존된 버전이며,
새 StatusClient와 Process Controller의 모든 연결을 자동 설치하지 않습니다.
[Unity 문서](09_unity_digital_twin.md)는 개발 구성의 역할과 공개 소스 API를 함께 설명합니다.

## Python validation subset

`Python_MDH`는 MDH table와 FK solver, `Common`은 행렬/RPY 계산,
`GroundTruth`는 단일·case 입력과 JSON/CSV/TXT writer, `Tests`는 직접 연결된 unittest입니다.
`numpy` 외 프로젝트 내부 import는 이 subset에 포함됩니다.

기존 개발 폴더는 `Unity/FAIRINO_FR5_DigitalTwin/Assets`,
공개 저장소는 root의 `Assets`를 사용합니다.
single runner와 Bridge의 경로 탐색은 두 배치를 지원합니다.

## 외부 C# Bridge

Unity 밖에서 SDK read-only joint feedback 또는 mock sample을 생성하는 Console 프로그램입니다.
SDK DLL은 외부 build dependency로 지정하며 SDK example의 실행 파일이나 bin 폴더를 배포하지 않습니다.
실행·빌드 인자는 [SDK Interface](10_fr5_sdk_integration.md)에 정리했습니다.

## 별도 ROS2 저장소

[fr5_ros2_ws / feat/fr5-gazebo-jig-attach-detach](https://github.com/seongyeop-dev/fr5_ros2_ws/tree/feat/fr5-gazebo-jig-attach-detach)

```text
src/
├─ fr5_description/
├─ fr5_gazebo/
├─ fr5_moveit_config/
├─ fr5_ros2_bridge/
└─ ros_tcp_endpoint/
```

## 실제 SDK 제어 Demo

[demos/README.md](../demos/README.md)는 Cocktail Robot Demo의 source·문서·실제 시연 자료로 연결됩니다.
Demo 내부 파일은 Digital Twin source 동기화 대상과 분리합니다.

## 생성물 관리

Library, Temp, Logs, build output, Python cache/venv, validation output과 recording 파일을
핵심 source 묶음에 포함하지 않습니다.
JSON/CSV/TXT 결과는 runner로 생성하고 .cs와 .meta는 함께 관리합니다.

[Script Reference](12_script_reference.md) · [Deployment](14_deployment_and_handoff.md)

---

## 문서 목차

[프로젝트 README](../README.md) · [문서 목록](README.md) · [맨 위로](#top)

**기본 문서**
[01 Overview](01_overview.md) · [02 Architecture](02_architecture.md) · [03 Features](03_features.md) · [04 Data Flow](04_data_flow.md) · [05 Validation](05_validation.md) · [06 Scope](06_project_scope.md) · [07 Structure](07_project_structure.md)

**상세 기술 문서**
[08 ROS2/Gazebo/MoveIt2](08_ros2_gazebo_moveit.md) · [09 Unity](09_unity_digital_twin.md) · [10 FR5 SDK](10_fr5_sdk_integration.md) · [11 Motion](11_motion_and_slot_validation.md) · [12 Scripts](12_script_reference.md) · [13 Decisions](13_design_decisions_and_issues.md) · [14 Deployment](14_deployment_and_handoff.md) · [15 Simulation](15_laptop_ros2_simulation_runtime.md) · [16 Camera](16_unity_camera_and_recording.md)
