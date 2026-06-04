# FR5 Digital Twin Pose Convention

## 1. 문서 목적

이 문서는 FR5 Digital Twin 프로젝트에서 Unity / ROS2 / SDK / Python 사이의 joint pose convention, 단위, topic, pose 용어를 명확히 분리하기 위한 기준 문서이다.

특히 `Home`, `Reset`, `Zero`, `Ready`가 같은 의미로 섞이지 않도록 현재 확인된 기준을 고정한다.

이 문서는 코드 동작을 변경하지 않는다. 현재 확인된 구조를 사람이 읽고 판단할 수 있도록 정리하는 문서이다.

## 2. 전체 convention 요약표

| Area | Source / File | External Unit | Unity Internal Unit | Policy |
|---|---|---:|---:|---|
| ROS2 JointState | `/joint_states` | radian | degree | `name` 기준으로 `joint1`~`joint6` mapping |
| ROS2 FR5 JointState | `/fr5/joint_states` | radian | degree | arm only topic, 현재 필수 변경 대상 아님 |
| Unity ROS2 Client | `Assets/Project/Scripts/RuntimeSources/ROS2/scr_FR5Ros2JointStateClient.cs` | radian | degree | `TryBuildJointDegrees()`에서 rad -> deg 변환 |
| Unity Runtime Sample | `Assets/Project/Scripts/Core/Runtime/FR5SdkPoseSample.cs` | - | degree | `jointDegrees`가 J1~J6 기준 저장소 |
| Unity Visual | `Assets/Project/Scripts/Robot/Control/scr_VirtualJointController.cs` | degree | degree | visual layer에서 joint별 axis/sign 보정 |
| Unity Command Publish | `/fr5/unity_command` | degree JSON | degree | `joints_deg` 사용, dry-run command |
| Python FK / MDH | `Python/Phase1_Kinematics` | degree input | radian internal | Ground Truth / validation 기준 |
| SDK / C# Bridge | Fairino SDK / BridgeTools | degree 중심 | degree | `GetActualJointPosDegree`, `MoveJ`, `JointPos.jPos` 기준 |

## 3. Topic 기준표

| Topic | Message Type | Unit | Current Status | Notes |
|---|---|---:|---|---|
| `/joint_states` | `sensor_msgs/msg/JointState` | radian | Unity가 현재 구독 중 | gripper joint가 앞에 포함될 수 있음 |
| `/fr5/joint_states` | `sensor_msgs/msg/JointState` | radian | Publisher 존재, Unity subscription 없음 | arm only topic |
| `/fr5/unity_command` | `std_msgs/msg/String` | JSON 내부 `joints_deg`는 degree | Unity publish 정상 | 현재 dry-run listener 검증용 |

현재 확인된 `/joint_states` name 순서:

```text
jaw_a_joint
jaw_b_joint
joint1
joint2
joint3
joint4
joint5
joint6
```

현재 확인된 `/fr5/joint_states` name 순서:

```text
joint1
joint2
joint3
joint4
joint5
joint6
```

## 4. JointState name-based mapping 정책

Unity ROS2 JointState mapping은 반드시 position index가 아니라 joint name 기준으로 판단한다.

현재 기준:

```text
expected arm joint names:
joint1
joint2
joint3
joint4
joint5
joint6
```

`scr_FR5Ros2JointStateClient.cs`는 `JointStateMsg.name` 배열에서 `joint1`~`joint6`을 찾고, 찾은 index의 `position` 값을 읽는다.

따라서 `/joint_states`처럼 `jaw_a_joint`, `jaw_b_joint`가 앞에 있어도 arm joint가 2칸 밀리지 않는다. `jaw_a_joint`, `jaw_b_joint` 같은 extra joint는 expected name이 아니므로 arm mapping에서 무시한다.

정책:

- Always map by joint name, not by position index.
- Expected arm joint names are `joint1`~`joint6`.
- Extra joints such as `jaw_a_joint` and `jaw_b_joint` are ignored.
- `/joint_states`와 `/fr5/joint_states` 모두 처리 가능해야 한다.
- `JointState.name`이 비어 있거나 `joint1`~`joint6`이 없으면 sample invalid로 처리한다.

## 5. Unit conversion flow

### ROS2 JointState -> Unity Runtime Sample

```text
sensor_msgs/msg/JointState.position
radian
-> scr_FR5Ros2JointStateClient.TryBuildJointDegrees()
-> Mathf.Rad2Deg
-> FR5SdkPoseSample.jointDegrees
degree
```

