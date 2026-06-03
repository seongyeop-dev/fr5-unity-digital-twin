# FR5 Legacy Candidates Map

이 문서는 legacy 후보를 삭제 대상이 아니라 확인 필요 후보로 표시하기 위한 문서입니다. 이름만 보고 삭제하지 않습니다.

## Common Tags

- `ACTIVE`
- `SUPPORT`
- `VALIDATION`
- `DEBUG`
- `STANDBY`
- `LEGACY_CANDIDATE`
- `DO_NOT_DELETE`

## Important Rule

`LEGACY_CANDIDATE`는 삭제해도 된다는 뜻이 아닙니다. 현재 active flow에서 직접 보이지 않거나 이름상 과거/비활성/복구 후보로 보인다는 뜻입니다. 삭제 전에는 Scene 참조, Prefab 참조, `.meta` GUID, Git history, 실행 테스트를 확인해야 합니다.

## Candidate List

| Candidate | Status | Why Not Delete Immediately |
|---|---|---|
| `Unity/FAIRINO_FR5_DigitalTwin/Assets/_Recovery` | `LEGACY_CANDIDATE` | Unity recovery Scene은 복구 기준점일 수 있음 |
| `Unity/FAIRINO_FR5_DigitalTwin/Assets/TextMesh Pro/Examples & Extras_disabled` | `LEGACY_CANDIDATE` | disabled 이름이어도 TMP asset/material/font 참조 가능성 확인 필요 |
| `Unity/FAIRINO_FR5_DigitalTwin/Assets/Project/Scripts/New` | `LEGACY_CANDIDATE` | 이전 staging 후보지만 Scene/Prefab 참조 가능성 확인 필요 |
| `Dummy` 이름 포함 파일/폴더 | `LEGACY_CANDIDATE` | dummy prefab/script가 validation 또는 placeholder로 쓰일 수 있음 |
| `Old` 이름 포함 파일/폴더 | `LEGACY_CANDIDATE` | 이전 기준 자료일 수 있음 |
| `Disabled` / `disabled` 이름 포함 파일/폴더 | `LEGACY_CANDIDATE` | 비활성 보관 또는 참조 보존 목적일 수 있음 |
| `*.blend1` | `LEGACY_CANDIDATE` | Blender backup이지만 model 복구/비교에 필요할 수 있음 |
| `build/install/log` | `LEGACY_CANDIDATE` | ROS2/colcon 생성 산출물이지만 실행 재현성 확인 전 삭제 금지 |
| `BridgeTools/**/obj` | `LEGACY_CANDIDATE` | C# build intermediate지만 Bridge 재빌드/참조 확인 전 삭제 금지 |
| `ThirdParty/**/obj` | `LEGACY_CANDIDATE` | 외부 SDK build output이며 DLL/source 관계 확인 필요 |
| `BridgeTools/**/bin` | `STANDBY` / `DO_NOT_DELETE` | 실행 파일과 DLL이 실제 bridge 실행에 필요할 수 있음 |
| `ThirdParty/**/bin` | `SUPPORT` / `DO_NOT_DELETE` | SDK DLL과 예제 실행 파일이 의존성 확인에 필요할 수 있음 |
| `Unity/FAIRINO_FR5_DigitalTwin/Library` | `LEGACY_CANDIDATE` | Unity generated cache지만 현재 로컬 editor 상태와 관련 있음 |
| `Unity/FAIRINO_FR5_DigitalTwin/Temp` | `LEGACY_CANDIDATE` | Unity temp output이지만 editor 상태 확인 전 삭제하지 않음 |
| `Unity/FAIRINO_FR5_DigitalTwin/Logs` | `DEBUG` | troubleshooting에 필요할 수 있음 |

## Required Checks Before Any Cleanup

1. `git status --short --untracked-files=all`
2. Unity Editor compile error 확인
3. Scene `01_FR_Simulator.unity` Missing Script 확인
4. Prefab reference 확인
5. `.meta` GUID 유지 여부 확인
6. ROS2 workspace `build/install/log` 재생성 가능 여부 확인
7. BridgeTools executable/DLL 실행 의존성 확인
8. ThirdParty SDK DLL/source 의존성 확인

## Current Recommendation

- 지금은 삭제하지 않습니다.
- 문서상으로만 `ACTIVE`, `STANDBY`, `LEGACY_CANDIDATE`, `DO_NOT_DELETE`를 구분합니다.
- 실제 이동/삭제는 별도 검증 단계에서만 진행합니다.
