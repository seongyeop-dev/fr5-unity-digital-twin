<a id="top"></a>

# 13. Design Decisions & Issues

> 개발 일지가 아니라 **문제 → 원인 → 결정 → 결과**로 최종 설계에 영향을 준 판단만 정리합니다.

[문서 목차](README.md) · [프로젝트 README](../README.md)

## Decision Summary

| # | 문제 | 결정 |
|:---:|:---|:---|
| 1 | 높은 Slot Direct Pick 간섭 | PREGRASP 분리 |
| 2 | Wrist Branch 변경 | 전체 Point J6 Guard |
| 3 | Gazebo / MoveIt 기준 차이 | Planning Scene 변환 |
| 4 | Jig Carry 불연속 | LIVE TF follower |
| 5 | Gazebo / Unity 역할 중복 | Physics vs Visualization 분리 |
| 6 | Jig 중복 표시 | Ownership State |
| 7 | Source / Finish 혼재 | Magazine 역할 분리 |
| 8 | SMT 속도 불일치 | 공통 World-space speed |
| 9 | Finish 직전 회전 | Rotation 유지 Straight Insert |
| 10 | Simulation / Hardware 상태 혼재 | Validation 단계 분리 |
| 11 | 개발 PC / Laptop 환경 차이 | Git + fresh build + SHA |
| 12 | Laptop RTF 저하 | true headless |
| 13 | Camera 다중 출력 | Camera Director |
| 14 | 촬영 자료 단조로움 | 10-shot + Recorder |
| 15 | UI 연결 신뢰성 | Button read-only audit |

## 1. Slot 높이에 따른 Direct Pick 간섭

**문제**: Slot이 높아질수록 Magazine Frame 여유가 감소했습니다.

**결정**: Slot01의 검증 동작을 유지하고 Slot02 이상은 PREGRASP → short Cartesian Approach를 사용했습니다.

**결과**: 상위 Slot 안전 여유를 확보하면서 Slot01 regression을 막았습니다.

## 2. IK Wrist Branch 전환

**문제**: End joint는 정상이어도 중간 trajectory에서 Wrist branch가 바뀔 수 있었습니다.

**결정**:

```text
J6 < 0 for every trajectory point
```

**결과**: Seed family와 trajectory guard를 함께 사용했습니다.

## 3. Gazebo / MoveIt 기준 차이

**문제**: Physics World와 Collision Model이 같은 위치로 보이지 않는 구간이 있었습니다.

**결정**: Robot Base는 고정하고 Planning Scene 변환에서 보정했습니다.

**결과**: 표현 문제 때문에 Robot pose를 이동하는 임시 해법을 피했습니다.

## 4. Jig Carry 표현

**문제**: Set Pose 방식은 Tool-Jig 관계를 잃습니다.

**결정**: Tool-to-Jig relative pose + LIVE TF follower.

**결과**: Pick→Insert 동안 Jig가 Tool을 일관되게 따라갑니다.

## 5. Gazebo / Unity 역할 중복

**결정**:

| 계층 | 책임 |
|:---|:---|
| Gazebo | Physics / Controller |
| MoveIt2 | Planning / Trajectory |
| Unity | Visualization / GUI / Process |

**결과**: Unity 화면을 맞추기 위해 검증된 Simulation 기준을 바꾸지 않습니다.

## 6. Jig Visual 중복

**결정**:

```text
Source → Carried → Runtime → Finish
```

**결과**: 한 시점에 한 Owner만 Jig를 표시합니다.

## 7. Source / Finish Magazine 역할 혼재

**결정**:

- Source: Slot01~07 공급, Slot08 EMPTY
- Finish: 완료 Jig 저장

**결과**: 공급량과 완료량을 별도로 추적할 수 있습니다.

## 8. SMT Jig 이동 속도 불일치

**문제**: fixed duration으로 거리별 속도가 달라졌습니다.

**결정**:

```text
speed = 0.15 m/s
duration = distance / speed
```

**결과**: Translation과 Process dwell을 분리했습니다.

## 9. Finish 삽입 전 Rotation

**문제**: Slot Rotation을 target으로 쓰면서 Jig가 삽입 직전 회전했습니다.

**결정**: Unloader rotation을 저장해 접근/삽입 동안 유지합니다.

```text
Quaternion.Angle(start, end) <= 0.01°
```

## 10. Simulation PASS / Actual Robot PASS 구분

**결정**:

```text
Simulation
→ Unity Live
→ SDK Read-only
→ Actual Command
→ Safety
```

**결과**: 문서와 구현에서 각 PASS를 별도 상태로 기록합니다.

## 11. 개발 PC / Laptop 환경 분리

**문제**: 개발 PC build artifact를 노트북에 복사하면 재현성을 보장하기 어렵습니다.

**결정**: Git lineage / SHA를 확인하고 Laptop에서 fresh build.

**결과**: Laptop Final Runtime HEAD `f02799c...`에서 Final TAKE를 재검증했습니다. 이 항목은 더 이상 “노트북 복원 Pending” 상태가 아닙니다.

## 12. Laptop RTF 저하

**문제**: Gazebo GUI + RViz 부하로 wall-clock timeout이 Simulation보다 먼저 만료됐습니다.

**결정**: Motion을 재튜닝하지 않고 true headless server-only mode를 추가했습니다.

**결과**: RTF `0.998`, MoveIt2 포함 `0.997`.

## 13. Camera 다중 출력 / AudioListener

**문제**: Main + Process Camera가 동시에 Game View 후보였고 AudioListener도 중복됐습니다.

**결정**: Camera Director가 단일 Game View output과 단일 AudioListener를 관리합니다.

**결과**: 15 Camera 중 Game View output 1 / AudioListener 1.

## 14. 포트폴리오 촬영 구조

**문제**: 일반 Screen Capture만으로는 Robot/Jig/공정별 clean shot을 만들기 어렵습니다.

**결정**: 기존 Process Camera 재사용 + Top / Close-up / Follow 추가, Unity Recorder 설치.

**결과**: 10-shot switching과 1080p30 recording configuration을 확보했습니다.

## 15. UI 연결 신뢰성

**문제**: 버튼 Text가 정상이어도 listener 중복·누락이 숨어 있을 수 있습니다.

**결정**: Scene Button을 read-only로 전수 수집하고 Target / Method / listener count를 기록했습니다.

**결과**: Button 96개, Missing Target/Method/Script 0, STOP listener 1. Reset 2개와 TAKE/Slot Runtime AddListener는 추가 source trace 대상으로 남겼습니다.

## 최종 설계 원칙

- 검증된 Pose/Take는 임의 재튜닝하지 않음
- Robot Base / Geometry를 표현 문제 해결용으로 움직이지 않음
- READ-ONLY audit 후 수정
- 한 계층이 다른 계층의 책임을 대신하지 않음
- Jig는 한 시점에 하나의 Owner
- Simulation / Unity / Hardware PASS 분리
- 실패 경로보다 Final PASS 기준을 Source of Truth로 유지
- Runtime performance 문제와 Motion 문제를 분리
- Portfolio presentation도 실제 검증 범위를 넘겨 과장하지 않음

---

[↑ 맨 위로](#top) · [문서 목차](README.md) · [프로젝트 README](../README.md)