변환식:

```csharp
jointDegrees[i] = (float)(msg.position[msgIndex] * Mathf.Rad2Deg);
```

### Unity Runtime Sample -> Unity Visual

```text
FR5SdkPoseSample.jointDegrees
degree
-> scr_FR5RuntimeSyncManager
-> scr_VirtualJointController.SetJointAngleByIndex()
-> scr_VirtualJointController.ApplyPose()
-> Unity Robot Visual
```

`scr_VirtualJointController.cs`는 Unity visual layer에서 joint별 `axis`와 `sign`을 적용한다. 이 보정은 Runtime Sample 자체의 degree convention을 바꾸는 것이 아니다.

### Unity Command -> ROS2

```text
Unity Button
-> scr_FR5UICommandRouter
-> scr_FR5Ros2CommandPublisher
-> std_msgs/msg/String JSON
-> /fr5/unity_command
```

`MOVE_J` command의 joint 배열은 `joints_deg`이며 unit은 degree이다.

## 6. Pose 용어 정의표

| Term | Value | Unit | Meaning | Status |
|---|---|---:|---|---|
| `RobotZeroPoseDeg` | `[0, 0, 0, 0, 0, 0]` | degree | canonical robot joint zero pose | 현재 기준 유지 |
| `UnityCommandHomePresetDeg` | `[0, -90, 90, -90, -90, 0]` | degree | current command UI home preset | SDK Home으로 확정 금지 |
| `SdkHomePoseDeg` | TBD | degree | actual Fairino SDK / real robot home pose | SDK 또는 robot controller 확인 필요 |
| `ROS2LivePose` | latest `/joint_states` or `/fr5/joint_states` | radian on wire, degree in Unity | ROS2에서 수신한 최신 pose | Unity visual의 live 기준 |
| `ReplayPose` | replay JSON sample | degree preferred, radian fallback | playback sample pose | replay source 기준 |
| `PythonGroundTruthPose` | 6 joint values | degree input, radian internal | MDH/FK validation pose | 검증 기준 |
| `ResetCommand` | command event | - | reset 요청 command | zero pose 적용과 동일시 금지 |
| `ResetToZeroPose` | `[0, 0, 0, 0, 0, 0]` 적용 | degree | 실제 zero pose로 local pose를 바꾸는 동작 | ROS2 Live Source에서는 직접 적용 금지 |
| `Ready` | state label | - | status label only | pose로 사용 금지 |

## 7. Active flow 정리

### ROS2 -> Unity

```text
/joint_states
sensor_msgs/msg/JointState.position
radian
-> scr_FR5Ros2JointStateClient
-> name-based mapping joint1~joint6
-> rad -> deg
-> FR5SdkPoseSample.jointDegrees
-> scr_FR5RuntimeSyncManager
-> scr_VirtualJointController
-> Unity Robot Visual
```

### Unity -> ROS2

```text
Unity Button
-> scr_FR5UICommandRouter
-> scr_FR5Ros2CommandPublisher
-> /fr5/unity_command
-> std_msgs/msg/String JSON
-> fr5_ros2_bridge/fr5_unity_command_listener.py
-> dry-run logging
```

현재 `/fr5/unity_command`는 실제 robot controller 또는 trajectory topic으로 publish하지 않는다.

### Replay -> Unity

```text
Unity Replay JSON
-> positions_deg preferred
-> positions_rad fallback with Mathf.Rad2Deg
-> FR5SdkPoseSample.jointDegrees
-> scr_FR5RuntimeSyncManager
-> scr_VirtualJointController
```

### Python FK validation

```text
Python joint input
degree
-> Python MDH / FK solver
-> internal radian math
-> Ground Truth / validation output
```

Python FK / MDH는 Unity visual axis/sign 보정과 분리해서 본다.

### SDK / C# Bridge

```text
Fairino SDK / C# Bridge
-> GetActualJointPosDegree / JointPos.jPos
-> jointDegrees
-> Unity Runtime Sample
```

Fairino SDK / C# Bridge는 degree 중심 API로 판단한다. 다만 실제 `SdkHomePoseDeg`는 아직 실제 SDK 또는 robot controller 기준 확인이 필요하다.

## 8. 위험한 혼동 지점

### Home

`Home`은 아직 하나의 확정된 의미가 아니다.

