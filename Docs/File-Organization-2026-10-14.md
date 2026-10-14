# 파일 정리 내역 — 2026-10-14

공통 코드는 `Assets/Script`, 스테이지 전용 코드는 `Assets/Stage1`, `Assets/Stage2`, `Assets/Stage3` 아래에서 관리한다. 런타임 코드는 `Scripts`, 편집·생성·검증 도구는 `Editor`에 둔다. 기존 JSON 정리 위치 `Assets/notedata JSON`을 그대로 사용한다.

## 이동한 파일 전체

아래 모든 이동은 Unity AssetDatabase를 사용했고, 원래 `.meta`와 GUID를 유지했다. 클래스명과 어셈블리는 변경하지 않았다.

| 기존 경로 | 새 경로 |
| --- | --- |
| `Assets/Script/Stage1CameraDirector.cs` | `Assets/Stage1/Scripts/Presentation/Stage1CameraDirector.cs` |
| `Assets/Script/Stage1Switchback.cs` | `Assets/Stage1/Scripts/Event/Stage1Switchback.cs` |
| `Assets/Script/StageJumpSetPiece.cs` | `Assets/Stage1/Scripts/Event/StageJumpSetPiece.cs` |
| `Assets/Editor/Stage1RhythmBuilder.cs` | `Assets/Stage1/Editor/Build/Stage1RhythmBuilder.cs` |
| `Assets/Editor/Stage1MapPresentationBuilder.cs` | `Assets/Stage1/Editor/Build/Stage1MapPresentationBuilder.cs` |
| `Assets/Editor/Stage1RhythmAcceptance.cs` | `Assets/Stage1/Editor/Validation/Stage1RhythmAcceptance.cs` |
| `Assets/Stage1Presentation/Shape.png` | `Assets/Stage1/Art/Shape.png` |
| `Assets/Stage1Presentation/Lines.mat` | `Assets/Stage1/Art/Lines.mat` |
| `Assets/Stage2/script/BackgroundLoop.cs` | `Assets/Stage2/Scripts/BackgroundLoop.cs` |
| `Assets/Stage2/script/BackgroundTrigger.cs` | `Assets/Stage2/Scripts/BackgroundTrigger.cs` |
| `Assets/Stage2/script/Camera Test.cs` | `Assets/Stage2/Scripts/Camera Test.cs` |

`Assets/Stage1Presentation` 폴더 자체를 `Assets/Stage1/Art`로, `Assets/Stage2/script` 폴더 자체를 `Assets/Stage2/Scripts`로 이동해 폴더 GUID도 유지했다. 스크립트 9개와 아트 에셋 2개를 이동했다.

추가된 폴더는 `Assets/Stage1`, `Scripts`, `Scripts/Event`, `Scripts/Presentation`, `Editor`, `Editor/Build`, `Editor/Validation`이며 각 폴더의 `.meta`가 생성됐다. 이동된 기존 경로가 Git에서 `D`로 표시되더라도 대응하는 새 위치에 파일이 있으므로 실제 데이터 삭제가 아니다.

## 경로를 수정한 파일 전체

| 파일 | 변경 내용 |
| --- | --- |
| `Assets/Stage1/Editor/Build/Stage1RhythmBuilder.cs` | Stage1Notes·Stage1Timing 읽기 경로와 씬에 설정하는 JSON 경로를 `Assets/notedata JSON`에 맞춤 |
| `Assets/Stage1/Editor/Build/Stage1MapPresentationBuilder.cs` | 아트 생성 위치를 `Assets/Stage1/Art`로 변경 |
| `Tools/build_stage1_chart.py` | 출력 위치를 `Assets/Charts`에서 `Assets/notedata JSON`으로 변경. 재실행 시 정리한 기존 JSON에 기록 |
| `Assets/Script/CreateNote.cs` | 기본 저장 경로를 `notedata JSON/notedata.json`으로 변경. 원래 주석 바이트 유지 |
| `Assets/Script/NoteMapPlacer.cs` | 기본 읽기 경로를 `notedata JSON/notedata.json`으로 변경 |
| `Assets/Scenes/Stage1.unity` | Stage1Notes를 가리키는 경로 2개와 CreateNote 저장 경로 1개 수정 |
| `Assets/Scenes/Stage2.unity` | notedata·notedata_Stage2 읽기 경로 2개와 CreateNote 저장 경로 1개 수정 |
| `Assets/Scenes/SampleScene.unity` | CreateNote 저장 경로 1개 수정 |
| `Docs/Stage1-Rhythm.md` | 채보 파일 경로를 현재 위치로 갱신 |
| `Docs/File-Organization-2026-10-14.md` | 이번 정리 내역과 삭제 승인 후보를 기록 |

JSON의 노트 내용, 채보 수, 타이밍, 씬의 TextAsset GUID는 변경하지 않았다. 채보 생성 도구도 실행하지 않았다.

## 공통 파일 분류 근거

