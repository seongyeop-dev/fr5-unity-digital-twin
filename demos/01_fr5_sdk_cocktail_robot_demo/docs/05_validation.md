# 05. 검증 결과

## 검증 환경

- 실제 FR5 협동로봇
- 전동 그리퍼
- 교육과정 칵테일 제조 Workcell
- Lua 제어 프로그램과 포인트 DB
- 메뉴 1·메뉴 2·Pick & Place 시연 영상

## 검증 결과

| 영역 | 검증 항목 | 결과 |
|:---|:---|:---:|
| 프로젝트 실행 | 실제 FR5에서 Lua 프로그램 실행 | PASS |
| Pick & Place | 접근·파지·이송·배치·Home 복귀 | PASS |
| 메뉴 1 | DI 0 입력과 펌프 위치 1~3 순차 방문 | PASS |
| 메뉴 2 | DI 1 입력과 펌프 위치 4~6 순차 방문 | PASS |
| 모션 | PTP·LIN·Spiral 동작 구분 | PASS |
| 그리퍼 | 컵·뚜껑 파지와 해제 | PASS |
| 반복 준비 | 작업 완료 후 Home 복귀와 입력 대기 | PASS |
| 공개 근거 | 영상 3개·이미지 6개·Lua 2개·포인트 DB | PASS |

## 확인 근거

- `src/pickplace_demo.lua`
- `src/cocktail_menu_demo.lua`
- `data/web_point.db`
- [실제 시연 영상](demo/README.md)
- 메뉴별 작업 순서 이미지

정량적인 반복 성공률, 사이클타임과 위치 오차 측정값은 기록하지 않았으므로 검증 결과에 임의의 수치를 추가하지 않았습니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
