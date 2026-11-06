# 3D 화면과 편집 위치

게임의 두 씬(Lobby, InGame), 방 배치, 조사 대상 위치, UI 메뉴와 1일차 진행 규칙을 유지한 3D 버전입니다. 기존 평면도의 X는 3D의 X, Y는 3D의 Z에 대응합니다. 사건의 시간·페널티·재료 순서는 기존 `DayOneTuning.asset`과 `ShiftSession`을 그대로 사용합니다.

## InGame

- `Cafe World` (`Assets/Prefabs/CafeWorld.prefab`): 기존 Floor Plan / Security Room / Lobby / Cafe Hall / Storage / Night Room / Player 구조를 유지합니다. 각 사물의 하위 모델은 메시·재질로 구성됩니다.
- `Cameras/Map Camera`: 원근 추적 카메라. 플레이어에서 `(0, 11, -9)`만큼 떨어져 바라봅니다. FOV 48도이며 북쪽이 화면 위에 오므로 기존 WASD와 방향키를 그대로 사용합니다. `PlayerFollowCamera`의 Offset, Look Height에서 조정합니다.
- `Cameras/CCTV Camera`: 일반 카메라와 분리된 촬영용 원근 카메라. 월드의 `CCTV Mounts` 아래 네 렌즈 위치·방향 중 선택한 위치에서 촬영합니다. 1280×720 RenderTexture → 기존 RawImage로 영상이 전달됩니다. 일반 플레이어 카메라는 전환하지 않습니다.
- `Night Shift UI`: 기존 HUD, 재료 슬롯, 문서, 규칙서, CCTV 채널, 일시정지, 실패·재도전, 완료 패널을 유지합니다.
- `Night Shift Controller`: 기존 NightGame과 튜닝 참조. 월드 표현과 물리 질의만 3D로 바뀌었습니다.
- `Screen Background Camera`: 기존처럼 부분 Viewport 밖을 초기화하여 CCTV를 닫은 뒤 잔상이 남지 않게 합니다.

현재 방만 사물을 표시하는 기존 규칙을 유지합니다. `CCTV Full Walls`(13번 레이어)는 CCTV에서만 높은 벽·천장과 문 위쪽을 표시합니다. 일반 시점은 낮은 벽을 보여 캐릭터·조사 대상이 가려지는 일을 줄입니다. 실제 충돌 벽은 높이를 유지하므로 낮게 보이는 벽을 통과할 수 없습니다.

## 직접 제작한 에셋

`Assets/Art/NightCafe3D/Models`에 22종의 편집 가능한 프리팹이 있습니다. 테이블, 의자, 책상, 카운터, 선반, 침대, 식물, CRT 단말기, 규칙서·메모·레시피, 속이 빈 커피 컵과 손잡이, 재료 5종, 여닫이문, 근무자, CCTV 본체, 펜던트 조명, 에스프레소 머신을 포함합니다.

`Materials`에는 목재·청록색 가구·도자기·금속·종이 등 공유 재질이 있습니다. 바닥은 실제 목재 판재와 타일 메시를 재질별로 합쳐 저장합니다. `Meshes`의 컵·손잡이·화분 등도 직접 작성한 형상입니다. 외부 모델 다운로드나 이미지 생성 API를 사용하지 않았습니다.

- 사물 이동: 기존 월드 프리팹의 상위 오브젝트 위치(X/Z)를 편집합니다.
- 모델 편집: 각 사물의 하위 `... Model` 또는 Models 폴더의 원본 프리팹을 편집합니다.
- 충돌 편집: 기존 BoxCollider2D 대신 BoxCollider를 사용합니다. 문과 가구의 평면 충돌 크기는 기존과 동일합니다.
- 근무자: Rigidbody + CapsuleCollider, 실제 Spot Light 손전등. Body 아래 다리·팔의 회전으로 보행·재료 투입을 표현합니다.
- 문: CafeInteractable의 Door Hinge 참조가 문짝을 회전시킵니다. 기존처럼 열린 문은 다시 닫히지 않습니다.
- 커피: 3D 수면 높이와 투명도가 기존 범람 상태를 따르고, 해결 후 기존 자동 정리 시간에 맞춰 내려갑니다.
- CCTV: `CCTV Mounts/CAM 0n/.../Lens viewpoint` 위치와 방향, CafeWorld의 Cctv Fields Of View를 조정합니다. 채널은 카페·감시실·창고·숙직실 순서입니다.
- CCTV 화면 효과: `CCTVScreen.shader`와 `Materials/CCTV screen.mat`의 약한 색조·주사선·노이즈를 사용합니다.

## Lobby

기존 시작 화면의 제목·메뉴·도움말과 입력을 유지하면서, 배경 일러스트를 실제 3D 카페로 바꾸었습니다. `Cafe 3D Title Set`은 동일 월드 프리팹을 사용하며 플레이어는 비활성입니다. 게임 진행 로직은 시작하지 않습니다.

## 제작 도구와 검증

- `Night Cafe/Convert Existing Game to 3D`: 기존 2D 씬·프리팹을 `Artifacts/3DBackups`에 백업하고 좌표·상호작용 참조를 보존해 변환합니다. 이미 변환한 월드의 수동 편집을 덮어쓰지 않습니다.
- `Night Cafe/Validate 3D Presentation`: 레거시 2D 월드 컴포넌트 제거, 3D 모델·물리·재질·CCTV 네 위치와 기존 상태 규칙을 검사합니다.
- `Night Cafe/Validate Day One Rules`: 기존 시간·정답/오답·실패·재도전·복귀 조건 검사.
- `Night Cafe/Build Windows FHD`: 기존 두 씬만 Windows x64로 빌드합니다.
- 실행 파일의 `-foundationSmokeTest -testArtifacts <절대경로>`: 실제 플레이어에서 3D 화면·충돌·네 채널 영상·상호작용 도달 가능성 및 기존 1일차 흐름을 검사하고 스크린샷을 저장합니다.

`Rebuild Authored Scenes`는 저장된 3D 월드를 사용해 씬과 UI를 다시 만들며 UI 수동 편집을 초기화합니다. 일반 작업에서는 기존 씬과 프리팹을 직접 편집하세요. 나눔고딕과 기존 UI 아이콘의 라이선스·원본은 기존 Art 폴더에 유지됩니다.
