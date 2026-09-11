# 05. 검증 결과

## 검증 원칙

기능을 한 번에 전체 수정하기보다 현재 상태를 확인한 뒤 한 부분만 수정하고 Static/Runtime/Visual 검증을 거쳐 기준을 고정하는 방식으로 작업했습니다.

```text
Inspect
→ Modify
→ Static Check
→ Runtime / Simulation
→ Visual Check
→ Lock
```

## ROS2 / Gazebo / MoveIt2

| 항목 | 확인 내용 | 결과 |
|:---|:---|:---:|
| ROS2 State | `/joint_states` 확인 | PASS |
| Planning Scene | 주요 Workcell Collision Object | PASS |
| Cartesian Path | 주요 Pick/Insert 구간 Full Path | PASS |
| Jig Follower | Tool-to-Jig Relative Pose 유지 | PASS |
| Negative J6 | 모든 Trajectory Point `J6 < 0` | PASS |
| TAKE1 | Slot01 One-Take | PASS |
| TAKE2 | Slot02 One-Take | PASS |
| TAKE3 | Slot03 One-Take | PASS |
| TAKE4 | Slot04 One-Take | PASS |
| TAKE5 | Slot05 One-Take | PASS |
| TAKE6 | Slot06 One-Take | PASS |
| TAKE7 | Slot07 One-Take | PASS |
| TAKE8 | Slot08 | 운영 제외 |

최종 Master:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

## Slot / Motion 검증

- Slot01 검증값 유지
- Slot02 이상 PREGRASP 적용
- Final Pre-Insert / Insert Position 확인
- Extract / Retreat 직선 이동 확인
- Slot03~07 ACTION05 미사용
- Slot01~02 ACTION05 사용
- TAKE1~07 최종 재실행 후 Visual PASS 확인

## Source 재현성과 노트북 이관 기준

아래는 개발 PC Ubuntu의 최종 확인 결과입니다. 노트북 restore나 실제 FR5 Hardware PASS를 뜻하지 않습니다.

| 항목 | 확인 내용 | 결과 |
|:---|:---|:---:|
| Git parity | Local / origin의 `46cf3ace69154e8befb2fb3a78686cd931c3428a` 일치, Ahead / Behind `0 / 0` | PASS |
| Motion Master | 최종 SHA256 일치 | PASS |
| Slot YAML | 최종 SHA256 parity | PASS |
| Workcell World | 최종 SHA256 parity | PASS |
| Workcell Launch | 최종 SHA256 parity | PASS |
| Listener Source | `fr5_unity_command_listener.py` tracked Source 확인 | PASS |
| Runtime 경로 | 조사 범위에서 절대 user 경로 의존성 미발견 | 확인 |
| Source 의존성 | 조사 범위에서 Source symlink 의존성 미발견 | 확인 |
| 노트북 restore | Source 복원, 의존성, fresh build, read-only preflight | Pending |
| Actual FR5 | Hardware feedback / command / 안전 중단 / 종단 검증 | Pending |

고정 branch, 파일별 경로와 SHA256은 [14. Deployment & Laptop Handoff](14_deployment_and_handoff.md)의 기준표를 사용합니다. 개발 PC의 `build/`, `install/`, `log/`는 노트북에 복사하지 않습니다.

## Backend Command / TAKE 검증 경계

| 항목 | 현재 확인 범위 | 상태 |
|:---|:---|:---:|
| 기존 Command Bridge | MOVE_J / HOME / RESET / STOP 및 Gripper 명령 처리 | 구현 확인 |
| Backend Status | `/fr5/command_status`, `std_msgs/msg/String` JSON Publisher | 구현 확인 |
| Master selector | `FR5_TAKE=1`~`7`, `ALL`은 TAKE1→TAKE7 순차 실행 | 코드 확인 |
| Unity → TAKE | Listener의 `RUN_TAKE` 및 Unity 요청 연결 | 미구현 |
| TAKE lifecycle | request/status correlation, BUSY, active Master STOP | Pending |
| Unity Status 수신 | Status Subscriber와 TAKE 완료 반영 | Pending |

Backend Status Publisher의 존재와 Unity의 상태 수신 완료는 별도로 판정합니다. Master selector 확인은 Simulation 기준이며 실제 FR5 실행 검증을 대신하지 않습니다.

## Unity 검증

확인한 항목:

- ROS2 Joint State Runtime Sync
- Source Slot01~07 유지
- Slot08 EMPTY
- Legacy Visual 비활성화
- External FR5 Input Mode
- Source → Carried → Runtime → Finish Ownership 전환
- SMT Process Sequence
- Finish Handoff 구조
- Edit Mode 기준 보호
- Scene SHA 변경 여부

C# 수정 후 가능한 범위에서 다음 검증을 함께 사용했습니다.

- C# Static Compile / Contract Check
- Offline Contract Validation
- `git diff --check`
- Scene SHA 작업 전/후 비교

## SDK / Actual Robot 검증 경계

SDK 구조와 Read-only Feedback/Command Path는 구성했지만, 실제 Robot Motion 검증은 Simulation PASS와 분리해 기록합니다.

최종 실제 장비 검증 예정 항목:

- Unity → ROS2 → FR5 Command Full Path
- Actual FR5 Motion과 Unity Joint Feedback 동기화
- Magazine Motion 실제 환경 Calibration
- 실제 Robot Speed / Safety 확인

## 현재 Unity 최종 검증

Unity 공정의 마지막 두 항목은 코드 수정과 정적 검증까지 완료했습니다.

- SMT Jig Transfer: 기존 Conveyor 기준 `0.15 m/s` 공통 World-space 선속도 적용
- 이동 시간: `duration = world_distance / speed` 기준으로 계산
- Finish Insert: Unloader 도착 시점의 Jig World Rotation 유지
- Rotation 검증: 삽입 시작/완료 `Quaternion.Angle` 기준 drift `<= 0.01°`
- Static C# Compile: `CSC_EXIT_CODE=0`
- Offline Contract: 24,241 assertions PASS
- Scene SHA256 전/후 동일: `E9D9C2F818BD7A8244AA80DC261BF659ACB507720DA9BAF272A54CBA03130ACE`

Scene과 Play Mode는 자동으로 변경하지 않았으며, 최종 상태 표기는 사용자의 Unity Play Mode 시각 검증 후 확정합니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
