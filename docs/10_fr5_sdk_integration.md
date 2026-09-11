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

Unity는 SDK 함수 자체를 직접 소유하기보다 ROS2 Interface를 통해 상태와 명령을 교환하는 구조를 사용합니다.

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

Unity Command 흐름은 다음과 같이 분리했습니다.

```text
Unity UI
  ↓
scr_FR5UICommandRouter
  ↓
scr_FR5Ros2CommandPublisher
  ↓
ROS2 Command Topic
  ↓
Ubuntu Listener / Robot Interface
  ↓
FR5 SDK
```

Command Status는 반대 방향으로 반환해 UI에서 요청 전송과 실제 처리 상태를 구분할 수 있도록 구성합니다.

프로젝트에서 사용한 Command/Status 계층에는 `/fr5/unity_command`, `/fr5/command_status`와 같은 ROS2 인터페이스가 포함됩니다.

## Simulation / Actual 분리

동일한 UI나 Motion 이름을 사용하더라도 실행 대상이 다릅니다.

| 모드 | 목적 | 실제 FR5 Motion |
|:---|:---|:---:|
| Unity Manual/Test | Joint/UI 동작 확인 | 없음 |
| Gazebo/MoveIt Execute | Simulation Motion 검증 | 없음 |
| SDK Read-only | 실제 Robot State 확인 | 없음 |
| Actual Command | 실제 FR5 제어 | 있음 |

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
