# 01. 프로젝트 개요

## 개발 배경

FAIRINO FR5를 Unity에서 시각화하고 Joint 상태를 반영하는 작업을 시작으로, 실제 로봇 시스템과 동일한 흐름을 검증하기 위해 ROS2, Gazebo, MoveIt2, FR5 SDK까지 범위를 확장했습니다.

프로젝트의 목표는 하나의 프로그램에서 모든 기능을 처리하는 것이 아니라, 각 계층의 역할을 분리한 상태에서 FR5의 상태·모션·공정 흐름을 연결하는 것이었습니다.

## 개발 목표

- FR5 Robot Joint 상태를 Unity Digital Twin에 실시간 반영
- Gazebo Workcell과 MoveIt Planning Scene의 공간 기준 정합
- Magazine Slot별 Pick & Place Motion 자동화
- Jig Pick/Carry/Insert/Release 과정을 시뮬레이션에서 검증
- Unity에서 Source → SMT Process → Finish Magazine 공정 표현
- Simulation과 실제 FR5 SDK 경로를 분리해 단계적으로 통합
- 구현 결과를 반복 검증할 수 있는 Script/Validation 구조 유지

## 구현 범위

### ROS2 / Gazebo / MoveIt2

- ROS2 Jazzy 기반 FR5 Workspace 구성
- Gazebo FR5 Workcell 구성
- MoveIt Planning Scene과 Collision Object 구성
- Joint Goal / Cartesian Path 기반 Motion Planning
- Slot01~07 One-Take Pick & Place
- Jig rigid follower, Release, Conveyor 이송
- Negative J6 Trajectory 검증

### Unity

- FR5 J1~J6 Joint Hierarchy 구성
- ROS2 JointState Runtime Sync
- Source / Finish Magazine 구성
- Jig Visual Ownership 관리
- SMT Process Sequence 구성
- UI / Command Routing / External FR5 Input 구성

### FR5 SDK

- 실제 FR5 상태 확인을 위한 Read-only Feedback 구조
- Simulation / Actual Robot Mode 분리
- ROS2 Command Path와 Unity Bridge 구조
- 실제 Robot Motion 적용 전 단계별 검증 경계 유지

## 운영 기준

프로젝트를 진행하면서 이미 검증된 Robot Base, Table, 설비 위치, Slot Motion을 불필요하게 다시 수정하지 않는 기준을 유지했습니다.

Motion은 Slot별로 하나씩 검증한 뒤 최종적으로 다음 Master에 누적했습니다.

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

Unity에서도 Edit Mode의 정적 기준과 Play Mode Runtime 상태를 분리해, Runtime Test 때문에 Scene 기준값이 바뀌지 않도록 관리했습니다.

## 최종 공정 흐름

```text
Source Magazine
      ↓
     FR5
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

Source Magazine은 Slot01~07을 사용하며 Slot08은 최종 운영 범위에서 제외했습니다.

## 현재 상태

### 완료 또는 기준 확정

- Gazebo FR5 Workcell
- MoveIt Planning Scene
- Slot01~07 One-Take Simulation
- Negative J6 Trajectory Policy
- LIVE TF 기반 Jig rigid follower
- Unity FR5 Joint Runtime Sync 구조
- Source Magazine / Finish Magazine 분리
- Unity SMT Process
- External FR5 Input 구조
- FR5 SDK Read-only / Command Path 분리

### 최종 확인 예정

- Unity SMT Transfer Speed 최종 통일
- Finish Magazine Straight Insert 최종 확인
- Unity ↔ ROS2 ↔ 실제 FR5 End-to-End 연동
- Actual FR5 Motion / Feedback 최종 검증
- 대표 이미지 및 검증 영상 추가

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
