# 01. 프로젝트 개요

## 개발 배경

FR5 Unity Digital Twin 개발에 앞서 실제 FR5 장비에서 포인트 이동, 그리퍼 동작, 메뉴 분기와 칵테일 제조 순서를 확인하기 위한 중간 시연 프로젝트를 진행했습니다.

## 프로젝트 목적

- 실제 FR5의 PTP·LIN·Spiral 모션 구분
- 전동 그리퍼를 이용한 컵·뚜껑 파지와 해제
- DI 입력에 따른 메뉴 1·2 분기
- 메뉴별 작업 위치의 순차 실행
- Pick & Place와 칵테일 제조 흐름 검증

## 개인 담당

- `pickplace_KSY` 포인트 작성
- `src/pickplace_demo.lua` 제어 순서 작성
- 실제 FR5 접근·파지·이송·배치 테스트
- 팀 통합 시연 참여와 공개 자료 정리

## 팀 통합 결과

`src/cocktail_menu_demo.lua`에서 DI 0·DI 1 입력을 구분하고, 메뉴별 펌프 위치를 순차 방문한 뒤 컵·뚜껑 작업, Spiral 혼합과 Home 복귀까지 수행했습니다.

## 개발 단계

```text
Pick & Place 포인트 작성
→ 개인 Lua 시퀀스 검증
→ 메뉴별 작업 위치 통합
→ DI 입력 분기
→ 실제 FR5 시연
→ 영상·이미지·문서 정리
```

## 최종 결과

실제 FR5에서 Pick & Place, 메뉴 1, 메뉴 2 시연을 완료했습니다. 이 결과는 진행 중인 FR5 Unity Digital Twin에서 실제 로봇 모션과 작업 순서를 이해하기 위한 선행 검증 자료로 활용됩니다.

---

[문서 목차](README.md) · [프로젝트 README](../README.md)
