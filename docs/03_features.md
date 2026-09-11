# 03. 주요 기능

## 1. FR5 Joint 동기화

ROS2 `sensor_msgs/JointState`를 Unity에서 수신하고 Joint 이름을 기준으로 J1~J6을 매핑합니다.

Gripper Joint가 앞에 포함될 수 있으므로 배열 Index를 직접 사용하지 않습니다.

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

## 2. Unity → ROS2 명령

Unity UI에서 생성한 명령을 `/fr5/unity_command`로 전달하고 ROS2 Listener가 Arm Controller로 연결합니다.

`/fr5/command_status`를 통해 결과 상태를 받을 수 있도록 구성했습니다.

## 3. MoveIt2 Plan / Execute

- `/move_group`
- RViz MotionPlanning
- Plan
- Execute
- Gazebo Arm Motion

흐름을 검증했습니다.

## 4. Python MDH Ground Truth

FR5 MDH 기반 FK를 Python으로 구현해 JSON / CSV / TXT 형태의 Ground Truth를 생성했습니다.

Unity C# FK와 비교하는 기준으로 사용합니다.

## 5. C# SDK Bridge

- Config 기반 Robot 연결 정보
- Read-only 모드
- Mock Feedback
- JSON 출력
- Unity Runtime Source = `CSharpBridge`

구조를 구현했습니다.

## 6. Magazine Slot One-Take

최종 Master:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

운영 기준:

```text
TAKE1 ~ TAKE7 : PASS / LOCK
TAKE8         : 미사용
```

## 7. Negative J6 정책

최종 Trajectory는 모든 Point에서 다음 조건을 유지합니다.

```text
J6 < 0
```

## 8. Unity SMT 공정

```text
FR5 Place
→ Conveyor 01
→ Mounter
→ Inspection
→ Conveyor 02
→ Unloader
→ Finish Magazine
```

## 9. Source Magazine

```text
Slot01 ~ Slot07 : 사용
Slot08          : EMPTY
```

Source Jig는 Runtime이 임의로 이동시키지 않습니다.

## 10. External FR5 Jig 입력

외부에서 전달된 Jig 인스턴스를 SMT 공정에 연결합니다.

검증 항목:

- Source Slot 1~7
- Jig 중복 방지
- Source Slot 중복 방지
- Dynamic Rigidbody 방지
- Finish Slot 존재 확인
- Legacy Runtime Jig 자동 생성 방지

## 11. Finish Magazine

Finish Lift를 대상 Slot 높이에 정렬한 뒤 Jig를 삽입하고, Runtime Jig를 비활성화한 후 filledSlot Visual로 전환합니다.

현재 삽입 Rotation과 공정 이동 속도는 추가 개선 중입니다.

## 12. Workcell 검증 도구

- Layout Analysis
- Layout Apply
- Layout Validate
- Material Assignment
- Protected Transform 검사
- Source / Finish Editor Setup

도구를 사용해 Scene 변경 범위를 제한했습니다.
