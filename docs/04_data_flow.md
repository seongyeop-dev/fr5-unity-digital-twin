# 04. 데이터 흐름

## 전체 Runtime 흐름

```text
Robot / Simulator
      ↓
ROS2 State / Event
      ↓
Unity Runtime
      ↓
Robot View / Workcell / UI
```

## Joint State 흐름

```text
Gazebo 또는 FR5 Feedback
        ↓
     /joint_states
        ↓
scr_FR5Ros2JointStateClient
        ↓
scr_FR5RuntimeSyncManager
        ↓
scr_VirtualJointController
        ↓
Unity FR5 J1~J6
```

Joint Name을 기준으로 J1~J6 값을 추출하고 ROS radian 값을 Unity Joint 기준으로 변환한 뒤 Local Rotation에 적용합니다.

## Unity Command 흐름

```text
Unity UI
   ↓
scr_FR5UICommandRouter
   ↓
scr_FR5Ros2CommandPublisher
   ↓
ROS2 Command Topic
   ↓
Ubuntu Listener
   ↓
MoveIt / Robot Interface
   ↓
Command Status
   ↓
Unity
```

UI와 Robot 실행 로직을 직접 연결하지 않고 Command Router와 ROS2 Publisher를 사이에 두었습니다.

## MoveIt / Gazebo Motion 흐름

```text
Current Joint State
       ↓
Slot Configuration
       ↓
Planning Scene Check
       ↓
Joint / Cartesian Planning
       ↓
Trajectory Validation
       ↓
Execute
       ↓
Gazebo FR5 Motion
       ↓
Jig Rigid Follower
```

Slot별 Configuration은 Python Master와 YAML 설정으로 분리해 관리합니다.

## Jig Ownership 흐름

```text
Source Jig
   │ PICK_DONE
   ▼
Carried Jig
   │ PLACE_DONE
   ▼
SMT Runtime Jig
   │ Finish Complete
   ▼
Finish Jig
```

이벤트가 발생할 때 위치만 옮기는 것이 아니라 현재 Jig Visual의 소유 상태를 함께 변경합니다.

## Unity SMT Process 흐름

```text
PLACE_DONE / External FR5 Input
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

Process Controller는 Jig Transfer와 Process Dwell을 구분해 관리합니다.

## Simulation / Actual Feedback 흐름

Simulation:

```text
Gazebo / MoveIt2
→ ROS2
→ Unity
```

Actual Robot:

```text
FR5
→ FR5 SDK
→ Bridge / ROS2
→ Unity
```

두 경로가 같은 Unity Runtime으로 들어오더라도 입력 모드를 분리해 Test Value와 External Feedback이 동시에 적용되지 않도록 구성했습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
