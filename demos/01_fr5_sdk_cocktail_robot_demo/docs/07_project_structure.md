# 07. 프로젝트 구조

## 저장소 구조

```text
fr5_cocktail_robot_demo/
├─ README.md
├─ .gitattributes
├─ .gitignore
├─ src/
│  ├─ cocktail_menu_demo.lua
│  └─ pickplace_demo.lua
├─ data/
│  └─ web_point.db
├─ media/
│  └─ videos/
│     ├─ cocktail_menu_1_demo.mp4
│     ├─ cocktail_menu_2_demo.mp4
│     └─ pickplace_demo.mp4
├─ docs/
│  ├─ README.md
│  ├─ 01_overview.md
│  ├─ 02_architecture.md
│  ├─ 03_features.md
│  ├─ 04_data_flow.md
│  ├─ 05_validation.md
│  ├─ 06_project_scope.md
│  ├─ 07_project_structure.md
│  ├─ demo/
│  ├─ images/
│  └─ reference/
├─ archive/
└─ references/
```

## 주요 파일

| 경로 | 역할 |
|:---|:---|
| `src/pickplace_demo.lua` | 개인 Pick & Place 제어 순서 |
| `src/cocktail_menu_demo.lua` | 팀 통합 메뉴 분기·제조 순서 |
| `data/web_point.db` | 작업 포인트와 관절·TCP 정보 |
| `media/videos/` | 실제 FR5 시연 영상 3개 |
| `docs/images/` | 대표·시연·작업 순서 이미지 6개 |
| `archive/` | 원본 FR5 프로그램 보관 자료 |
| `references/` | 외부 참고 링크와 출처 설명 |
| `docs/reference/` | 모션 순서 원문과 배선 참고 자료 |

## 소스 구성

이 저장소는 전체 Unity 프로젝트가 아니라 실제 FR5 제어 데모의 공개 가능한 결과를 모은 저장소입니다. Scene, Prefab, `ProjectSettings`는 포함하지 않으며 Lua 제어 코드, 포인트 DB와 시연 자료를 중심으로 구성합니다.

## 포인트 DB

`web_point.db`의 `points` 테이블에는 23개 컬럼과 107개 레코드가 있습니다.

- 이름·속도·가속도
- Tool·Workpiece 번호
- 관절값 `j1~j6`
- 외부축 `E1~E4`
- TCP 좌표 `x·y·z·rx·ry·rz`

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
