# FR5 ROS2 Map

이 문서는 FR5 Digital Twin의 ROS2 관련 구조를 구분하기 위한 문서입니다. 현재 Windows 프로젝트 루트와 Ubuntu ROS2 workspace는 분리되어 있습니다.

## Common Tags

- `ACTIVE`
- `SUPPORT`
- `VALIDATION`
- `DEBUG`
- `STANDBY`
- `LEGACY_CANDIDATE`
- `DO_NOT_DELETE`

## Workspace Boundary

실제 ROS2 workspace 기준:

```text
~/fr5_ros2_ws
```

Windows 프로젝트 루트:

```text
PROJECT_ROOT_260531/PROJECT_ROOT_260531
```

현재 Windows 루트에는 `fr5_ros2_ws` source tree가 직접 포함되어 있지 않을 수 있습니다. ROS2 package source, `build`, `install`, `log`는 Ubuntu 쪽에서 별도로 확인해야 합니다.

## ROS2 Package Roles

| Package / Area | Status | Role |
|---|---|---|
| `fr5_description` | `SUPPORT` / `DO_NOT_DELETE` | FR5 URDF/Xacro, robot description, TF/RViz model basis |
| `fr5_gazebo` | `SUPPORT` / `STANDBY` | Gazebo launch, controllers, simulation config |
| `fr5_ros2_bridge` | `ACTIVE` / `SUPPORT` | `/joint_states` publish, Unity command listener, Unity bridge nodes |
| `ros_tcp_endpoint` | `ACTIVE` | Unity ROS-TCP-Connector와 ROS2 network bridge |
| `launch` | `SUPPORT` | ROS2 node/Gazebo/bridge launch entrypoints |
| `config` | `SUPPORT` | controller yaml, bridge config |
| `urdf/xacro` | `SUPPORT` / `DO_NOT_DELETE` | robot model and joint chain definition |
| `controller` | `SUPPORT` | ros2_control settings |
| listener node | `ACTIVE` / `STANDBY` | `fr5_unity_command_listener.py` dry-run command logging |

## ROS2 to Unity Flow

```text
/joint_states
→ ros_tcp_endpoint
→ Unity ROSConnectionPrefab
→ scr_FR5Ros2JointStateClient
→ scr_FR5RuntimeSyncManager
→ scr_VirtualJointController
→ Unity Robot Visual
```

Unity Scene currently serializes ROS2 topic as `/joint_states` for the active client. Some docs and replay files also reference `/fr5/joint_states`; topic naming should be checked per launch/test context.

## Unity to ROS2 Flow

```text
Unity Button
→ scr_FR5UICommandRouter
→ scr_FR5Ros2CommandPublisher
→ /fr5/unity_command
→ fr5_ros2_bridge/fr5_unity_command_listener.py
→ dry-run logging
```

The first publish phase uses `std_msgs/String` JSON only. It must not publish actual robot trajectory commands.

## Generated Folders

| Path | Meaning | Rule |
|---|---|---|
| `build` | colcon build output | 확인 없이 삭제 금지 |
| `install` | colcon install output | 확인 없이 삭제 금지 |
| `log` | colcon logs | 확인 없이 삭제 금지 |

`build/install/log`는 일반적으로 생성 산출물이지만, 현재 workspace 재현성, launch 상태, debugging history에 필요할 수 있습니다. `.gitignore`, symlink install 여부, 실행 재현성을 확인한 뒤 별도 정리해야 합니다.
