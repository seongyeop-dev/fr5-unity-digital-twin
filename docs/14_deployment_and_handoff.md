# 14. Deployment & Laptop Handoff

## 목적

개발 PC에서 검증한 Simulation Motion과 Command Backend를 같은 Git 기준으로 노트북에 재현하기 위한 인계 문서입니다. 파일을 수동 복사하는 대신 Git lineage, 보호 파일 SHA256, 새 Build, 단계별 Preflight로 실행 환경의 차이를 관리합니다.

개발 PC에서 확정한 Simulation 기준은 노트북 Ubuntu로 이관되었고, fresh build, Gazebo / MoveIt2 Runtime, Planning Scene, Final TAKE1~TAKE7 Simulation 실행까지 노트북에서 재검증을 완료했습니다. 실제 FR5 Hardware 검증은 아직 별도 단계이며 Simulation PASS와 구분합니다.

## Source of Truth

| 항목 | 확정 기준 |
|:---|:---|
| Repository | `git@github.com:seongyeop-dev/fr5_ros2_ws.git` |
| Branch | `feat/fr5-gazebo-jig-attach-detach` |
| Ubuntu Workspace | `~/fr5_ros2_ws` |
| 개발 PC 최종 Simulation Baseline | `46cf3ace69154e8befb2fb3a78686cd931c3428a` |
| Laptop Final Runtime HEAD | `f02799cfd3126210ef72238990861c9c027c84af` |
| GitHub origin branch HEAD | `f02799cfd3126210ef72238990861c9c027c84af` |
| Laptop Local / Remote parity | PASS |

개발 PC의 `46cf3ace...`는 TAKE1~TAKE7 Motion을 확정한 기준 commit으로 보존합니다.
노트북에서는 해당 기준을 fast-forward로 이관한 뒤,
headless Gazebo Runtime 지원을 추가한 `f02799cfd3126210ef72238990861c9c027c84af`까지 검증했습니다.

Motion Master 자체는 변경하지 않았으며 보호 SHA를 별도로 확인합니다.

## Locked Motion Baseline

| Asset | Path | SHA256 | Role |
|:---|:---|:---|:---|
| Motion Master | `src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py` | `80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be` | 검증된 Slot Motion과 TAKE 선택 |
| Slot YAML | `src/fr5_moveit_config/config/slot01_to_slot08_final_one_take_v1.yaml` | `b823401d6c77037ec35502a8e11ac35692f6f4a86ff7bf6c8efb8825a3f486e6` | Slot별 Motion 설정 |
| Workcell World | `src/fr5_gazebo/worlds/fr5_workcell.sdf` | `dac1c53f068aa56dd497cf3f66e64559dda1af010584566c71d96bf95104be65` | 최종 Gazebo 설비 배치 |
| Workcell Launch | `src/fr5_gazebo/launch/fr5_workcell.launch.py` | `a6b8c094d9653ae3bc65fcd56df2714d912f5fee78bec51dd1e7c56b50daead6` | Workcell 및 headless Runtime 진입점 |
| Gazebo Control Launch | `src/fr5_gazebo/launch/fr5_gazebo_control.launch.py` | `d2fd8e715b99ea1d65e1519b1cb8f198dfb09f8f48e61e31f13ded0f7edb907f` | Gazebo / ros2_control 및 `gz_args` 전달 |

운영 정책:

- TAKE1~TAKE7 사용, TAKE8 unused
- ACTION05는 Slot01~02에서만 허용, Slot03~07에서는 금지
- 검증된 Trajectory의 모든 Point에서 `J6 < 0`
- Robot Base, Joint axis, 검증된 Pose/Trajectory 또는 설비 배치를 임의 보정하지 않음
- 노트북 Runtime 성능 문제 해결을 위해 Motion 자체를 재튜닝하지 않음

Motion 검증은
[11. Motion & Slot Validation](11_motion_and_slot_validation.md),
Laptop Runtime 상세는
[15. Laptop ROS2 Simulation Runtime](15_laptop_ros2_simulation_runtime.md)을 참고합니다.

## ROS2 Runtime Contract

실제 Listener:

```text
src/fr5_ros2_bridge/fr5_ros2_bridge/fr5_unity_command_listener.py
```

현재 Listener SHA256:

```text
db65ea65279d78f468c3b078c039e15d3b3c72288d046fa42e845d1331ec8425
```

