# Unity에서 게임 편집하기

Unity 메뉴에서 `Night Cafe → 편집 도구`를 열면 씬, 프리팹, 설정과 C# 파일을 바로 열 수 있습니다. Hierarchy의 오브젝트를 선택한 뒤 Inspector에서 Sprite, 위치, 연결된 컴포넌트를 편집할 수 있습니다.

| 바꿀 것 | 편집 위치 |
| --- | --- |
| 시작 화면과 버튼 | `Lobby → Start Screen UI`. `Navigation`에는 버튼, `Help`에는 조작 안내가 있습니다. |
| 방의 바닥·벽·가구·소품 | `CafeWorld.prefab → 해당 방 → Floor & Walls / Furniture / Interactions / Effects` |
| 1일차 컵과 커피 | `CafeWorld.prefab → Cafe Hall`. `CafeWorld`의 `Cup Frames`가 컵의 애니메이션 순서입니다. |
| 2~5일차 소품과 괴물 | `InGame → Night Events`. 사건별 부모 아래에 소품과 효과가 들어 있습니다. |
| 문 위치와 도착점 | `InGame → Passages & Doors → Rooms and Passageways → Doors by Room / Arrivals by Room` |
| 지름길 복도 | 같은 `Rooms and Passageways` 아래의 `홀 연결 복도`와 `카운터 연결 복도`에서 벽과 안내판을 편집합니다. `Corridor Hazards`에는 복도 이벤트 오브젝트가 있습니다. |
| 화면과 팝업 | `NightGameUI.prefab`의 `Header`, `Shift Record`, `Inventory`, `Interaction Feedback`, `Overlays`. 5일차 조사 및 엔딩 패널은 `InGame → Night Shift UI → Night 05 UI`에 있습니다. |
| CCTV 영상과 줄무늬 | `Night Shift UI → Overlays → CCTV Overlay`에서 영상 UI를 편집합니다. 채널별 기본 구도는 `CafeWorld.ViewCctv`와 `NightGame.Campaign.ViewShiftCamera`에서 정합니다. |
| 시간·이동 속도·레시피·암호 | `Assets/Data/DayOneTuning.asset` |

문은 출발하는 방 아래에 있고, 도착 마커는 도착하는 방 아래에 있습니다. 도착점을 움직이면 `NightRoutes`가 해당 마커의 월드 좌표를 사용합니다. 문과 가구를 옮길 때는 도착점이 벽이나 가구에 닿지 않는지 확인하세요. 새로 배치한 상호작용 소품은 해당 밤의 컴포넌트나 `CafeWorld.Interactables`에 등록해야 합니다.

| 스크립트 폴더 | 편집 범위 |
| --- | --- |
| `Assets/Scripts/Character` | 플레이어 이동과 따라오는 카메라 |
| `Assets/Scripts/Gameplay/Campaign` | 밤 전환, 상호작용, 추격과 조사 진행 |
| `Assets/Scripts/Gameplay/Rules` | 밤별 판정, 퍼즐, 조정값 |
| `Assets/Scripts/World` | 맵, 문, 복도, 배치된 소품의 참조 |
| `Assets/Scripts/Presentation` | 시작 화면, HUD, 버튼 스타일 |
| `Assets/Scripts/Diagnostics` | 실행 검증 |
| `Assets/Editor` | 편집 도구, 씬 제작, 빌드 |

아트는 짙은 남색과 바랜 청회색을 바탕으로 녹슨 붉은색을 강조점에 사용합니다. 픽셀 원본은 `Assets/Art/NightCafe`, 컵의 GIF 프레임은 그 안의 `FloatingCup`에 있습니다. `Tools/create_pixel_art.py`는 카페 배경·가구·소품·UI의 기본 PNG를 다시 생성합니다. 직접 수정한 PNG를 보존하려면 필요한 파일만 교체하세요.

일상 편집은 현재 씬과 프리팹을 열어 작업합니다. `Rebuild Authored Scenes`는 초기 제작용으로, 실행하면 배치와 UI를 다시 생성합니다.
