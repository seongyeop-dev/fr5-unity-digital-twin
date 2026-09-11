# 07. 프로젝트 구조

## Unity 저장소

```text
FR5_UNITY_DIGITAL_TWIN/
├─ Assets/
├─ Packages/
├─ ProjectSettings/
├─ README.md
└─ docs/
```

Unity 생성 폴더는 Git에서 제외합니다.

```text
Library/
Temp/
Logs/
obj/
Build/
Builds/
UserSettings/
```

## 주요 Unity 영역

```text
Assets/
├─ Model/
└─ Project/
   ├─ Art/
   ├─ Editor/
   ├─ Prefabs/
   ├─ Scenes/
   └─ Scripts/
```

## 기준 Scene

```text
Assets/Project/Scenes/01_FR_Simulator.unity
```

## Workcell 관련 Script

```text
Assets/Project/Scripts/Workcell/
```

주요 파일:

```text
EquipmentProcessSequenceController.cs
FR5InsertedJigTestTrigger.cs
```

Editor 구성:

```text
Assets/Project/Editor/FR5/FR5SourceFinishSetup.cs
```

## ROS2 Workspace

Unity 저장소와 별도로 관리합니다.

```text
~/fr5_ros2_ws
```

최종 Slot Master:

```text
src/fr5_moveit_config/scripts/slot01_to_slot08_final_one_take.py
```

## Python

프로젝트 내부 Python은 주로 다음 역할입니다.

```text
MDH
FK
Ground Truth
Validation
```

실시간 Robot Runtime은 ROS2 / SDK 계층이 담당합니다.

## 공개 저장소 주의사항

GitHub 공개 전 반드시 확인합니다.

- Unity Asset 재배포 가능 여부
- CAD / STEP / ZIP 라이선스
- Vendor SDK Binary
- Robot IP
- Credential / Token
- 대용량 파일
- 불필요한 Recovery / Backup

## 문서 구조

```text
README.md
docs/
├─ README.md
├─ 01_overview.md
├─ 02_architecture.md
├─ 03_features.md
├─ 04_data_flow.md
├─ 05_validation.md
├─ 06_project_scope.md
├─ 07_project_structure.md
└─ images/
```