- `RobotZeroPoseDeg`: `[0, 0, 0, 0, 0, 0]`
- `UnityCommandHomePresetDeg`: `[0, -90, 90, -90, -90, 0]`
- `SdkHomePoseDeg`: TBD

현재 `[0, -90, 90, -90, -90, 0]` preset을 `SDK Home`이라고 확정해서 부르면 안 된다.

### Reset

`ResetCommand`는 command/event semantic이다. 반드시 zero pose 적용을 의미하지 않는다.

실제 `[0, 0, 0, 0, 0, 0]` pose 적용은 `ResetToZeroPose`로 별도 명명한다.

### Ready

`Ready`는 state/status label이다. pose로 사용하지 않는다.

### Unity visual axis/sign

Unity visual layer의 axis/sign 보정은 mesh 표시를 위한 보정이다. ROS2, SDK, Python의 joint degree convention 자체를 바꾸는 기준으로 사용하지 않는다.

### Topic 변경

`/fr5/joint_states`는 arm only topic이라 깔끔하지만, 현재 Unity live sync는 `/joint_states`에서 이미 동작 중이다. 현재 권장 정책은 `/joint_states` 유지 + name-based mapping 문서화이다.

`/fr5/joint_states`로 변경하려면 Scene/Inspector 변경과 회귀 검증이 필요하다.

## 9. 현재 권장 정책

현재 권장 정책:

```text
/joint_states 유지
+ name-based mapping 유지
+ joint1~joint6만 arm joint로 사용
+ jaw_a_joint, jaw_b_joint는 무시
+ Unity internal Runtime Sample은 degree 유지
+ Unity command JSON은 joints_deg 유지
```

ROS2 Live Source 상태에서는 Unity local `Home` / `Reset` pose를 직접 적용하지 않는다. Unity visual은 `ROS2LivePose`만 따라간다.

`HOME`, `RESET`, `MOVE_J`, `STOP`은 `/fr5/unity_command`로 publish한다. 현재는 dry-run listener만 수신하며 실제 robot controller command는 보내지 않는다.

## 10. 이후 확인 필요 항목

다음 항목은 실제 Ubuntu ROS2 workspace 또는 실제 SDK / robot controller 기준 확인이 필요하다.

| Item | Purpose |
|---|---|
| `ros2 topic echo /joint_states --once` | 현재 gripper + arm joint name/order 확인 |
| `ros2 topic echo /fr5/joint_states --once` | arm only joint name/order 확인 |
| `ros2 topic info /joint_states` | Unity subscription 연결 확인 |
| `ros2 topic info /fr5/joint_states` | arm only topic subscription 여부 확인 |
| `ros2 topic echo /fr5/unity_command --once` | Unity command JSON 확인 |
| actual SDK home command/result | `SdkHomePoseDeg` 확정 |
| real robot controller home definition | `SdkHomePoseDeg`와 UI preset 비교 |
| trajectory publish 설계 | `joints_deg`를 ROS2 trajectory radian으로 변환할 위치 확정 |

## 11. 수정 금지 / 삭제 금지 주의사항

이 문서는 현재 기준을 기록하기 위한 문서이며, 아래 작업을 의미하지 않는다.

- 코드 수정 금지
- 기존 파일 수정 금지
- 파일 삭제 금지
- 파일 이동 금지
- 파일 이름 변경 금지
- Scene/Prefab 수정 금지
- build 실행 금지
- commit/push 금지

특히 다음 항목은 이름만 보고 정리하거나 삭제하지 않는다.

- `Assets/Project/Scripts/RuntimeSources/ROS2/scr_FR5Ros2JointStateClient.cs`
- `Assets/Project/Scripts/Core/Runtime/FR5SdkPoseSample.cs`
- `Assets/Project/Scripts/Core/Runtime/scr_FR5RuntimeSyncManager.cs`
- `Assets/Project/Scripts/Robot/Control/scr_VirtualJointController.cs`
- `Assets/Project/Scripts/RuntimeSources/ROS2/scr_FR5Ros2CommandPublisher.cs`
- `Assets/Project/Scripts/UI/Actions/scr_FR5UICommandRouter.cs`
- `Python/Phase1_Kinematics`
- `BridgeTools`
- `ThirdParty`

이번 문서는 현재 확인된 기준을 고정하기 위한 문서이다.

실제 SDK Home pose는 아직 TBD이다.

실제 robot motion command 연결 전까지 `/fr5/unity_command`는 dry-run 검증을 유지한다.
