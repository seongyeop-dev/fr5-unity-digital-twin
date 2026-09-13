<a id="top"></a>

# 04. 데이터 흐름

> Robot State, UI Command, Jig Ownership, Process Event가 서로 다른 흐름을 가지도록 분리했습니다.

[문서 목차](README.md) · [프로젝트 README](../README.md) · [Architecture](02_architecture.md)

## 데이터 흐름 요약

| 흐름 | Source | Destination | 상태 |
|:---|:---|:---|:---:|
| Joint Feedback | Gazebo / FR5 | Unity J1~J6 | 시뮬레이션 경로 구성 |
| Unity Command | UI | ROS2 Listener | 기존 명령 처리 경로 구성 |
| Command Status | ROS2 Backend | Unity | Status Publisher 구성, TAKE 연동은 후속 단계 |
| Jig Ownership | Source / FR5 / SMT | Finish | 상태 전환 구현 |
| Camera | Camera Director | Game View / Recorder | 시점 전환 구현 |
| RUN_TAKE | Unity Slot | Motion Sequence | 후속 통합 단계 |

## JointState Flow

```mermaid
sequenceDiagram
    participant G as Gazebo / FR5
    participant R as ROS2
    participant C as JointStateClient
    participant S as RuntimeSyncManager
    participant V as VirtualJointController

    G->>R: Joint state
    R->>C: /joint_states
    C->>S: J1~J6 mapped values
    S->>V: selected runtime source
    V->>V: Local Rotation update
```

Joint name을 기준으로 J1~J6를 추출하고 ROS radian을 Unity Joint 기준에 맞춰 적용합니다.

## Unity Command / Status Flow

```mermaid
sequenceDiagram
    participant UI as Unity UI
    participant Router as UICommandRouter
    participant Pub as ROS2CommandPublisher
    participant L as Ubuntu Listener
    participant Ctrl as Gazebo / Controller
    participant Stat as Command Status

    UI->>Router: user action
    Router->>Pub: normalized command
    Pub->>L: /fr5/unity_command JSON
    L->>Ctrl: legacy command dispatch
    L-->>Stat: /fr5/command_status JSON
```

현재 Listener는 기존 MOVE_J / HOME / RESET / STOP / Gripper 명령을 처리합니다. `RUN_TAKE`는 아직 연결되지 않았습니다.

## TAKE Flow: 현재와 목표

현재:

```mermaid
flowchart LR
    SLOT["Unity Slot UI"] --> SEL["TAKE selection / mapping"]
    SEL --> STOP["enableTakeDispatch = false"]
```

목표:

```mermaid
sequenceDiagram
    participant U as Unity
    participant L as ROS2 Listener
    participant M as Motion Sequence
    participant S as Status

    U->>L: RUN_TAKE + request_id
    L->>M: FR5_TAKE=1..7 / ALL
    M-->>L: BUSY / COMPLETE / FAILED
    L-->>S: correlated status
    S-->>U: request completion
```

위 목표 계약은 아직 구현 완료 상태가 아닙니다.

## MoveIt / Gazebo Motion Flow

```mermaid
flowchart TB
    JS["Current Joint State"] --> CFG["Slot Configuration"]
    CFG --> PS["Planning Scene"]
    PS --> PLAN["Joint / Cartesian Plan"]
    PLAN --> GUARD["Negative-J6 Guard"]
    GUARD --> EXEC["Execute"]
    EXEC --> GZ["Gazebo Motion"]
    GZ --> JIG["LIVE TF Jig Follower"]
```

## Jig Ownership Flow

```mermaid
stateDiagram-v2
    [*] --> Source
    Source --> Carried: PICK_DONE
    Carried --> Runtime: PLACE_DONE
    Runtime --> Finish: Process Complete
    Finish --> [*]
```

위치는 물론 현재 visual owner 자체를 바꿔 동일 Jig가 여러 위치에 동시에 보이지 않게 합니다.

## SMT Process Flow

```mermaid
sequenceDiagram
    participant F as FR5 / External Input
    participant C1 as Conveyor01
    participant M as Mounter
    participant I as Inspection
    participant C2 as Conveyor02
    participant U as Unloader
    participant FM as Finish Magazine

    F->>C1: PLACE_DONE
    C1->>M: Transfer
    M->>I: Dwell complete
    I->>C2: Transfer
    C2->>U: Transfer
    U->>FM: Straight insert / handoff
```

## Camera / Recording Flow

```mermaid
flowchart LR
    K["Keyboard / Camera UI"] --> D["PortfolioCameraDirector"]
    D --> C["Selected Shot Camera"]
    C --> G["Single Game View"]
    G --> R["Unity Recorder"]
    R --> MP4["Project/Recordings/*.mp4"]
```

RenderTexture Camera는 기존 UI 용도로 계속 유지합니다.

## Simulation / Actual Feedback

```mermaid
flowchart TB
    G["Gazebo / MoveIt2"] --> R["ROS2"] --> U["Unity"]
    F["Actual FR5"] -.-> S["FR5 SDK"] -.-> U
```

두 입력이 동시에 Unity Joints를 소유하지 않도록 Runtime 입력 소유권을 유지합니다.

---

[↑ 맨 위로](#top) · [문서 목차](README.md) · [프로젝트 README](../README.md)
