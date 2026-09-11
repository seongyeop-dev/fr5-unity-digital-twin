# 14. Deployment & Laptop Handoff

## 목적

개발 PC에서 검증한 Simulation Motion과 Command Backend를 같은 Git 기준으로 노트북에 재현하기 위한 인계 문서입니다. 파일을 수동 복사하는 대신 Git lineage, 보호 파일 SHA256, 새 Build, 단계별 Preflight로 실행 환경의 차이를 관리합니다.

아래 확인 완료 항목은 개발 PC Ubuntu의 최종 확인 결과를 기준으로 합니다. 노트북 환경 복원과 실제 FR5 Hardware 검증은 아직 Pending이며, Simulation PASS와 구분합니다.

## Source of Truth

| 항목 | 확정 기준 |
|:---|:---|
| Repository | `git@github.com:seongyeop-dev/fr5_ros2_ws.git` |
| Branch | `feat/fr5-gazebo-jig-attach-detach` |
| Ubuntu Workspace | `~/fr5_ros2_ws` |
| 개발 PC Local HEAD | `46cf3ace69154e8befb2fb3a78686cd931c3428a` |
| GitHub origin branch HEAD | `46cf3ace69154e8befb2fb3a78686cd931c3428a` |
| Ahead / Behind | `0 / 0` |

노트북 이관의 Source of Truth는 위 GitHub branch의 고정 commit입니다. 이 표는 최종 확인 시점의 parity이며, 향후 branch가 이동하더라도 인계 기준 commit과 보호 SHA를 별도로 비교합니다. 최종 Slot / Conveyor 배치 이전 상태인 기존 노트북 Workspace를 기준본으로 사용하지 않습니다.

## Locked Motion Baseline

| Asset | Path | SHA256 | Role |
|:---|:---|:---|:---|
| Motion Master | `src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py` | `80009dda5e196e8afbc4242bd859a35b5982d0efef531fdcc9286293f4ae59be` | 검증된 Slot Motion과 TAKE 선택 |
| Slot YAML | `src/fr5_moveit_config/config/slot01_to_slot08_final_one_take_v1.yaml` | `b823401d6c77037ec35502a8e11ac35692f6f4a86ff7bf6eb8825a3f486e6` | Slot별 Motion 설정 |
| Workcell World | `src/fr5_gazebo/worlds/fr5_workcell.sdf` | `dac1c53f068aa56dd497cf3f66e64559dda1af010584566c71d96bf95104be65` | 최종 Gazebo 설비 배치 |
| Workcell Launch | `src/fr5_gazebo/launch/fr5_workcell.launch.py` | `2abca81b2dfdd50777f48970bca8ad3cc213b37486742c8b22cfd647b45d9607` | Workcell 실행 기준 |

운영 정책:

- TAKE1~TAKE7 사용, TAKE8 unused
- ACTION05는 Slot01~02에서만 허용, Slot03~07에서는 금지
- 검증된 Trajectory의 모든 Point에서 `J6 < 0`
- 이관을 위해 Robot Base, Joint axis, 검증된 Pose/Trajectory 또는 설비 배치를 임의 보정하지 않음

Motion 검증 내용은 [11. Motion & Slot Validation](11_motion_and_slot_validation.md)을 참고합니다.

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

개발 PC Ubuntu는 Simulation / Development 기준본 생성 역할을 종료합니다. 검증 결과는 GitHub의 `46cf3ace69154e8befb2fb3a78686cd931c3428a` 기준으로 보존하고, 이후 실제 FR5 연동은 노트북 Ubuntu에서 진행합니다.

개발 PC의 실행 프로세스, 절대 user 경로 또는 생성 산출물에 기대어 노트북을 구성하지 않습니다. 최종 Source 조사에서는 Runtime의 절대 user 경로 의존성과 Source symlink 의존성이 발견되지 않았습니다. 이 결과가 노트북의 의존성·장비·네트워크 검증을 대신하지는 않습니다.

## Laptop Migration Strategy

1. 기존 노트북 Workspace의 Git remote, branch, commit, dirty 상태와 로컬 변경을 먼저 확인합니다.
2. 같은 Git lineage이며 로컬 변경을 보존한 채 fast-forward 가능한 경우에만 기준 branch로 update합니다.
3. 분기되었거나 기준이 불명확하면 기존 Workspace를 보존하고 별도 경로에 fresh clone합니다. 로컬 작업을 폐기하는 reset/clean 방식은 사용하지 않습니다.
4. 받은 Source의 commit과 보호 SHA를 위 표와 비교합니다. 불일치하면 실행하지 않고 원인을 확인합니다.
5. `build/`, `install/`, `log/`는 개발 PC에서 복사하지 않고 노트북에서 새로 생성합니다.

별도 경로를 사용했다면 `${HOME}/fr5_ros2_ws`를 참조하는 기존 Runner가 오래된 Workspace를 source하지 않는지 확인해야 합니다. 기존 폴더를 덮어쓰거나 symlink로 우회하지 않고, 실제 사용할 Workspace 경로와 실행 환경을 먼저 일치시킵니다.

## Environment Restore

Target은 Ubuntu 24.04 / ROS2 Jazzy / `ROS_DOMAIN_ID=90`입니다. 의존성을 복원하고 fresh build한 뒤 새 install 환경만 source합니다.

아래는 commit/hash 검증이 끝난 노트북 Workspace에서 수행할 복원 절차 예시이며, 이 문서 작업에서 실행한 명령이 아닙니다.

```bash
source /opt/ros/jazzy/setup.bash
rosdep install --from-paths src --ignore-src -r -y --rosdistro jazzy
colcon build --symlink-install
source install/setup.bash
export ROS_DOMAIN_ID=90
```

`rosdep` 설정과 권한, 의존성 설치 결과를 먼저 확인합니다. 다른 Workspace의 install overlay를 중복 source하지 않으며, 환경 복원 과정에서는 Motion Master나 command bridge를 자동 실행하지 않습니다.

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

- Unity `RUN_TAKE` payload와 Slot/OneTakeAll 연결
- Backend `RUN_TAKE` dispatch 및 검증된 Master 호출
- request/status correlation과 Unity Status Subscriber
- TAKE 실행 중 BUSY 및 중복 요청 처리
- active Master를 대상으로 한 STOP·종료 처리
- 노트북 Source 복원 / fresh build / read-only preflight 검증
- 실제 FR5 feedback, command, 안전 중단 및 End-to-End Hardware 검증

현재 상태는 **simulation motion baseline validated**, **backend command bridge implemented for legacy commands**, **TAKE selector verified in master**입니다. **Unity-to-TAKE dispatch remains an integration item**, **laptop hardware validation pending** 경계를 유지합니다.

---

[문서 목차](README.md) · [05. Validation](05_validation.md) · [10. FR5 SDK Integration](10_fr5_sdk_integration.md) · [프로젝트 README](../README.md)
