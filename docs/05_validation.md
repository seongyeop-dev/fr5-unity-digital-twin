# 05. 검증 결과

## 검증 원칙

각 PASS는 검증 범위를 함께 표시합니다.

Unity 화면에서 움직였다는 사실만으로 실제 FR5 제어가 검증됐다고 판단하지 않습니다.

## 결과 요약

| 영역 | 검증 | 결과 |
|---|---|---|
| Gazebo Gripper | Open / Close Command | PASS |
| ROS2 → Unity | `/joint_states` 수신 및 Joint 매핑 | PASS |
| Unity → ROS2 | Command Publish / Listener | PASS |
| ROS2 → Gazebo | Arm Controller 실행 | PASS |
| MoveIt2 | Plan / Execute → Gazebo | PASS |
| Python | MDH Ground Truth | PASS |
| C# Bridge | Read-only / Mock Feedback | PASS |
| ROS2 Slot | TAKE1~TAKE7 One-Take | PASS |
| Unity Source | Slot01~07 / Slot08 EMPTY | PASS |
| Unity External Input | Edit Mode 준비 | PASS |
| Unity Finish | 최종 이동 동작 | 개선 중 |

## ROS2 Self Check 이력

과거 통합 검증 결과:

```text
PASS 39
WARN 0
FAIL 0
```

## Slot One-Take

최종 Master SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

```text
TAKE1 ~ TAKE7 : 최종 PASS
TAKE8         : 미사용
```

## Unity Source / Finish 정적 검증

2026-09-11 기준:

```text
C# static compile:
CSC_EXIT_CODE=0

Offline contract:
1810 PASS

git diff --check:
PASS
```

코드 작업 단계에서는 Scene을 저장하지 않았습니다.

## Unity Edit Mode

Source / Finish Configure:

```text
Source 7                    : PASS
Slot08 EMPTY                : PASS
Protected Transform 유지    : PASS
UI / Camera 유지            : PASS
Scene Save                  : 없음
```

External FR5 Input Prepare:

```text
ExternalFr5 mode            : 준비 완료
Legacy owners               : inactive
Protected SMT transforms    : unchanged
Scene Save                  : 없음
```

## 현재 개선 중인 항목

Play Mode 육안 확인에서 다음 두 항목을 발견했습니다.

```text
Inspection → Conveyor02 이동 속도 과다
Finish Magazine 삽입 직전 Jig 회전
```

최종 PASS 전에 두 항목을 다시 검증할 예정입니다.
