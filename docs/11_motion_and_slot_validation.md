# 11. Motion & Slot Validation

## 최종 운영 범위

Magazine Motion은 TAKE 단위로 검증했습니다. 최종 운영 범위는 TAKE1~TAKE7이며 TAKE8은 사용하지 않습니다.

| TAKE | Slot | 최종 상태 |
|:---:|:---:|:---:|
| TAKE1 | Slot01 | PASS |
| TAKE2 | Slot02 | PASS |
| TAKE3 | Slot03 | PASS |
| TAKE4 | Slot04 | PASS |
| TAKE5 | Slot05 | PASS |
| TAKE6 | Slot06 | PASS |
| TAKE7 | Slot07 | PASS |
| TAKE8 | Slot08 | 운영 제외 |

TAKE1~TAKE7은 최종 Simulation Master 기준으로 재실행 후 Gazebo Visual PASS까지 확인한 상태입니다. 실제 FR5 Hardware 실행 결과와는 구분합니다.

## Master Motion 기준

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

최종 SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

새 Take를 만들 때 별도 Runner를 추가하지 않고 같은 Master 파일 안에서 누적 수정했습니다. 이미 PASS한 Take의 Pick/IK/Action/Approval/Corridor는 이후 Take 작업에서 다시 바꾸지 않는 방식으로 기준을 잠갔습니다.

## Master TAKE Selector

Master 코드에서 확인된 입력은 환경변수 `FR5_TAKE`입니다.

| 값 | Simulation 실행 범위 |
|:---|:---|
| `1`, `2`, `3`, `4`, `5`, `6`, `7` | 해당 TAKE 단독 선택 |
| `ALL` | TAKE1 → TAKE7 순차 실행 |

TAKE8은 운영 범위에서 사용하지 않습니다. 아래는 Simulation Master 실행 예이며 Hardware 실행 명령이나 배포 시 자동 실행 절차가 아닙니다.

```bash
FR5_TAKE=1 python3 -u \
  src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py \
  --execute
```

`--execute`는 Simulation 실행 구분입니다. Master의 selector 및 `ALL` 지원은 확인됐지만, 현재 Listener에는 `RUN_TAKE`가 없으므로 Unity Slot/OneTakeAll 요청과 Master 실행은 아직 연결되지 않았습니다. Unity-to-TAKE dispatch와 완료 상태 처리는 별도 통합 항목으로 남깁니다.

Slot YAML / World / Launch의 보호 SHA와 노트북 인계 기준은 [14. Deployment & Laptop Handoff](14_deployment_and_handoff.md)에 모았습니다.

## Slot01

Slot01은 초기 기준 Pose를 직접 검증한 Slot입니다.

운영 특징:

- 기존 Direct Pick 유지
- 확정된 Pick Pose 재사용
- Extract / Carry / Place / Retreat 경로 잠금
- ACTION05 사용

Slot01을 상위 Slot 보정 때문에 다시 변경하지 않는 것이 전체 Motion 회귀를 줄이는 데 중요했습니다.

## Slot02

Slot02부터는 Magazine 높이 증가를 고려해 PREGRASP를 사용합니다. Slot01 대비 Pick 높이 Offset과 World-Y Orientation 보정을 포함하고, 짧은 직선 접근 후 Grasp하도록 구성했습니다.

최종 Runtime Override에는 PREGRASP Backoff와 Orientation Adjustment가 포함됩니다.

## Slot03~07

Slot03 이상은 검증된 Negative-J6 Family를 사용하고 ACTION05를 사용하지 않습니다.

```text
Slot03~07
PREGRASP
→ Cartesian Approach
→ Pick
→ Extract
→ Carry
→ Pre-Insert
→ Straight Insert
→ Release
→ Retreat
```

## Negative J6 Trajectory 검증

끝점 Joint만 검사하면 중간 Path에서 다른 Wrist Branch로 넘어갈 수 있기 때문에 모든 Trajectory Point를 확인합니다.

검증 조건:

```text
for every trajectory point:
    J6 < 0
```

이 조건은 Motion Planning 결과를 실제 실행하기 전에 확인하는 Guard로 사용했습니다.

## Cartesian 구간

주요 직선 구간은 End Pose만 맞는지보다 실제 Path가 원하는 방향으로 생성되는지 확인합니다.

- Final Pick 접근
- Extract
- Final Pre-Insert → Straight Insert
- Release 후 Retreat

특히 Jig와 Magazine이 가까운 구간에서는 Joint-space 우회보다 Cartesian 방향을 명시적으로 유지했습니다.

## Jig Follower 검증

Attach 이후 Tool-to-Jig Relative Pose가 유지되는지 확인합니다.

```text
T_tool_to_jig(initial)
≈
T_tool_to_jig(runtime)
```

Carry 중 Jig가 Tool에서 이탈하거나 World에 고정된 것처럼 보이는 현상이 없는지 Gazebo와 TF 상태를 함께 확인했습니다.

## ACTION05 정책

| 범위 | 정책 |
|:---|:---|
| Slot01~02 | ACTION05 허용 |
| Slot03~07 | ACTION05 금지 |
| Slot08 | 사용 안 함 |

ACTION05를 모든 Slot의 공통 필수 단계로 만들지 않고, 실제 검증된 Take별 경로를 유지했습니다.

## Planning Scene / Environment 확인

Motion 검증 전에 다음 상태가 함께 맞아야 합니다.

- `/joint_states` 수신
- FR5 Base 고정
- Robot Table / Magazine / Conveyor Collision Object
- Gazebo와 MoveIt 기준 변환
- Jig Model 상태
- Slot별 대상 Jig/Label

Robot Motion 문제를 설비 표시나 RViz Visual 문제와 혼동하지 않도록 Runtime State와 Planning Scene을 분리해 확인했습니다.

## 검증 방식

```text
READ-ONLY 상태 확인
→ Plan
→ Trajectory Guard
→ Execute
→ Gazebo Visual 확인
→ PASS 기준 잠금
```

Take별 실패 경로를 다음 Take의 기본값으로 재사용하지 않고, 최종 PASS한 값만 Master에 남겼습니다.

---

[문서 목차](README.md) · [08. ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) · [05. Validation](05_validation.md)
