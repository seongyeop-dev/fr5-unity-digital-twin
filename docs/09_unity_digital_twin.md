# 09. Unity Digital Twin 상세

## Unity의 역할

Unity는 Robot Motion Planner나 물리 시뮬레이터를 대체하지 않습니다. ROS2/Gazebo에서 검증한 상태와 이벤트를 사용자가 확인할 수 있도록 표현하고, Workcell 공정과 UI를 구성하는 Digital Twin 계층으로 사용했습니다.

주요 범위는 다음과 같습니다.

- FR5 Joint Runtime Sync
- Robot / Table / Workcell 시각 구성
- Source / Finish Magazine 상태
- Jig Visual Ownership
- Conveyor / SMT Process Sequence
- External FR5 Input
- UI Command Routing

## FR5 Joint Runtime Sync

ROS2 `/joint_states`는 Unity에서 바로 Transform에 임의 적용하지 않고 역할을 분리했습니다.

```text
/joint_states
  ↓
scr_FR5Ros2JointStateClient
  ↓
scr_FR5RuntimeSyncManager
  ↓
scr_VirtualJointController
  ↓
FR5 J1~J6 Transform
```

`Manual/Test`와 `External Feedback`을 구분해 실제 Robot Feedback과 Unity 내부 테스트 입력이 동시에 Joint를 소유하지 않도록 구성했습니다.

## Edit Mode 기준과 Runtime 상태

Scene의 정적 배치와 Play Mode 동작을 분리해 관리합니다.

정적 기준:

- Robot / Table Transform
- Magazine / Conveyor / SMT 설비 배치
- Material / Prefab 기준
- Source / Finish Slot 기준

Runtime 상태:

- Joint Feedback
- Jig Ownership
- Conveyor 이동
- Equipment Process
- UI 상태

No-Save Editor Validation을 사용해 Scene을 저장하지 않고도 Source/Finish 상태와 보호 대상을 확인할 수 있도록 했습니다.

## Source Magazine

최종 Source Magazine 정책은 다음과 같습니다.

```text
Slot01~07 = Jig 존재
Slot08    = EMPTY
```

Slot08은 ROS2/Gazebo 최종 운영 범위에서도 사용하지 않기 때문에 Unity에서도 공급 대상으로 만들지 않습니다.

Source Magazine에서 Jig가 Pick되면 단순히 같은 Visual을 복제해 여러 곳에 표시하지 않고 Ownership을 전환합니다.

## Jig Visual Ownership

한 개의 생산 Jig가 여러 위치에 동시에 나타나는 문제를 막기 위해 상태를 구분했습니다.

```text
Source Slot Visual
      ↓ PICK_DONE
Carried Jig
      ↓ PLACE_DONE
Runtime SMT Jig
      ↓ Finish Handoff
Finish Slot Visual
```

각 상태는 이전 Visual을 비활성화하거나 소유권을 넘긴 뒤 다음 Visual을 활성화하는 방식으로 전환됩니다.

## SMT Process

Unity 공정 흐름은 다음 순서로 구성했습니다.

```text
FR5 Place
  ↓
EQ_Conveyor_01
  ↓
Mounter
  ↓
Inspection
  ↓
EQ_Conveyor_02
  ↓
Unloader
  ↓
Finish Magazine
```

공정 대기시간과 Jig의 Translation은 분리합니다. Mounter/Inspection dwell과 이동 속도를 같은 duration 값으로 처리하지 않습니다.

### 공통 Jig Transfer Speed

마지막 Unity 보정에서는 구간별 fixed duration과 SmoothStep에 의해 실제 선속도가 달라지는 문제를 정리했습니다.

```text
common linear speed = 0.15 m/s
duration = world_distance / speed
```

정상 Jig Translation 구간은 동일한 World-space 선속도 기준을 사용하며, Process Dwell과 Lift Timing은 유지합니다. 이 변경은 Static Compile과 Offline Contract까지 완료했고 최종 Play Mode 시각 검증을 남겨두고 있습니다.

## Finish Magazine Straight Insert

Finish Magazine에 들어가기 직전 Jig가 Slot Rotation으로 회전하는 현상을 제거하기 위해 Unloader 출력 시점의 World Rotation을 기준으로 유지합니다.

```text
Unloader End
→ insertionRotation 저장
→ Slot Height Alignment
→ Horizontal Approach
→ Straight Insert
→ Runtime Jig Hide
→ Filled Slot Handoff
```

Finish Slot Transform은 위치/높이 기준으로 사용하고, Jig Trajectory의 Rotation Target으로 사용하지 않습니다.

Rotation 검증 기준은 다음과 같습니다.

```text
Quaternion.Angle(
    insertionStartRotation,
    insertionEndRotation
) <= 0.01°
```

## Source / Finish Editor Setup

`FR5SourceFinishSetup.cs`는 Source 7 + Slot08 EMPTY와 Finish Placeholder 상태를 No-Save 방식으로 구성/검증합니다. 보호 Transform이 변경되면 중단/Undo하도록 구성해 Robot/Table/설비 기준을 같이 변경하지 않도록 했습니다.

현재 Finish Placeholder 방향 보정은 코드에 반영되어 있으나, 실제 메뉴 적용과 Play Mode 결과는 사용자가 최종 확인한 뒤 Scene 기준으로 확정합니다.

## External FR5 Input

Unity 단독 Offline Process와 외부 FR5 입력을 분리합니다. External Mode에서는 기존 Unity Legacy Owner가 같은 Jig를 동시에 움직이지 않도록 비활성화하고, 외부 Place/Release 이벤트 이후 SMT Process가 이어지도록 구성했습니다.

## 현재 검증 경계

완료:

- Source Slot01~07 / Slot08 EMPTY 구조
- Source / Carried / Runtime / Finish Ownership
- External FR5 Input 구조
- 공통 0.15 m/s Transfer 코드/정적 검증
- Finish Rotation Drift 코드/정적 검증

최종 확인 예정:

- Play Mode에서 모든 SMT 구간의 시각적 동일 속도
- Unloader → Finish 무회전 직선 삽입
- Runtime Jig → Filled Slot Handoff 순간 Visual Snap 여부
- Unity ↔ ROS2 ↔ 실제 FR5 종단 연동

---

[문서 목차](README.md) · [05. Validation](05_validation.md) · [프로젝트 README](../README.md)
