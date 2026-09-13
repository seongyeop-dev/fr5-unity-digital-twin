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
| 15 | UI 연결 신뢰성 | Button 구조 점검 |
| 16 | SDK Demo와 Digital Twin 통합 | 초기 SDK Demo를 하위 프로젝트로 포함해 발전 과정 연결 |

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

**문제**: 목표점으로 직접 배치하는 방식은 Tool-Jig 관계를 잃습니다.

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

**결과**: 각 실행 영역에서 한 시점의 표현 owner를 구분합니다. ROS2 follower와 Unity external Jig 입력은 별도 경계이고, Unity Finish는 runtime Jig hide 후 placeholder를 표시합니다.

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
RotationDriftDegrees(start, end) <= 0.01 degree
```

## 10. 실제 제어 경험과 Simulation 책임

**결정**: Cocktail Robot Demo의 FR5 SDK/Gripper/DIO/Lua 경험을 Digital Twin 설계에 반영하되, 실물 Demo와 ROS2/Gazebo 시뮬레이션 결과를 각 실행 환경에 대응시킵니다.

**결과**: Digital Twin에서는 feedback source, simulation command, SMT 공정과 Camera를 분리했습니다.

## 11. 개발 PC / Laptop 환경 분리

**문제**: 개발 PC build artifact를 노트북에 복사하면 재현성을 보장하기 어렵습니다.

**결정**: Workspace revision / SHA를 확인하고 Laptop에서 fresh build.

**결과**: 노트북 Ubuntu 환경에서 동일한 TAKE1~TAKE7 시뮬레이션을 다시 실행해 동작을 확인했습니다.

## 12. Laptop RTF 저하

**문제**: Gazebo GUI + RViz 부하로 wall-clock timeout이 Simulation보다 먼저 만료됐습니다.

**결정**: Motion을 재튜닝하지 않고 true headless server-only mode를 추가했습니다.

**결과**: RTF `0.998`, MoveIt2 포함 `0.997`.

## 13. Camera 다중 출력 / AudioListener

**문제**: Main + Process Camera가 동시에 Game View 후보였고 AudioListener도 중복됐습니다.

**결정**: Camera Director가 단일 Game View output과 단일 AudioListener를 관리합니다.

**결과**: 15 Camera 중 Game View output 1 / AudioListener 1.

## 14. 촬영 구조

**문제**: 일반 Screen Capture만으로는 Robot/Jig/공정별 clean shot을 만들기 어렵습니다.

**결정**: 기존 Process Camera 재사용 + Top / Close-up / Follow 추가, Unity Recorder 설치.

**결과**: 10-shot switching과 1080p30 recording configuration을 확보했습니다.

## 15. UI 연결 신뢰성

**문제**: 버튼 Text가 정상이어도 listener 중복·누락이 숨어 있을 수 있습니다.

**결정**: Scene Button을 read-only로 전수 수집하고 Target / Method / listener count를 기록했습니다.

**결과**: Button 96개를 대상으로 Target/Method/Script와 STOP 단일 entry point를 검사했습니다. Persistent listener와 Runtime AddListener를 구분하여 버튼 이름만으로 연결 성공을 판단하지 않았습니다.

## 16. SDK 중간 시연과 Digital Twin 통합

**문제**: Cocktail Robot Demo는 FR5 SDK 교육 기반의 중요한 실제 Robot Control 이력이지만, 독립 저장소로 계속 유지하면 현재 Digital Twin과의 발전 관계가 끊겨 보이고 Portfolio repository 수도 불필요하게 증가합니다.

**결정**: FR5 SDK 기반 Cocktail Robot Demo를 `demos/` 아래 하위 프로젝트로 포함해 초기 제어 단계와 Digital Twin 확장 과정을 한 저장소에서 확인할 수 있도록 구성했습니다.

**결과**: `SDK 교육 → Cocktail Robot Demo → Unity Digital Twin → ROS2/Gazebo/MoveIt2`의 발전 흐름을 한 저장소에서 설명하면서도, 기존 Demo의 README·문서·소스·미디어는 원본 그대로 유지합니다.

## 최종 설계 원칙

- 검증된 Pose/Take는 임의 재튜닝하지 않음
- Robot Base / Geometry를 표현 문제 해결용으로 움직이지 않음
- 현재 상태를 먼저 확인한 뒤 필요한 범위만 수정
- 한 계층이 다른 계층의 책임을 대신하지 않음
- Jig는 한 시점에 하나의 Owner
- Simulation / Unity / Hardware PASS 분리
- 확인한 실행 경로와 검사 기준을 기준 구성으로 유지
- Runtime performance 문제와 Motion 문제를 분리
- Portfolio presentation도 실제 검증 범위를 넘겨 과장하지 않음

## 17. 독립 FK 비교

**결정**: Python MDH와 Unity C# FK를 별도로 계산하고 좌표계·축 방향·오차와 tolerance를 비교했습니다.

**결과**: transform order와 frame 정의의 차이를 확인 가능한 검증 항목으로 다룹니다. 현재 두 구현의 완전 일치를 전제하지 않습니다.

---

## 문서 목차

### 기본 문서

| 번호 | 문서 | 내용 |
| ---: | --- | --- |
| **01** | [Overview](01_overview.md) | 프로젝트 배경과 핵심 결과 |
| **02** | [Architecture](02_architecture.md) | Simulation, Integration, Unity, 수학 검증 계층 |
| **03** | [Features](03_features.md) | 구현 기능과 책임 |
| **04** | [Data Flow](04_data_flow.md) | Joint, Command, Jig, Camera 흐름 |
| **05** | [Validation](05_validation.md) | 수학·시뮬레이션·Unity 검증 |
| **06** | [Project Scope](06_project_scope.md) | 프로젝트별 실행 환경과 설계 경계 |
| **07** | [Project Structure](07_project_structure.md) | 공개 소스와 별도 ROS2 저장소 |

### 상세 기술 문서

| 번호 | 문서 | 내용 |
| ---: | --- | --- |
| **08** | [ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) | Workcell, Planning, TF follower |
| **09** | [Unity Digital Twin](09_unity_digital_twin.md) | 핵심 Runtime Script 구조 |
| **10** | [FR5 SDK Interface](10_fr5_sdk_integration.md) | 실제 제어 경험과 Read-only Bridge |
| **11** | [Motion & Slot Validation](11_motion_and_slot_validation.md) | TAKE1~TAKE7 및 Trajectory 검사 |
| **12** | [Script Reference](12_script_reference.md) | 실제 코드 경로·역할·의존 관계 |
| **13** | [Design Decisions](13_design_decisions_and_issues.md) | 주요 문제와 해결 |
| **14** | [Deployment](14_deployment_and_handoff.md) | Unity·Python·Bridge·ROS2 실행 환경 |
| **15** | [Simulation Runtime](15_laptop_ros2_simulation_runtime.md) | Headless, RTF, 노트북 실행 결과 |
| **16** | [Camera & Recording](16_unity_camera_and_recording.md) | 시점 전환·관찰·녹화 설정 |

### 추가 자료

| 구분 | 바로가기 |
| --- | --- |
| **프로젝트** | [프로젝트 README](../README.md) · [전체 문서 인덱스](README.md) |
| **수학 · 기구학 검증** | [Validation](05_validation.md) · [Motion Validation](11_motion_and_slot_validation.md) · [MDH Parameters](../Python/Phase1_Kinematics/Python_MDH/fr5_mdh_params.py) · [Python FK Solver](../Python/Phase1_Kinematics/Python_MDH/fr5_fk_solver.py) · [Kinematics Tests](../Python/Phase1_Kinematics/Tests/) |
| **실제 FR5 · Cocktail Demo** | [Demo Hub](../demos/README.md) · [FR5 SDK Cocktail Robot Demo](../demos/01_fr5_sdk_cocktail_robot_demo/README.md) |
| **ROS2 Simulation** | [ROS2 / Gazebo / MoveIt2](08_ros2_gazebo_moveit.md) · [fr5_ros2_ws Repository](https://github.com/seongyeop-dev/fr5_ros2_ws) |

[문서 목록으로 이동](README.md)