확인된 기존 명령 경로와 미완료 수신 경계를 구분합니다.

```text
Unity Router / Publisher
  → /fr5/unity_command [std_msgs/msg/String, JSON]
  → fr5_unity_command_listener
  → Gazebo / controller

Backend Status Publisher [구현됨]
  → /fr5/command_status [std_msgs/msg/String, JSON]
  → Unity Status Subscriber / TAKE correlation [Pending]
```

Listener의 기존 지원 명령:

```text
MOVE_J, HOME, RESET, STOP
GRIPPER_OPEN, GRIPPER_SMALL_CLOSE
GRIPPER_NORMAL_CLOSE, GRIPPER_RETURN_OPEN
```

기존 `STOP`은 hold trajectory를 처리합니다. 이를 실행 중인 TAKE Master 중단이나 실제 FR5 Emergency Stop 검증과 동일하게 취급하지 않습니다.

기존 Status JSON 구조는 다음과 같습니다. `...`, boolean 및 timestamp는 구조를 보여주기 위한 예시값이며 실제 실행 결과가 아닙니다.

```json
{
  "source": "ros2",
  "robot": "FR5",
  "command": "...",
  "accepted": true,
  "executed": true,
  "state": "...",
  "message": "...",
  "timestamp_unix_ms": 0
}
```

Backend Status Publisher는 구현되어 있지만 Unity Status Subscriber와 TAKE 완료 correlation은 아직 완료되지 않았습니다. 현재 Backend에는 TAKE 전용 `request_id` / `fr5_take` field가 없으며, 위 schema의 `state`만으로 TAKE용 BUSY/완료 계약이 구현됐다고 판단하지 않습니다.

### Launch / Runner

`src/fr5_gazebo/launch/fr5_gazebo_control.launch.py`에 Listener가 포함되며 다음 parameter를 사용합니다.

| Parameter | 값 |
|:---|:---|
| `command_topic` | `/fr5/unity_command` |
| `command_status_topic` | `/fr5/command_status` |

`scripts/run_fr5_gazebo_command_bridge.sh`는 `${HOME}/fr5_ros2_ws`를 기준으로 환경을 source하고 bridge launch를 실행합니다. 특정 username 경로에 고정되어 있지 않으며 `EXECUTE_UNITY_TRAJECTORY` 환경변수를 지원합니다. 이 변수는 Gazebo bridge 실행 설정이지 실제 Hardware command 허가가 아닙니다.

## TAKE Execution Boundary

| 계층 | 현재 상태 |
|:---|:---|
| Simulation Master | `FR5_TAKE=1`~`7` 및 `ALL` selector 확인 |
| Master `ALL` | TAKE1 → TAKE7 순차 실행, TAKE8 제외 |
| Master 실행 구분 | `--execute`로 Simulation 실행 |
| Command Listener | 기존 명령 지원, `RUN_TAKE` 미구현 |
| Unity Slot01~07 | TAKE Mapping/선택 UI, 실제 TAKE 요청은 NOT SENT |
| Unity OneTakeAll | Backend 연결 전 비활성 유지 |
| TAKE 요청/상태 대응 | request/status correlation, BUSY, 완료·중단 처리 Pending |

Master의 `ALL` 지원과 Unity OneTakeAll 버튼의 실행 연결은 서로 다른 구현 단계입니다. `Unity Slot Button → RUN_TAKE → Master`는 남은 통합 항목이며, 현재 지원하는 요청처럼 새 JSON payload를 정의하지 않습니다. Master 실행 예는 [11. Motion & Slot Validation](11_motion_and_slot_validation.md)에 정리했습니다.

## Development PC Freeze

개발 PC Ubuntu는 Simulation Motion 기준본 생성 역할을 종료했습니다.
TAKE1~TAKE7 Motion baseline은 개발 PC의
`46cf3ace69154e8befb2fb3a78686cd931c3428a`에서 확정했습니다.

이후 ROS2 Simulation Runtime 운영 기준은 노트북 Ubuntu로 이동했으며,
노트북 Final Runtime HEAD는 `f02799cfd3126210ef72238990861c9c027c84af`입니다.

Windows 개발 PC는 Unity Digital Twin과 SDK Runtime을 유지하며,
노트북 ROS2 / Gazebo / MoveIt2와 Network Integration 및
동시 촬영을 담당하는 구조로 분리합니다.

## Laptop Migration Strategy

