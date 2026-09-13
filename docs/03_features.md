<a id="top"></a>

# 03. 주요 기능

> 기능을 “있다/없다”가 아니라 **구현 상태와 검증 상태를 함께** 기록합니다.

[문서 목차](README.md) · [프로젝트 README](../README.md) · [Validation](05_validation.md)

## 기능 Matrix

| 영역 | 기능 | 상태 |
|:---|:---|:---:|
| Gazebo | FR5 Workcell / Controller | PASS |
| MoveIt2 | Planning Scene / Joint / Cartesian | PASS |
| Motion | TAKE1~TAKE7 | PASS |
| Motion | TAKE8 | OUT OF SCOPE |
| Motion | Negative-J6 Guard | PASS |
| Jig | LIVE TF rigid follower | PASS |
| Unity | ROS2 JointState Sync | 구현 완료 |
| Unity | Source / Finish Magazine | 구현 완료 |
| Unity | Jig Ownership | 구현 완료 |
| Unity | SMT Process | 구현 완료 |
| Unity | Camera 10-shot switching | PASS |
| Unity | Cinematic Follow | 구현 완료 · Play Mode 시점 전환 확인 |
| Unity | Recorder 5.1.7 | FHD 1080p30 설정 완료 |
| Unity | Recorder MP4 sample | 검증 예정 |
| GUI | Workcell Status Text | PASS (Scene Verify) |
| GUI | STOP single listener | PASS |
| GUI | 96 Button Audit | 구조 확인 |
| ROS2↔Unity | Live JointState E2E | 최종 통합 검증 예정 |
| TAKE | Unity `RUN_TAKE` Backend | 후속 통합 단계 |
| SDK | Read-only Feedback 구조 | 구현 완료 |
| Actual FR5 | Hardware E2E | 실제 장비 검증 예정 |

## FR5 SDK 중간 시연 데모

Digital Twin 이전 단계에서 FR5 SDK 교육을 기반으로 실제 Robot Control Demo를 구성했습니다.

| 시연 | 보존 근거 |
|:---|:---|
| 메뉴 1 칵테일 제조 | Demo 문서 / 이미지 / MP4 |
| 메뉴 2 칵테일 제조 | Demo 문서 / 이미지 / MP4 |
| Pick & Place | Demo 문서 / 이미지 / MP4 |
| 제어 Source | Lua script |
| Position / Reference | Point DB / DIO reference |

이 단계의 목적은 현재 Digital Twin 기능을 중복 설명하는 것이 아니라, **SDK 기반 실제 Robot Control 경험이 이후 ROS2·Gazebo·MoveIt2·Unity 구조로 확장됐음을 보여주는 것**입니다.

초기 제어 프로젝트: [`demos/01_fr5_sdk_cocktail_robot_demo`](../demos/01_fr5_sdk_cocktail_robot_demo/)

## ROS2 / Gazebo / MoveIt2

### Gazebo Workcell

FR5 Robot, Robot Table, Magazine, Magazine Conveyor, Jig Place Conveyor, Jig Inventory를 하나의 Simulation Workcell로 구성했습니다. Robot Base를 고정하고 표현 문제를 해결하기 위해 Robot Geometry를 바꾸지 않았습니다.

### MoveIt Planning Scene

Gazebo Facility와 대응하는 Collision Object를 MoveIt Planning Scene에 구성했습니다. Allowed Collision Matrix는 필요한 예외만 허용합니다.

### Slot01~07 One-Take

```text
Gripper Open
→ PREGRASP / PICK
→ Gripper Close
→ Extract
→ Carry
→ Pre-Insert
→ Straight Insert
→ Gripper Open
→ Retreat
→ Conveyor
```

통합 모션 시퀀스:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

### Cartesian Motion

직선성 자체가 기능 요구사항인 구간에 Cartesian Path를 사용했습니다.

- Pick 직전 접근
- Magazine Extract
- Pre-Insert → Insert
- Release 후 Retreat

### Negative J6 Guard

끝점만 정상인 trajectory가 중간에 다른 Wrist Branch로 넘어가는 문제를 막기 위해 전체 Point를 검사합니다.

```text
for every trajectory point:
    J6 < 0
```

### Jig Rigid Follower

Attach 시 Tool-to-Jig relative pose를 저장하고 LIVE TF를 사용해 Carry 구간에서 Jig가 Tool을 따라가도록 했습니다.

## Unity Digital Twin

### Joint Runtime Sync

```text
/joint_states
→ scr_FR5Ros2JointStateClient
→ scr_FR5RuntimeSyncManager
→ scr_VirtualJointController
→ J1~J6
```

### Source / Finish Magazine

```text
Source Slot01~07 : Jig
Source Slot08    : EMPTY
Finish Magazine  : 완료 Jig 저장
```

### Jig Visual Ownership

```text
Source
→ Carried
→ Runtime SMT
→ Finish
```

### SMT Process

```text
Conveyor01
→ Mounter
→ Inspection
→ Conveyor02
→ Unloader
→ Finish Magazine
```

Translation은 공통 `0.15 m/s` 기준으로 계산하고 Process dwell과 분리했습니다.

## Unity Camera / Recording

### Camera Shot

| Key | Shot |
|:---:|:---|
| `1` | FR5 Cell Wide |
| `2` | Source Magazine |
| `3` | Jig Insert |
| `4` | Mounter |
| `5` | Inspection |
| `6` | Conveyor02 |
| `7` | Unloader |
| `8` | Top Overview |
| `9` | FR5 Close-up |
| `0` | Cinematic Follow |
| `` ` `` | Main Camera |

Play Mode에서 Shot 전환 자체는 확인했습니다. 최종 framing은 ROS2 Live 촬영 시 실제 동작을 보면서 조정합니다.

### Recorder

- Package: `com.unity.recorder@5.1.7`
- Source: Game View
- Resolution: FHD 1080p
- Aspect: 16:9
- Encoder: Unity Media Encoder
- Codec: H.264 MP4
- Quality: High
- Frame: Constant 30 FPS
- Audio: OFF
- Output: `Project/Recordings`

실제 5~10초 샘플 영상은 최종 촬영 단계에서 생성·재생을 확인할 예정입니다.

## GUI / UI 구성 점검

- STOP listener 1개
- Workcell Status Text binding
- 기술 고유명사 `FR5`, `ROS2`, `SDK`, `TCP`는 영문 유지
- 신규 상태/공정 사용자 문구는 한국어 중심
- Button 96개 구조 점검
- Missing Target / Method / Script = 0
- Slot01~08 / OneTakeAll 9개 zero persistent listener는 Runtime `AddListener` trace 필요

## FR5 SDK / Robot Interface

실제 Robot 연동은 다음 순서를 기본으로 합니다.

```text
Simulation
→ Read-only Feedback
→ Command Path
→ Actual Robot Validation
```

Simulation 결과를 Hardware PASS로 승격하지 않습니다.

---

[↑ 맨 위로](#top) · [문서 목차](README.md) · [프로젝트 README](../README.md)
