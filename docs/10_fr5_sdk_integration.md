# 10. FR5 SDK Integration

## 목적

FR5 SDK 연동은 Unity에서 실제 Robot에 바로 명령을 보내는 구조로 만들지 않았습니다. Simulation, Read-only Feedback, Actual Command를 분리해 각 단계의 검증 범위를 명확하게 유지하는 것을 우선했습니다.

## 계층 구조

```text
FAIRINO FR5
   ↕
FR5 SDK
   ↕
Ubuntu Robot Interface / ROS2
   ↕
ROS2 Topics / Status
   ↕
Unity
```

위 그림은 실제 장비 연동의 계층 구조입니다. 현재 확인된 Gazebo Command Backend와 실제 FR5 SDK 실행 검증을 구분하며, 노트북 Hardware 종단 검증은 Pending입니다.

## Read-only Feedback 우선

실제 FR5 연결 시 가장 먼저 확인하는 경로는 Motion Command가 아니라 Robot State입니다.

확인 범위:

- Robot 연결 상태
- Joint Position / Joint State
- SDK Feedback 수신
- ROS2 Publish
- Unity Runtime Joint 반영

Read-only 경로가 확인되기 전에는 Unity 테스트 버튼이나 Simulation PASS를 실제 Robot Command 허용으로 해석하지 않습니다.

## Command Path

구현이 확인된 기존 명령의 Simulation 경로는 다음과 같습니다.

```text
Unity UI
  ↓
scr_FR5UICommandRouter
  ↓
scr_FR5Ros2CommandPublisher
  ↓
/fr5/unity_command [std_msgs/msg/String, JSON]
  ↓
fr5_unity_command_listener
  ↓
Gazebo / controller
```

실제 Listener는 `src/fr5_ros2_bridge/fr5_ros2_bridge/fr5_unity_command_listener.py`입니다. 기존 MOVE_J / HOME / RESET / STOP 및 Gripper 명령을 dispatch하고, `/fr5/command_status`에 `std_msgs/msg/String` JSON을 publish합니다. Status Publisher는 미래 기능이 아니라 현재 Backend에 존재합니다.

반면 Unity Status Subscriber와 TAKE completion correlation은 아직 완료되지 않았습니다. 기존 schema는 `source`, `robot`, `command`, `accepted`, `executed`, `state`, `message`, `timestamp_unix_ms`를 사용하며 TAKE 전용 `request_id` / `fr5_take`는 없습니다. 전체 JSON과 Listener SHA는 [14. Deployment & Laptop Handoff](14_deployment_and_handoff.md)에 정리했습니다.

### TAKE와 STOP 경계

- Simulation Master: `FR5_TAKE` 1~7 / ALL 및 `--execute` 지원 확인
- Backend Listener: `RUN_TAKE` 미구현; Unity Slot → Master dispatch는 Pending
- 기존 `STOP`: hold trajectory 처리
- active Master STOP / TAKE BUSY / request-status correlation: 별도 구현·검증 필요
- 실제 FR5 안전 중단 및 Hardware command enable: 노트북에서 별도 검증

기존 STOP 처리를 Master 중단이나 Hardware Emergency Stop의 검증 완료로 해석하지 않습니다. Unity의 로컬 SENT/READY 표시도 Backend TAKE 완료 응답을 대신하지 않습니다.

## Simulation / Actual 분리

동일한 UI나 Motion 이름을 사용하더라도 실행 대상이 다릅니다.

| 모드 | 목적 | 실제 FR5 Motion |
|:---|:---|:---:|
| Unity Manual/Test | Joint/UI 동작 확인 | 없음 |
| Gazebo/MoveIt Execute | Simulation Motion 검증 | 없음 |
| SDK Read-only | 실제 Robot State 확인 | 없음 |
| Actual Command | 실제 FR5 제어 대상 경로 | Hardware 검증 Pending |

따라서 `--execute`, Unity Play Mode, SDK 연결 성공은 서로 다른 검증 단계입니다.

## 안전 경계

실제 Robot Path에서 중요하게 본 기준은 다음과 같습니다.

- Simulation PASS와 Hardware PASS 분리
- Read-only Feedback 먼저 확인
- Command와 Status Topic 분리
- Actual Command 허용 전 Robot 상태 확인
- Simulation/Test Mode가 Actual Command를 자동 호출하지 않도록 분리
- Robot Base / Geometry / Joint 구조를 Unity 표현 때문에 변경하지 않음

## Unity Runtime Mode

Unity에서는 Robot Pose를 누가 소유하는지 명확히 합니다.

```text
Manual/Test
또는
External Feedback
```

두 입력이 동시에 같은 Joint Transform을 갱신하지 않도록 Runtime Sync Manager에서 상태를 관리합니다.

## 최종 실제 장비 검증 항목

아래 항목은 Simulation/Bridge 구현과 분리해 실제 장비에서 최종 확인해야 합니다.

개발 PC Ubuntu의 역할은 종료하며, 노트북에서는 [고정 commit/SHA와 fresh build 기준](14_deployment_and_handoff.md)을 먼저 확인합니다. read-only FR5 feedback → Unity network → STOP path 검증을 거친 뒤에만 command를 enable합니다.

- FR5 SDK Joint Feedback 지속 수신
- ROS2 Joint State와 Actual Joint 일치
- Unity FR5 Joint Transform 실시간 동기화
- Unity Command → ROS2 → SDK Full Path
- Robot Speed / Safety Parameter
- 실제 Magazine/Workcell Coordinate Calibration
- Emergency Stop / Command Abort 동작 범위

이 문서에서는 구현된 Interface와 검증 경계를 설명하며, 실제 FR5 최종 Hardware PASS는 별도 완료 전까지 확정 상태로 표현하지 않습니다.

---

[문서 목차](README.md) · [02. Architecture](02_architecture.md) · [프로젝트 README](../README.md)
