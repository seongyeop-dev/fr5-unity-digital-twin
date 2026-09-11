# FR5 Unity Digital Twin

Unity, ROS2, Gazebo, MoveIt2를 연결해 **FAIRINO FR5 협동로봇과 SMT 지그 이송 공정**을 디지털 트윈으로 구성한 프로젝트입니다.

실제 로봇 제어 계층, 시뮬레이션 계층, Unity 시각화 계층을 분리하고, Joint 상태 동기화·MoveIt2 경로 실행·Magazine Slot One-Take·SMT 공정 흐름을 단계적으로 검증했습니다.

> 이 저장소의 문서는 포트폴리오 공개용 요약본입니다.  
> 실제 제어, Gazebo 시뮬레이션, Unity 연출을 동일한 기능으로 표현하지 않습니다.

## 핵심 기능

- ROS2 `/joint_states` 기반 FR5 Joint 동기화
- Unity → ROS2 → Gazebo 명령 경로
- MoveIt2 Plan / Execute 연동
- FR5 MDH 기반 Python Ground Truth
- C# SDK Read-only / Mock Feedback 구조
- Gazebo Magazine Slot01~Slot07 One-Take 검증
- Unity SMT 공정 흐름 구성
- Source Magazine Slot01~07 / Slot08 EMPTY 정책
- External FR5 Jig 입력과 Finish Magazine 연결 구조
- Workcell / Material / Layout 검증 도구

## 전체 구조

```text
실제 FR5 / FR5 SDK
        ↕
      ROS2
        ↕
 MoveIt2 / Gazebo
        ↕
 Unity Digital Twin
        ↕
  Runtime UI / SMT
```

Python은 실시간 제어기가 아니라 **MDH 기반 정기구학 Ground Truth 검증 계층**으로 사용했습니다.

## 대표 데이터 흐름

```text
Gazebo / Robot
→ /joint_states
→ ROS TCP Endpoint
→ Unity Runtime Sync
→ FR5 Visual
```

```text
Unity UI
→ /fr5/unity_command
→ ROS2 Listener
→ Arm Controller
→ Gazebo
→ /joint_states
→ Unity Feedback
```

## SMT 공정

```text
FR5 Jig Place
→ EQ_Conveyor_01
→ EQ_Mounter_01
→ EQ_Inspection_01
→ EQ_Conveyor_02
→ EQ_Unloader
→ Finish Magazine
```

Source Magazine은 다음 정책으로 운용합니다.

```text
Slot01 ~ Slot07 : 사용
Slot08          : EMPTY
```

## 주요 검증 결과

| 항목 | 결과 |
|---|---|
| ROS2 `/joint_states` → Unity | PASS |
| Unity → ROS2 → Gazebo 명령 경로 | PASS |
| MoveIt2 Plan / Execute → Gazebo | PASS |
| Python MDH Ground Truth | PASS |
| C# SDK Read-only / Mock Feedback | PASS |
| Gazebo TAKE1~TAKE7 One-Take | PASS |
| Source 7 / Slot08 EMPTY 구성 | PASS |
| External FR5 Input Edit Mode 준비 | PASS |
| Unity Finish 공정 최종 동작 | 개선 중 |

Gazebo Slot One-Take 최종 기준은 `TAKE1~TAKE7`이며 `TAKE8`은 사용하지 않습니다.

## 현재 진행 상태

Unity SMT 단일 Jig 검증에서 다음 두 항목을 추가 개선 중입니다.

- `EQ_Inspection_01 → EQ_Conveyor_02` 구간 이동 속도 통일
- `EQ_Unloader → Finish Magazine` 직선 삽입 시 Jig 회전 제거

이 항목은 완료로 표시하지 않습니다.

## 문서

- [프로젝트 개요](docs/01_overview.md)
- [시스템 아키텍처](docs/02_architecture.md)
- [주요 기능](docs/03_features.md)
- [데이터 흐름](docs/04_data_flow.md)
- [검증 결과](docs/05_validation.md)
- [프로젝트 범위](docs/06_project_scope.md)
- [프로젝트 구조](docs/07_project_structure.md)

문서 전체 목차는 [docs/README.md](docs/README.md)를 참고하세요.

## 기술 스택

```text
Unity 6000.3.15f1
C#
ROS2 Jazzy
Gazebo Sim 8
MoveIt2
Python
FAIRINO FR5 SDK
Git / GitHub
```

## 저장소 공개 원칙

- 실제 Robot IP, Credential, Token 등 민감정보는 공개하지 않습니다.
- 외부 Asset과 Vendor SDK는 재배포 가능 여부를 확인한 뒤 포함합니다.
- Unity `Library`, `Temp`, `Logs`, `obj`, `UserSettings` 등 생성 폴더는 Git 대상에서 제외합니다.
