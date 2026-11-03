# Hierarchy에서 수정하는 화면 구조

## Lobby

`Unity/Assets/Scenes/Lobby.unity`를 열면 `Start Camera`, `EventSystem`, `Start Screen UI`가 있습니다. `Start Screen UI`는 `Assets/Prefabs/StartScreen.prefab`의 인스턴스입니다.

- `Cafe illustration`: `title-cafe.png` 배경. Image의 Source Image를 바꿀 수 있습니다.
- 제목·설명·메뉴 버튼: RectTransform과 Text로 직접 편집합니다.
- `Help`: 미리 만들어 둔 비활성 도움말 패널. 편집 시 활성화하면 내용을 볼 수 있습니다.
- 루트의 `StartScreen`: 버튼 3개, 도움말, 닫기 버튼, 게임 씬 참조. 화면을 생성하지 않습니다.

## InGame

```text
InGame
├─ Cafe World                         [CafeWorld.prefab]
│  ├─ Floor Plan                      공통 바닥·벽·문·방 이름
│  ├─ Security Room                   감시실 가구·문서·CCTV
│  ├─ Lobby
│  ├─ Cafe Hall                       재료·컵·커피 효과·가구
│  ├─ Storage
│  ├─ Night Room
│  └─ Player                          [NightPlayer.prefab]
│     ├─ Head
│     └─ Flashlight Pivot
│        └─ Flashlight
├─ Night Shift UI                     [NightGameUI.prefab]
│  ├─ Shift Record                    목표·상태·진행·규칙서 버튼
│  ├─ Header                          타이머·현재 방·일시정지
│  ├─ Inventory                       재료 아이콘·슬롯
│  ├─ Interaction Feedback             조사 안내·메시지·범람 경고
│  └─ Overlays
│     ├─ Rules Overlay
│     ├─ Reading Overlay
│     ├─ CCTV Overlay
│     ├─ Pause Overlay
│     ├─ Failure Overlay
│     └─ Complete Overlay
├─ Night Shift Controller             NightGame·튜닝·씬 참조
├─ Cameras
│  ├─ Map Camera
│  └─ CCTV Camera
└─ EventSystem                        프로젝트의 UI Input Actions 참조
```

## 수정 방법

가구·문·재료는 월드 프리팹에서 이동하거나 SpriteRenderer의 Sprite를 바꿉니다. 가구와 벽의 충돌 범위는 BoxCollider2D에서 따로 수정합니다. `CafeInteractable`의 종류·제목·내용·재료 종류는 Inspector에서 편집할 수 있습니다. 상호작용 대상을 추가하면 월드 루트 `CafeWorld.Interactables` 목록에도 등록합니다.

HUD의 동적 글자는 `NightGameView` 필드 이름으로 연결되어 있습니다. 오브젝트 이름을 바꾸더라도 참조는 유지됩니다. 패널은 삭제하지 않고 켜고 끄므로, 문서를 여러 번 열어도 Hierarchy가 늘어나지 않습니다. 팝업을 편집할 때는 `Overlays` 아래 해당 패널을 활성화하세요. 게임 시작 시 모두 닫힙니다.

`CafeButtonStyle`의 Idle/Selected Sprite를 교체하면 메뉴·CCTV·재료 슬롯의 선택 이미지를 바꿀 수 있습니다. UI Image는 9-slice를 사용하고, 픽셀 아트는 Point 필터와 무압축으로 가져옵니다. 루트의 `CafeFont.Override Font`에서 한글 글꼴을 바꿀 수 있습니다.

기존 1일차 상태·재료 판정은 `ShiftSession`, 시간·이동·레시피 설정은 `Assets/Data/DayOneTuning.asset`에 유지됩니다. 규칙서·CCTV·메모 중 시간 진행, Esc 일시정지, 오답 페널티, 재도전 시 지식 유지 규칙도 유지했습니다.

## 아트 제작

`Tools/create_pixel_art.py`를 Python + Pillow로 실행하면 `Unity/Assets/Art/NightCafe`에 원본 PNG를 재현합니다. 고정 난수를 쓰는 직접 제작 픽셀 아트이며, 이미지 모델·OpenAI API·로그인 토큰을 사용하지 않습니다. 현재 세션에서는 내장 이미지 생성 도구를 사용할 수 없었고 별도 API 키도 없었습니다.

이미지 모델에 전달할 미술 방향은 다음과 같습니다. 이번 PNG 제작에서는 이 프롬프트로 API를 호출하지 않았습니다.

> 어두운 야간 카페의 2D 픽셀 아트. 청록색과 잿빛 목재, 오래된 종이와 은은한 호박색 조명. 비 오는 창가와 카운터 위에 떠 있는 커피 컵. 시작 화면은 왼쪽에 제목과 메뉴를 위한 여백, 오른쪽에 카페와 이상현상. 맵용 에셋은 기존 탑다운 시점의 가구·CCTV 단말기·문서·우유·초콜릿·아몬드·타피오카·크림·플레이어. 개별 스프라이트는 투명 배경, 텍스트와 워터마크 없이 제작.

나눔고딕은 Google Fonts의 Nanum Gothic 배포본을 사용합니다. 원본: https://github.com/google/fonts/tree/main/ofl/nanumgothic . OFL 라이선스 파일을 함께 포함했습니다.