- `Assets/Script/Event`의 `CaveEventManager`, `EventNoteJudge`, `EventNoteSpawner`, `EventTrigger`, `FallTransitionEffect`, `FixedEventManager`는 Stage1·Stage2 씬에서 모두 참조하므로 공통 위치에 유지한다.
- 판정, 노트, 음악 시계, 이동 경로, 플레이어 이동·애니메이션, 일시정지, 결과 UI는 공통 코드다. 런타임에 생성되는 컴포넌트와 프리팹 참조도 있어 씬에 직접 없다는 이유만으로 삭제하지 않는다.
- `StageJumpSetPiece`는 현재 Stage1에서만 쓰는 시소·발사 연출이다. 공통 `JumpTrigger`가 이 타입을 참조하지만 타입과 어셈블리를 유지하므로 폴더 이동으로 참조가 끊어지지 않는다.
- `BackgroundFollowCamera`는 카메라를 따라가는 범용 기능이므로 공통 Player 폴더에 유지한다.
- Stage3의 Gameplay·Event·Presentation·Data·Editor 분류는 이미 정리되어 있다.
- `Assets/Editor/UnityMcpSetup.cs`, `UnityMcpReconnect.cs`는 프로젝트 공통 에디터 도구다.
- `notedata.json`, `notedata_Stage2.json`, `Stage1Notes.json`, `Stage1Timing.json`, Stage3의 `LastWishChart.json`은 실제 사용처가 있으므로 삭제 후보가 아니다. `notedata_cave.json`도 이번 작업에서 삭제하지 않았다.

## 사용자 승인 후 삭제 완료

| 파일 | 삭제 후보 이유 |
| --- | --- |
| `Assets/Stage3/Scripts/Legacy/Stage3Director.cs` | 현재 Stage3Gameplay로 대체된 이전 독립 플레이 구현 |
| `Assets/Stage3/Scripts/Legacy/Stage3Session.cs` | Stage3Director와 구버전 검증에서만 쓰는 이전 판정 세션 |
| `Assets/Stage3/Editor/Validation/Legacy/Stage3Validation.cs` | 위 두 구버전 타입에 의존하는 이전 검증 도구 |
| `Assets/Stage3/Editor/Validation/Legacy/Stage3PlayValidation.cs` | Stage3Director를 찾는 이전 재생 검증 도구 |

위 4개는 Assets의 씬·프리팹·에셋에서 스크립트 GUID 참조가 0건이고, 코드 의존성도 4개 내부에만 있었다. 사용자가 삭제를 승인해 Unity AssetDatabase로 스크립트 4개, 대응 `.meta` 4개, `Scripts/Legacy`와 `Editor/Validation/Legacy` 폴더 및 폴더 `.meta` 2개를 삭제했다. `Assets/Stage3/README.md`에서도 구버전 보관 안내를 제거했다. 구버전 독립 플레이·판정 구현과 전용 검증 호출은 제거했으며, 현재 `Stage3Acceptance` 검증 도구는 유지한다.

Reports의 과거 씬·채보 백업은 복구 자료라 이번 삭제 승인 목록에 포함하지 않았다.

## 검증

- 이동 전 기록한 226개 파일과 비교: 219개는 바이트가 같고, 7개는 의도한 경로 수정 파일이며, 누락된 파일은 0개다.
- 이동한 스크립트·아트 에셋의 `.meta`, JSON 채보 내용은 모두 원본과 같다.
- 씬에 저장된 모든 `jsonFileName` 경로가 실제 Assets 파일을 가리킨다.
- Assets·Docs·Tools의 현재 코드와 문서에서 예전 Charts·Stage1Presentation·Stage2/script 경로 참조를 제거했다.
- Python 채보 생성 스크립트 구문 검사 통과. 재생성은 실행하지 않았다.
- Unity Editor.log의 최신 자동 컴파일 구간에서 어셈블리 재로드 성공을 확인했다. 해당 구간에 C# 컴파일 오류는 0건이며 `git diff --check`도 통과했다.
- 최초 정리 단계에서는 Unity 강제 새로고침 요청의 자동 승인 검토가 사용량 제한으로 실패했고, 재개 당시 MCP 도구도 제공되지 않았다. 이후 삭제 승인 단계에서는 MCP로 삭제와 새로고침·재컴파일을 완료했다.
- 삭제 후 Unity 컴파일 종료와 C# 컴파일 오류 로그 0건을 확인했다. Stage3 씬을 임시로 추가 로드해 누락된 스크립트 0개, Stage3Gameplay 및 음악·채보·판정·전환·무대 참조 정상, 일반 노트 279개와 연타 이벤트 3개, Legacy 의존성 0개를 확인했다. 검사 후 임시 로드한 씬을 닫고 원래 Stage1을 활성 상태로 복원했다. 실제 전곡 재생 검증은 이번 삭제 작업에서 실행하지 않았다.

재개 당시 이미지 `.meta` 16개에 WebGL·Android 임포터 플랫폼 항목 변경이 별도로 존재했다. 이번 파일 분류·경로 수정에서는 이 파일들을 직접 편집하지 않았으며, 소유가 불명확한 변경을 임의로 되돌리지 않았다. 기존 미추적 `.unity-mcp/source`도 정리 대상에서 제외했다.
