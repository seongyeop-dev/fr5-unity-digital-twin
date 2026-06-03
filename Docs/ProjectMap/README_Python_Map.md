# FR5 Python Map

이 문서는 `Python` 폴더의 역할을 구분하기 위한 문서입니다. Python 코드는 Unity와 직접 연결되지 않더라도 FR5 수학 검증의 기준이므로 보수적으로 유지합니다.

## Common Tags

- `ACTIVE`
- `SUPPORT`
- `VALIDATION`
- `DEBUG`
- `STANDBY`
- `LEGACY_CANDIDATE`
- `DO_NOT_DELETE`

## Python Folder Map

| Path | Status | Role | Delete Rule |
|---|---|---|---|
| `Python/Phase1_Kinematics/Python_MDH` | `VALIDATION` | FR5 MDH parameter, FK solver, pose debug | `DO_NOT_DELETE` |
| `Python/Phase1_Kinematics/GroundTruth` | `VALIDATION` | Ground Truth testcase loader/runner/output writer | `DO_NOT_DELETE` |
| `Python/Phase1_Kinematics/Common` | `SUPPORT` | transform utilities and shared math helpers | `DO_NOT_DELETE` |
| `Python/Phase2_Unity/Compare` | `VALIDATION` / `DEBUG` | Unity result and Python result compare area | Keep for now |
| `Python/Phase2_Unity/Interface` | `SUPPORT` | Unity/Python interface candidate | Keep for now |
| `Python/Phase2_Unity/Output` | `SUPPORT` / `VALIDATION` | Generated output or validation artifacts | Keep for now |
| `Python/Docs` | `SUPPORT` | Project history, ROS2/Gazebo/Unity architecture documents | `DO_NOT_DELETE` |

## Role Summary

### `Python/Phase1_Kinematics/Python_MDH`

FR5 kinematics의 기준 계산 계층입니다. Unity C# FK와 비교할 때 기준값 역할을 하므로 삭제하지 않습니다.

### `Python/Phase1_Kinematics/GroundTruth`

Ground Truth testcase를 읽고 실행하고 결과를 쓰는 계층입니다. Unity validation과 포트폴리오 검증 자료에 연결될 수 있으므로 삭제하지 않습니다.

### `Python/Phase1_Kinematics/Common`

공통 변환 수학 helper 계층입니다. 여러 Python validation script가 의존할 수 있습니다.

### `Python/Phase2_Unity`

Unity와 Python 검증을 이어주는 비교/인터페이스/output 후보입니다. 현재 직접 runtime active flow가 아니더라도 validation/debug 용도이므로 유지합니다.

### `Python/Docs`

ROS2, Gazebo, Unity, Bridge, validation 작업 이력과 설계 판단을 담고 있습니다. 실제 code source가 외부 workspace에 있을 때도 문서가 유일한 맵 역할을 할 수 있으므로 삭제하지 않습니다.

## Notes

- `__pycache__`는 일반적으로 Python cache지만, 이번 문서화 단계에서는 삭제하지 않습니다.
- Ground Truth와 MDH 코드는 Unity runtime과 분리되어 있어도 기준 검증 계층입니다.
- Python output은 재생성 가능할 수 있지만, 비교 기준 자료일 수 있으므로 확인 전 삭제하지 않습니다.
