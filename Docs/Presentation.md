# Unity에서 게임 편집하기

Unity 메뉴에서 `Night Cafe → 편집 도구`를 열면 씬, 프리팹, 설정과 C# 파일을 바로 열 수 있습니다. Hierarchy의 오브젝트를 선택한 뒤 Inspector에서 Sprite, 위치, 연결된 컴포넌트를 편집할 수 있습니다.

| 바꿀 것 | 편집 위치 |
| --- | --- |
| 시작 화면과 버튼 | `Lobby → Start Screen UI`. `Navigation`에는 버튼, `Help`에는 조작 안내가 있습니다. |
| 방의 바닥·벽·가구·소품 | `CafeWorld.prefab → 해당 방 → Floor & Walls / Furniture / Interactions / Effects` |
| 주인공 스프라이트 | `NightPlayer.prefab → Appearance`의 Sprite Renderer. 원본에서 추출한 `Assets/Art/NightCafe/Player/human.png`를 사용합니다. |
| 1일차 컵과 커피 | `CafeWorld.prefab → Cafe Hall`. `CafeWorld`의 `Cup Frames`가 컵의 애니메이션 순서입니다. |
| 2~5일차 소품과 괴물 | `InGame → Night Events`. 사건별 부모 아래에 소품과 효과가 들어 있습니다. |
| 문 위치와 도착점 | `InGame → Passages & Doors → Rooms and Passageways → Doors by Room / Arrivals by Room` |
| 지름길 복도 | 같은 `Rooms and Passageways` 아래의 `홀 연결 복도`와 `카운터 연결 복도`에서 벽과 안내판을 편집합니다. `Corridor Hazards`에는 복도 이벤트 오브젝트가 있습니다. |
| 화면과 팝업 | `NightGameUI.prefab`의 `Hand and Equipment HUD`, `Inventory Overlay`, `Interaction Feedback`, `Overlays`. 5일차 조사 및 엔딩 패널은 `InGame → Night Shift UI → Night 05 UI`에 있습니다. |
| 손전등과 감시실 문 조명 | 손전등은 `CafeWorld.prefab → Security Room → Interactions`, 빨간 등은 `InGame → Passages & Doors → Rooms and Passageways → Doors by Room → 로비 → 감시실 문` 아래에 있습니다. |
| CCTV 영상과 평면도 | `Night Shift UI → Overlays → CCTV Overlay`의 `Camera Map`과 `Desk Post-it`을 편집합니다. 채널별 위치와 줌은 `InGame → Cameras → Live Room Cameras` 아래 각 Camera의 Inspector에서 수정합니다. |
| 시간·이동 속도·레시피·암호 | `Assets/Data/DayOneTuning.asset` |

문은 출발하는 방 아래에 있고, 도착 마커는 도착하는 방 아래에 있습니다. 도착점을 움직이면 `NightRoutes`가 해당 마커의 월드 좌표를 사용합니다. 문과 가구를 옮길 때는 도착점이 벽이나 가구에 닿지 않는지 확인하세요. 새로 배치한 상호작용 소품은 해당 밤의 컴포넌트나 `CafeWorld.Interactables`에 등록해야 합니다.

전체 기획서 3.3.4의 개별 도면 표제와 전체 연결도를 기준으로 사용합니다. 월드 1유닛은 주인공 한 칸인 64×64px입니다. 1920×1080 기본 게임 화면에서도 이동 화면 한 칸을 64px로 표시합니다. 문 너비는 기본 2칸입니다. 일부 개별 도면의 격자 외곽 표기와 4일차 문서의 32px 잠정 규격은 별도 안입니다.

| 구역 | 타일 | 설계 픽셀 |
| --- | ---: | ---: |
| 감시실 | 9×7 | 576×448 |
| 로비 | 13×9 | 832×576 |
| 창고·숙직실 | 각 11×8 | 각 704×512 |
| 카운터 | 14×9 | 896×576 |
| 홀 | 19×11 | 1,216×704 |
| 외부 | 17×11 | 1,088×704 |
| 카운터 연결 복도 | 20×6 | 1,280×384 |
| 홀 연결 복도 | 18×8 | 1,152×512 |
| 상부 우회로 | 18×6 | 1,152×384 |
| 하부 우회로 | 18×7 | 1,152×448 |

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

`Assets/Art/maps`에는 방 원본 이미지가 있고, `Assets/Art/MapPieces`에는 그 이미지에서 분리한 바닥, 벽, 창문, 장식 스프라이트가 있습니다. `Assets/Art/furniture`에서 가구 스프라이트를 선택할 수 있습니다. 바닥과 벽은 Inspector의 Sprite Renderer에서 Sprite와 Size를 편집하고, 가구의 물리 범위는 Box Collider 2D에서 확인합니다. 원본에서 스프라이트를 다시 추출하려면 `Tools/slice_map_art.py`의 잘라낼 범위를 편집한 후 실행합니다.

일상 편집은 현재 씬과 프리팹을 열어 작업합니다. `Rebuild Authored Scenes`는 초기 제작용으로, 실행하면 배치와 UI를 다시 생성합니다.
