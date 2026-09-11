# 06. 프로젝트 범위

## 주요 문제 해결

### 1. Slot 높이에 따른 Pick 간섭

Slot01에서 안정적인 Direct Pick을 확보한 뒤 Slot이 높아질수록 Magazine Frame 간섭 위험이 증가했습니다. Slot02 이상에는 PREGRASP와 짧은 Cartesian Approach를 적용해 진입 경로를 분리했습니다.

### 2. Wrist IK Branch 전환

높은 Slot에서 Wrist가 다른 Branch로 전환되는 현상을 확인해 Trajectory 전체 Point에서 Negative J6를 검사하는 조건을 추가했습니다.

### 3. Gazebo / MoveIt 좌표 정합

Gazebo Workcell과 MoveIt Collision Object의 Z 기준 차이를 Planning Scene 변환에서 보정했습니다. Robot Base 자체를 이동해 문제를 맞추는 방식은 사용하지 않았습니다.

### 4. Jig Transport 표현

Jig를 최종 위치로 즉시 이동시키는 대신 Tool-to-Jig Relative Transform과 LIVE TF 기반 rigid follower를 사용해 Carry 구간을 표현했습니다.

### 5. Unity Jig 중복 표시

Jig를 Source, Carried, Runtime, Finish 상태로 구분하고 Workcell Event 기준으로 Ownership을 전환해 동일 Jig가 여러 위치에 동시에 보이는 문제를 해결했습니다.

### 6. Simulation과 Actual Robot 경계

Unity Play Mode나 Gazebo `--execute`가 실제 FR5 Motion 허용과 같은 의미가 되지 않도록 Simulation, Read-only Feedback, Actual Command 경로를 분리했습니다.

## 최종 구현 범위

### Robot / Motion

- FR5 Gazebo Simulation
- MoveIt Planning Scene
- Joint/Cartesian Motion
- Slot01~07 One-Take
- Jig Follow / Release / Conveyor
- Trajectory Branch Validation

### Unity Digital Twin

- FR5 Joint Runtime Sync
- Workcell Layout
- Source / Finish Magazine
- Magazine Conveyor
- Jig Ownership
- SMT Process
- External FR5 Input
- UI / Command Routing

### Robot Interface

- FR5 SDK 연결 구조
- Read-only Feedback
- ROS2 Command Publisher / Listener
- Command Status
- Simulation / Actual Robot Mode Separation

## 구현 범위에서 제외하거나 보수적으로 유지한 부분

- Slot08은 최상단 간섭 위험으로 최종 운영 범위에서 제외
- Unity는 Gazebo를 대체하는 Robot Physics Simulator로 사용하지 않음
- Simulation PASS를 Actual FR5 Hardware PASS로 표현하지 않음
- 실제 장비 Safety/Speed는 최종 Hardware Validation 전 확정하지 않음

## 남은 연동

- Unity SMT Transfer Speed 최종 보정
- Finish Magazine Straight Insert 최종 확인
- Unity ↔ ROS2 ↔ Actual FR5 End-to-End 검증
- 실제 FR5 Motion/Feedback 최종 검증
- 대표 이미지 / 검증 영상 추가

큰 구조나 Motion Policy를 다시 설계하는 단계는 종료했고, 현재는 통합과 최종 검증 단계에 있습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
