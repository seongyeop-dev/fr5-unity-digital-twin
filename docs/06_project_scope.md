# 06. 프로젝트 범위

## 완료·검증된 범위

### ROS2 / Gazebo

- JointState
- Arm Controller
- Gripper Command
- Unity Command Listener
- MoveIt2 Plan / Execute
- Slot01~Slot07 One-Take
- Negative J6 Trajectory 정책

### Unity

- FR5 Visual
- ROS2 Joint Sync
- Workcell 기본 구성
- SMT 생산라인
- Source Magazine Slot01~07
- Slot08 EMPTY
- External FR5 Jig 입력 준비
- Finish Magazine Lift 구조

### Python

- MDH FK
- Ground Truth
- Unity 비교 데이터

### C# SDK Bridge

- Read-only Preflight
- Mock Feedback
- Unity Runtime Source 연동

## 진행 중

- SMT 공정 간 이동 속도 통일
- Finish Magazine 직선 Insert
- Finish Placeholder Orientation
- Process Camera 실제 배치와 UI 연결
- Slot Operation UI 기능 연결
- OneTakeAll Unity UI 연결

## 별도 실기 검증이 필요한 범위

다음 항목은 Gazebo 또는 Mock PASS만으로 실제 FR5 완료로 간주하지 않습니다.

- 실제 FR5 전체 SMT End-to-End
- 실제 SDK Motion Command
- 실제 장비에서의 최종 안전 검증
- 실제 Workcell 설치 공차

## Unity 연출 범위

다음은 실제 산업 장비의 내부 제어 로직을 그대로 재현한다는 의미가 아닙니다.

- Conveyor 시각 이동
- Mounter 처리 연출
- Inspection 처리 연출
- Stack Light 상태
- Finish Magazine 시각 Hand-off

## 포트폴리오 표현 원칙

문서와 README에서는 다음을 구분해 표시합니다.

```text
실제 제어
시뮬레이션
Unity 시각화
미검증 / 진행 중
```
