# Stage3 폴더 안내

현재 Stage3 씬은 `Stage3Gameplay`와 공통 `Assets/Script`의 판정·일시정지·결과 UI를 사용합니다.

| 폴더 | 역할 / 주요 파일 |
| --- | --- |
| `Scripts/Gameplay` | `Stage3Gameplay`: 노트 생성, 음악 진행, 플레이어·카메라 갱신. `Stage3Route`: 이동 경로, 점프, 막 전환 시각. |
| `Scripts/Event` | `Stage3Transitions`: 붕괴·리프트·피날레 전환. `Stage3MashGate`: 연타 이벤트 판정과 표시. |
| `Scripts/Presentation` | `Stage3Scenography`: 배경과 무대 오브젝트 연출. `Stage3Drawing`: 선·링·사각형 생성 도우미. |
| `Scripts/Data` | `Stage3Chart`: JSON 채보와 노트 데이터 형식. |
| `Scripts/Legacy` | `Stage3Director`, `Stage3Session`: 이전 독립 플레이·판정 구현. 현재 Stage3 씬에서는 사용하지 않으며 구버전 검증용으로 보관. |
| `Editor/Build` | `Stage3SceneBuilder`: 씬 구성. `Stage3SceneryBuilder`: 배경과 무대 구성. |
| `Editor/Validation` | `Stage3Acceptance`: 현재 Stage3 전체 재생·일시정지·결과 검증. |
| `Editor/Validation/Legacy` | 이전 독립 구현의 검증 도구. 현재 씬 검증에는 상위 폴더의 `Stage3Acceptance` 사용. |
| `Data` | 실제 채보 JSON 및 곡 선택 데이터. |
| `Art` / `Audio` | 이미지·머티리얼 / 음악 에셋. |

결과창 진입은 공통 `JudgeManager` → `Assets/Script/Result/ResultTransition.cs`를 사용합니다. 음악과 마지막 판정이 끝나면 0.5초 페이드 후 0.35초 암전을 유지합니다.

스크립트 이동 시 `.meta`를 함께 보존합니다. 폴더별로 어셈블리나 네임스페이스를 변경하지 않습니다.
