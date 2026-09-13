<a id="top"></a>

# 11. Motion & Slot Validation

> Slot별 Motion을 하나씩 검증하고 PASS한 값만 Final Master에 누적해 회귀를 줄였습니다.

[문서 목차](README.md) · [프로젝트 README](../README.md) · [ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md)

## 최종 운영 범위

| TAKE | Slot | 상태 |
|:---:|:---:|:---:|
| TAKE1 | Slot01 | PASS |
| TAKE2 | Slot02 | PASS |
| TAKE3 | Slot03 | PASS |
| TAKE4 | Slot04 | PASS |
| TAKE5 | Slot05 | PASS |
| TAKE6 | Slot06 | PASS |
| TAKE7 | Slot07 | PASS |
| TAKE8 | Slot08 | OUT OF SCOPE |

## Master

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

SHA256:

```text
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

## TAKE Selector

| 값 | 실행 |
|:---|:---|
| `1`~`7` | 해당 TAKE |
| `ALL` | TAKE1→TAKE7 |
| TAKE8 | 사용하지 않음 |

`--execute`는 Simulation execution을 의미합니다.

## Motion Sequence

```mermaid
flowchart LR
    O["Open"] --> P["PREGRASP / PICK"]
    P --> C["Close"]
    C --> E["Extract"]
    E --> R["Carry"]
    R --> PI["Pre-Insert"]
    PI --> I["Insert"]
    I --> OP["Open"]
    OP --> RT["Retreat"]
    RT --> CV["Conveyor"]
```

## Slot01

- 초기 검증 기준 유지
- 확정 Pick Pose 재사용
- 이후 Slot 개발 때문에 재튜닝하지 않음

## Slot02 이상

- Magazine height 증가 고려
- PREGRASP
- 짧은 Cartesian Approach
- Negative-J6 seed family
- Final Pre-Insert / Straight Insert

## Negative-J6

```text
for every trajectory point:
    J6 < 0
```

End point가 아니라 모든 point를 검사합니다.

## Cartesian Validation

- Final Pick approach
- Extract
- Pre-Insert → Insert
- Release → Retreat

직선 구간은 endpoint뿐 아니라 실제 generated path를 확인합니다.

## Jig Follower

```text
T_tool_to_jig(initial)
≈
T_tool_to_jig(runtime)
```

Carry 중 relative pose가 유지되는지 확인합니다.

## ACTION 단계 기록 원칙

초기 운영 메모에는 ACTION05 범위를 Slot01~02로 제한한 표현이 있었지만, 최종 Laptop 실행 log에는 TAKE3~7 sequence에서도 ACTION05 단계가 관찰되었습니다.

따라서 최종 문서는 ACTION05를 Slot 범위로 단순화하지 않고 **Final Master와 실제 Final Runtime log를 기준**으로 설명합니다. 핵심 검증 대상은 ACTION label 자체보다 Pick/Extract/Carry/Insert/Retreat와 Negative-J6, follower, collision validity입니다.

## Laptop Final Revalidation

```text
Laptop HEAD
f02799cfd3126210ef72238990861c9c027c84af

Final Master SHA
80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be
```

Final result:

```text
FINAL ONE-TAKE TAKE1 -> TAKE7 PASS
FINAL_MASTER_RETURN_CODE=0
TAKE1_TO_TAKE7_FINAL_SIMULATION=PASS
```

Performance 문제를 이유로 Motion Pose를 다시 튜닝하지 않았습니다.

## 검증 절차

```mermaid
flowchart LR
    R["READ-ONLY"] --> P["Plan"]
    P --> G["Trajectory Guard"]
    G --> E["Execute"]
    E --> V["Visual / TF"]
    V --> L["Lock"]
```

실패 경로가 아니라 최종 PASS 기준을 Master에 남깁니다.

---

[↑ 맨 위로](#top) · [문서 목차](README.md) · [프로젝트 README](../README.md)
