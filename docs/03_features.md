<a id="top"></a>

# 03. 핵심 기능

## 구현 기능

| 영역 | 기능 | 책임 |
|---|---|---|
| ROS2 / Motion | TAKE1~TAKE7, Cartesian 구간, negative J6 guard | MoveIt2/Gazebo 실행 |
| Planning Scene | 주요 설비 collision 구성 | 로봇 주변 공간 모델 |
| Jig follower | LIVE Tool TF 기반 고정 상대변환 | Gazebo Jig pose 갱신 |
| Runtime Source | ROS2, C# JSON Bridge, Replay 선택 | 선택 source의 joint sample 전달 |
| Virtual Joint | 이름·단위·axis/sign mapping | 기존 Unity J1~J6 local rotation |
| Command / Status | 기존 ROS2 명령 JSON과 응답 | GUI와 controller 사이 계약 |
| Workcell | ExternalFr5 입력, dwell, SMT, Finish | 동일 Jig의 공정 소유권 |
| Camera | 수동 전환, Main 복귀, 시간 기반 sequence, Follow | 관찰 및 Game View |
| Recording | FHD 1080p30 Recording 구성 완료 | H.264 MP4 출력 설정 |
| Validation | Python MDH, C# FK, 좌표·축·오차 비교 | 독립 계산과 결과 기록 |

## Joint Feedback

`/joint_states`의 `joint1~joint6`를 이름으로 찾고 radian을 degree로 변환합니다.
선택된 source만 RuntimeSync에서 적용하며 기존 joint axis/sign을 재정의하지 않습니다.

## Command / Status

Simulation 명령 topic은 `/fr5/unity_command`, 응답은 `/fr5/command_status`입니다.
Backend는 MOVE_J, HOME, RESET, STOP과 네 Gripper 명령을 처리합니다.
STOP의 hold trajectory는 실제 장비의 비상정지와 다른 기능입니다.

## Workcell

- Source Slot01~07과 Finish `filledSlots[]`를 별도 관리.
- `TryAcceptFr5InsertedJig`로 동일 Jig를 명시적으로 인계.
- 3.0초 dwell 후 현재 위치에서 Conveyor01 출구 방향으로 이송.
- 공통 `0.15 m/s` 선속도; Mounter/Inspection 체류시간 보존.
- ExternalFr5 모드에서 자동 Jig 생성·자동 다음-cycle 시작 차단.
- Finish Lift world-Y 정렬과 무회전 수평 삽입.
- runtime Jig를 숨긴 뒤 해당 filledSlot만 표시.

## UI와 Camera

상태 Text의 주기 제한·값 cache·deadband로 잦은 재표시를 억제했습니다.
Camera Director/Follow는 로봇·SMT 명령을 호출하지 않고,
기존 RenderTexture UI와 별도 Game View 출력을 관리합니다.

## 수학 검증

Python canonical MDH와 C# FK를 독립 구현하고 position/rotation tolerance로 비교합니다.
transform order 및 frame 정의를 검증 항목으로 다루며, 서로 다른 계산 결과를 일치한다고 가정하지 않습니다.

[데이터 흐름](04_data_flow.md) · [검증](05_validation.md) · [핵심 코드](12_script_reference.md)

---

## 문서 목차

| 구분 | 바로가기 |
| --- | --- |
| **프로젝트** | [프로젝트 README](../README.md) · [전체 문서 인덱스](README.md) |
| **기본 문서** | [01 Overview](01_overview.md) · [02 Architecture](02_architecture.md) · [03 Features](03_features.md) · [04 Data Flow](04_data_flow.md) · [05 Validation](05_validation.md) · [06 Scope](06_project_scope.md) · [07 Structure](07_project_structure.md) |
| **상세 기술 문서** | [08 ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) · [09 Unity Digital Twin](09_unity_digital_twin.md) · [10 FR5 SDK](10_fr5_sdk_integration.md) · [11 Motion & Slot Validation](11_motion_and_slot_validation.md) · [12 Script Reference](12_script_reference.md) · [13 Design Decisions](13_design_decisions_and_issues.md) · [14 Deployment](14_deployment_and_handoff.md) · [15 Simulation Runtime](15_laptop_ros2_simulation_runtime.md) · [16 Camera & Recording](16_unity_camera_and_recording.md) |
| **수학 · 기구학 검증** | [Validation 문서](05_validation.md) · [Motion Validation](11_motion_and_slot_validation.md) · [MDH Parameters](../Python/Phase1_Kinematics/Python_MDH/fr5_mdh_params.py) · [Python FK Solver](../Python/Phase1_Kinematics/Python_MDH/fr5_fk_solver.py) · [Kinematics Tests](../Python/Phase1_Kinematics/Tests/) |
| **실제 FR5 · Cocktail Demo** | [Demo Hub](../demos/README.md) · [FR5 SDK Cocktail Robot Demo](../demos/01_fr5_sdk_cocktail_robot_demo/README.md) |
| **ROS2 Simulation** | [ROS2 / Gazebo / MoveIt2 문서](08_ros2_gazebo_moveit.md) · [fr5_ros2_ws Repository](https://github.com/seongyeop-dev/fr5_ros2_ws) |

[문서 목록으로 이동](README.md)