1. 기존 노트북 Workspace의 Git remote, branch, commit, dirty 상태와 로컬 변경을 먼저 확인합니다.
2. 같은 Git lineage이며 로컬 변경을 보존한 채 fast-forward 가능한 경우에만 기준 branch로 update합니다.
3. 분기되었거나 기준이 불명확하면 기존 Workspace를 보존하고 별도 경로에 fresh clone합니다. 로컬 작업을 폐기하는 reset/clean 방식은 사용하지 않습니다.
4. 받은 Source의 commit과 보호 SHA를 위 표와 비교합니다. 불일치하면 실행하지 않고 원인을 확인합니다.
5. `build/`, `install/`, `log/`는 개발 PC에서 복사하지 않고 노트북에서 새로 생성합니다.

별도 경로를 사용했다면 `${HOME}/fr5_ros2_ws`를 참조하는 기존 Runner가 오래된 Workspace를 source하지 않는지 확인해야 합니다. 기존 폴더를 덮어쓰거나 symlink로 우회하지 않고, 실제 사용할 Workspace 경로와 실행 환경을 먼저 일치시킵니다.

## Environment Restore

Target은 Ubuntu 24.04.4 / ROS2 Jazzy / `ROS_DOMAIN_ID=90`입니다.

노트북에서 Source parity 확인 후 기존 build artifact를 기준으로 사용하지 않고
fresh `colcon build --symlink-install`을 완료했습니다.

최종 Laptop Runtime HEAD:

```text
f02799cfd3126210ef72238990861c9c027c84af
```

Gazebo Simulation execute 경로에 필요한 Python binding도 확인했습니다.

```text
python3-gz-msgs10
python3-gz-transport13
```

True headless 실행은 다음 launch 경로를 사용합니다.

```bash
ros2 launch fr5_gazebo fr5_workcell.launch.py \
  gz_args:="-s -r"
```

검증 결과 Gazebo 단독 RTF `0.998`,
MoveIt2 포함 RTF `0.997`을 확인했습니다.

노트북 복원·fresh build·Simulation Runtime preflight는 더 이상 Pending 항목이 아닙니다.

## Pre-Hardware Validation

다음 순서를 통과한 뒤에만 명시적으로 command를 enable합니다.

1. Git branch / commit 확인
2. 보호 파일 SHA 확인
3. `rosdep` dependency restore
4. `colcon build` — 노트북에서 fresh build
5. `source install/setup.bash`
6. `ROS_DOMAIN_ID=90` 확인
7. read-only ROS graph 확인 — Node / Topic / 실행 모드
8. read-only FR5 joint feedback 검증 — 실제 장비 수신 상태와 Joint mapping
9. Unity network 및 feedback 경로 검증 — 명령 전송 없이 확인
10. STOP path 검증 — 실행 대상과 중단 범위를 별도 확인; legacy hold trajectory와 active Master/Hardware 중단을 구분
11. command enable — 앞 단계 통과 및 실제 장비 실행 승인 후에만 진행

Build 성공, ROS 연결, Simulation `--execute`는 실제 FR5 Motion 허용을 의미하지 않습니다. STOP 검증이 끝나기 전에는 자동 TAKE 실행을 허용하지 않습니다.

## Remaining Integration

노트북 Simulation Runtime 자체의 이관과 실행 검증은 완료되었습니다.

다음 통합 항목:

- Laptop ROS-TCP Endpoint ↔ Windows Unity Live 연결
- Unity ROS2 Runtime Source에서 실제 `/joint_states` 수신 확인
- Gazebo / RViz / Unity 동일 동작 동시 표시 및 포트폴리오 촬영
- Unity `RUN_TAKE` payload와 Backend Master dispatch 연결
- request/status correlation, BUSY, 완료·중단 처리
- active Master를 대상으로 한 STOP / 종료 처리
- 실제 FAIRINO FR5 feedback / command / 안전 중단 검증
- Actual Robot ↔ Unity SDK End-to-End 촬영 및 검증

현재 상태는 **Laptop Simulation Runtime validated**,
**TAKE1→TAKE7 Final Simulation PASS**입니다.

Windows Unity Live Integration과 Actual Robot Hardware Validation은
각각 별도 PASS 상태로 검증합니다.

---

[문서 목차](README.md) · [05. Validation](05_validation.md) · [10. FR5 SDK Integration](10_fr5_sdk_integration.md) · [프로젝트 README](../README.md)
