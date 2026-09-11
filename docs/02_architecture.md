# 02. 시스템 아키텍처

## 전체 구조

```text
┌─────────────────────────────┐
│ 실제 FAIRINO FR5 / FR5 SDK │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ ROS2 Jazzy                  │
│ JointState / Command / TF   │
└──────────────┬──────────────┘
               │
      ┌────────┴────────┐
      ▼                 ▼
┌──────────────┐  ┌──────────────┐
│ MoveIt2      │  │ Gazebo Sim 8 │
│ Planning / IK│  │ Physics      │
└──────────────┘  └──────┬───────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │ Unity           │
                 │ Digital Twin    │
                 └─────────────────┘
```

## Unity

Unity의 역할:

- FR5 Robot Visual
- Workcell
- SMT 생산라인
- Source / Finish Magazine
- Runtime UI
- Process Camera
- ROS2 / C# Bridge 상태 표현

기준 Scene:

```text
Assets/Project/Scenes/01_FR_Simulator.unity
```

## ROS2

ROS2의 역할:

- `/joint_states`
- Unity 명령 수신
- Arm / Gripper Controller
- MoveIt2 실행
- Gazebo 상태 전달

## Gazebo

Gazebo는 Robot Motion, Physics, Gripper, Jig, Magazine, Conveyor를 검증하는 시뮬레이션 계층입니다.

시각 외형의 검증 기준도 RViz보다 Gazebo를 우선했습니다.

## MoveIt2

MoveIt2의 역할:

- Planning
- IK
- Collision Check
- Start State 검증
- Trajectory Execution

## Python

Python은 실시간 제어 대신 다음 용도로 사용합니다.

```text
FR5 MDH
→ FK
→ Ground Truth
→ Unity C# FK 비교
```

## C# SDK Bridge

FR5 SDK 연결은 Read-only와 Mock Feedback부터 검증했습니다.

```text
FR5 SDK / Mock
→ C# Bridge
→ JSON
→ Unity Runtime Source
```

Mock 검증 결과를 실제 Robot Motion 완료로 해석하지 않습니다.

## 설계 원칙

실제 Robot, ROS2/Gazebo, Unity의 책임을 섞지 않는 것이 핵심입니다.

Unity Coroutine으로 움직인 Object는 실제 FR5 제어 완료로 취급하지 않습니다.
