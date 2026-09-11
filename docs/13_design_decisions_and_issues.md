# 13. Design Decisions & Issues

## 문서 목적

구현 과정에서 발생한 문제를 시간순 개발 일지로 나열하지 않고, 최종 구조에 영향을 준 문제와 선택 기준을 정리합니다.

## 1. Slot 높이에 따른 Direct Pick 간섭

### 문제

Slot01에서 사용한 Direct Pick 접근을 높은 Slot까지 동일하게 적용하면 Magazine Frame과의 여유가 줄어듭니다.

### 적용 기준

- Slot01의 이미 검증된 Motion은 유지
- Slot02 이상은 PREGRASP를 먼저 확보
- 최종 Pick 직전만 짧은 Cartesian Approach 사용

### 결과

상위 Slot에서 접근 경로를 분리하면서 Slot01의 확정 동작을 회귀시키지 않는 구조를 만들었습니다.

## 2. IK Wrist Branch 전환

### 문제

Motion 끝점의 Joint 값이 정상이어도 중간 Trajectory에서 Wrist Branch가 바뀌는 경우가 있었습니다.

### 적용 기준

끝점만 검사하지 않고 모든 Trajectory Point에서 J6를 확인합니다.

```text
J6 < 0 for every trajectory point
```

### 결과

Slot02~상위 Slot에서 Negative-J6 Seed Family와 Trajectory Guard를 함께 사용하게 됐습니다.

## 3. Gazebo와 MoveIt 좌표 기준 차이

### 문제

Gazebo의 Workcell 배치와 MoveIt Planning Scene의 충돌 모델이 같은 위치로 보이지 않는 구간이 있었습니다.

### 적용 기준

- Robot Base는 고정
- 설비 기준을 다시 임의 배치하지 않음
- Gazebo ↔ MoveIt 기준 차이는 Planning Scene 변환에서 보정

### 결과

Robot Pose를 움직여 충돌 모델을 맞추는 임시 보정 대신 Simulation과 Planning Scene의 책임을 분리했습니다.

## 4. Jig Carry 표현

### 문제

Pick 이후 Jig를 다음 위치로 직접 Set Pose하면 Robot Tool과 Jig의 상대관계가 사라지고, Carry 동작이 물리적으로 연결되지 않은 것처럼 보입니다.

### 적용 기준

Attach 시 Tool-to-Jig Relative Pose를 저장하고 LIVE TF 기반 rigid follower로 Carry합니다.

### 결과

Pick부터 Insert까지 Jig가 Robot Tool을 따라가는 관계를 일관되게 유지할 수 있게 됐습니다.

## 5. Gazebo와 Unity의 역할 중복

### 문제

Unity 화면을 맞추는 과정에서 Robot/설비 Pose까지 Unity 기준으로 다시 보정하면 Gazebo/MoveIt에서 검증한 Motion 기준이 흔들릴 수 있습니다.

### 적용 기준

- Gazebo: Physics / Collision / Robot Motion
- MoveIt2: Planning / Trajectory
- Unity: Visualization / UI / Process

### 결과

Unity 표현을 맞추기 위해 Robot Base, Joint, Collision Geometry를 수정하지 않는 기준을 확정했습니다.

## 6. Jig Visual 중복

### 문제

Source Magazine, Robot Tool, Conveyor, Finish Magazine에 같은 Jig가 동시에 보이는 문제가 있었습니다.

### 적용 기준

Jig를 Visual Ownership 상태로 분리합니다.

```text
Source → Carried → Runtime → Finish
```

### 결과

`PICK_DONE`, `PLACE_DONE`, Finish Handoff 이벤트를 기준으로 Visual을 전환해 한 시점에 한 Owner만 Jig를 표현하도록 했습니다.

## 7. Source와 Finish Magazine 역할 혼재

### 문제

생산 전 Jig 공급 Magazine과 공정 완료 Jig 저장 Magazine을 같은 상태로 취급하면 Slot Count와 Visual Ownership을 추적하기 어렵습니다.

### 적용 기준

- Source Magazine: Slot01~07 공급, Slot08 EMPTY
- Finish Magazine: 생산 완료 Jig 저장

### 결과

공급과 결과 저장을 분리해 공정 단계와 Jig 수량을 독립적으로 확인할 수 있게 됐습니다.

## 8. SMT Jig 이동 속도 불일치

### 문제

구간별 fixed duration과 SmoothStep을 사용하면서 이동 거리가 달라도 같은 시간에 이동하거나 순간 속도가 달라졌습니다. 특히 Inspection → Conveyor02에서 차이가 크게 보였습니다.

### 적용 기준

Process Dwell과 Translation을 분리하고 World-space 공통 선속도를 사용합니다.

```text
speed = 0.15 m/s
duration = distance / speed
```

### 결과

모든 정상 Jig Transfer가 거리 기준으로 같은 선속도를 사용하도록 코드와 Offline Contract를 정리했습니다. 최종 시각 판정은 Play Mode에서 확인합니다.

## 9. Finish Magazine 삽입 전 Rotation

### 문제

Finish Slot의 Rotation을 Jig 목표 자세로 사용하면서 Unloader에서 Magazine으로 들어가기 직전 Jig가 회전했습니다.

### 적용 기준

- Unloader 출력 Rotation 저장
- Height Alignment / Approach / Insert 동안 유지
- Slot Transform은 Position/Height 기준으로만 사용
- Handoff 시 Placeholder도 동일 Orientation 사용

### 검증

```text
Quaternion.Angle(startRotation, endRotation) <= 0.01°
```

### 결과

Finish 삽입은 수평 직선 Translation과 Rotation 유지로 분리했습니다. Static/Offline 검증은 완료했고 Play Mode 시각 검증을 남겨두고 있습니다.

## 10. Simulation PASS와 Actual Robot PASS 구분

### 문제

Gazebo/MoveIt에서 정상 동작하거나 Unity UI가 Command를 생성하는 것만으로 실제 FR5에서 안전하게 동작한다고 볼 수 없습니다.

### 적용 기준

```text
Simulation
→ Read-only Feedback
→ Command Path
→ Actual Robot Validation
```

### 결과

문서와 구현 모두에서 Simulation/Bridge 검증과 실제 Hardware 검증을 별도 상태로 관리합니다.

## 최종 설계 원칙

이 프로젝트에서 반복적으로 사용한 기준은 다음과 같습니다.

- 검증된 Pose/Take는 다시 임의 수정하지 않음
- Robot Base와 핵심 Geometry를 표현 문제 해결용으로 이동하지 않음
- Scene/Workcell 변경 전 READ-ONLY 상태 확인
- 한 계층이 다른 계층의 책임을 대신하지 않음
- Jig는 한 시점에 하나의 Owner만 가짐
- Simulation과 Actual Robot 검증 상태를 분리함
- 실패 경로보다 최종 PASS 기준을 Master에 남김

---

[문서 목차](README.md) · [06. Project Scope](06_project_scope.md) · [프로젝트 README](../README.md)
