# 01. 프로젝트 개요

## 프로젝트 목표

FAIRINO FR5를 중심으로 실제 로봇 제어, ROS2, Gazebo, MoveIt2, Unity를 연결하고, SMT 카메라 모듈 지그 이송 공정을 디지털 트윈으로 표현·검증하는 것이 목표입니다.

단순 애니메이션이 아니라 다음 계층을 분리했습니다.

```text
실제 FR5 / SDK
ROS2 / MoveIt2
Gazebo Simulation
Unity Digital Twin
```

## 해결하려 한 문제

산업용 로봇 프로젝트에서는 다음 문제가 자주 발생합니다.

- 실제 Robot과 Simulation의 Joint 상태가 다르게 보임
- Unity에서 보이는 동작이 실제 명령인지 시각 연출인지 구분하기 어려움
- MoveIt2 Planning 결과와 Gazebo 실행 결과가 일치하지 않음
- Magazine / Jig / Conveyor 공정이 별도 Script에 흩어짐
- 디지털 트윈이 실제 공정 검증보다 화면 연출 중심으로 변질됨

이 프로젝트에서는 각 계층의 책임을 분리하고 검증 결과를 기준으로 기능 상태를 관리했습니다.

## 핵심 결과

- ROS2 JointState를 Unity FR5 Visual에 동기화
- Unity 명령을 ROS2와 Gazebo까지 전달
- MoveIt2 Plan / Execute와 Gazebo 실행 연동
- Python MDH Ground Truth 구축
- C# SDK Read-only / Mock Feedback 경로 구축
- Magazine Slot01~Slot07 One-Take 검증
- Unity Workcell과 SMT 생산라인 구성
- Source Magazine과 Finish Magazine 역할 분리
- External FR5 Jig 입력 구조 구현

## SMT 생산 흐름

```text
FR5
→ EQ_Conveyor_01
→ EQ_Mounter_01
→ EQ_Inspection_01
→ EQ_Conveyor_02
→ EQ_Unloader
→ Finish Magazine
```

Source Magazine은 Slot01~Slot07을 사용하며 Slot08은 비워 둡니다.

## 현재 상태

ROS2/Gazebo Slot One-Take는 `TAKE1~TAKE7`까지 최종 PASS했습니다.

Unity SMT 공정은 Source 7개 구성과 External FR5 입력 준비까지 검증했으며, Finish 공정의 이동 속도와 삽입 방향을 추가 개선 중입니다.
