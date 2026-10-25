# Stage2 ESC 메뉴와 HUD 복구

Stage2에는 Stage1과 같은 체력·콤보·점수 TMP 텍스트와 공통 PauseManager, PauseMenuEffect, 메뉴 버튼 이벤트가 이미 있었다. 기존 UI를 유지하고 표시·입력 설정을 복구했다.

## 변경

- Stage1의 EventSystem과 StandaloneInputModule 구성을 Stage2에 추가했다. 계속하기·재시도·곡 선택·타이틀 버튼의 기존 PauseManager 연결을 유지했다.
- 화면 공간 카메라 모드인 루트 Canvas의 비어 있던 카메라 참조를 Stage2 Main Camera로 연결했다. 루트 RectTransform의 0 스케일을 1로 복구했다.
- Stage1과 동일한 위치·크기·글꼴을 가진 기존 체력·콤보·점수 UI를 사용한다. 판정 매니저가 각 텍스트를 갱신하는 참조도 유지했다.
- 배경이 포함된 루트 Canvas의 정렬 순서 -10은 유지하고, HUD에 정렬 순서 100, ESC 메뉴에 200, 전환용 검은 페이드에 300인 하위 Canvas를 추가했다. 각 하위 Canvas에 GraphicRaycaster를 연결했다.
- CanvasScaler를 1920×1080 기준 화면 크기 비례 모드로 변경해 다른 Game 뷰 크기에서도 기존 배치를 유지하도록 했다.
- 체력은 기존 Stage1과 같은 HP 숫자 표시다. 콤보가 0일 때 공통 JudgeManager가 콤보 텍스트를 비우는 기존 동작도 유지한다.

## 확인

- 체력·콤보·점수 텍스트의 TMP 설정이 Stage1과 일치한다.
- PauseManager가 활성 상태이며 메뉴와 열기·닫기 효과 참조가 연결되어 있다.
- 메뉴 버튼 4개의 계속하기·재시도·곡 선택·타이틀 호출이 유지되어 있다.
- 씬의 로컬 fileID 참조가 모두 존재하며 식별자 중복이 없다. EventSystem은 1개다.
- git diff --check 통과.
- 현재 이 채팅에는 Unity MCP 도구가 제공되지 않아 실제 Play 모드의 표시·ESC 입력·버튼 동작은 검증하지 못했다. 파일 기준 복구와 연결 검사를 완료한 상태다.

씬 수정 외 공통 스크립트·채보·Stage1은 변경하지 않았다. Stage2에 이미 있던 BGM 볼륨 0.45 등 다른 작업의 변경도 유지했다.
