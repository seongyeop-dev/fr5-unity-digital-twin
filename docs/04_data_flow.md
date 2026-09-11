# 04. 데이터 흐름

## 1. Robot / Gazebo → Unity

```text
FR5 또는 Gazebo
→ /joint_states
→ ROS TCP Endpoint
→ Unity ROSConnection
→ JointState Client
→ Runtime Sync Manager
→ Virtual Joint Controller
→ FR5 Visual
```

핵심은 Joint 배열 Index가 아니라 `JointState.name`으로 매핑하는 것입니다.

## 2. Unity → ROS2 → Gazebo

```text
Unity UI
→ JSON Command
→ /fr5/unity_command
→ fr5_unity_command_listener
→ /fr5_arm_controller/joint_trajectory
→ Gazebo
→ /joint_states
→ Unity
```

## 3. MoveIt2

```text
Target Pose
→ MoveIt2 Planning
→ Collision / IK
→ Trajectory
→ Gazebo Controller
→ Robot Motion
```

## 4. Python Ground Truth

```text
Joint Degree
→ MDH Parameter
→ FK
→ Position / Rotation
→ JSON / CSV / TXT
→ Unity C# FK 비교
```

## 5. C# SDK Bridge

```text
FR5 SDK 또는 Mock
→ C# Bridge
→ State JSON
→ Unity CSharpBridge Source
→ Runtime Sample
→ UI / Robot Visual
```

## 6. SMT External Jig

```text
FR5 Jig Release
→ TryAcceptFr5InsertedJig()
→ 3초 초기 Dwell
→ EQ_Conveyor_01
→ EQ_Mounter_01
→ EQ_Inspection_01
→ EQ_Conveyor_02
→ EQ_Unloader
→ Finish Magazine
```

External 모드에서는 다음 Jig를 자동 생성하지 않습니다.

## 7. Finish Visual Hand-off

목표 흐름:

```text
Unloader 도착
→ Finish Lift 높이 정렬
→ Jig 직선 삽입
→ Runtime Jig 비활성
→ filledSlot 활성
```

현재 구현은 이 구조를 사용하지만 삽입 직전 Rotation 제거를 추가 검증 중입니다.
