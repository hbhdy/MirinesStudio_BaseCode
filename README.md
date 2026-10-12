<div align="center">

# Mirines Studio · Game Framework

**게임마다 반복되는 준비를 줄이고, 게임 자체의 개발에 집중하기 위한 Unity 기반 프레임워크**

실무에서 얻은 지식과 경험을 바탕으로 공통 기능을 정리하고 개선하는 프로젝트입니다.

![Unity](https://img.shields.io/badge/Unity-6000.3.12f1-222222?logo=unity&logoColor=white)
![Language](https://img.shields.io/badge/Language-C%23-6955DB)
![Pipeline](https://img.shields.io/badge/Render_Pipeline-URP-4078C0)
![Status](https://img.shields.io/badge/Status-In_Development-2EA87E)

초기화와 씬 전환 · UI 수명 관리 · 데이터 도구 · 사운드 · 개발 편의 기능

</div>

---

## 프로젝트 소개

새로운 게임을 시작할 때마다 초기화, 씬 이동, 팝업, 데이터 로딩 등의 기반을 다시 만드는 비용을 줄이는 것이 목표입니다. 필요한 기능을 가져와 기본 게임 구조를 구성하고, 프로젝트에 맞게 확장할 수 있도록 만들고 있습니다.

기능을 많이 넣는 것만큼 **각 기능의 역할과 수명을 명확하게 나누는 것**을 중요하게 생각합니다.

- **실무 경험 기반** — 개발하면서 겪은 반복 작업과 불편함을 공통 코드로 정리합니다.
- **작은 단위의 확장** — 기존 구조에서 해결할 수 있는 문제는 불필요한 추상화 없이 보완합니다.
- **선택적인 SDK 연결** — 게임의 기본 흐름을 먼저 만들고, 외부 서비스는 필요한 시점에 연결합니다.
- **개발 중 확인 가능한 상태** — 초기화 상태, 팝업 목록, 데이터 오류를 확인할 수 있는 도구를 제공합니다.

## 주요 기능

| 영역 | 구현 내용 | 관련 코드 |
|---|---|---|
| 코어 | 매니저 등록, 초기화 단계 분리, 준비·실패 상태 관리 | [GameCore](MirinesStudio_BaseCode/Assets/Scripts/GameCore.cs) |
| 씬 | Init → Lobby 진입, 비동기 씬 로딩, 씬별 초기화·정리 | [SceneLoadingManager](MirinesStudio_BaseCode/Assets/Scripts/Manager/SceneLoadingManager.cs), [SceneContext](MirinesStudio_BaseCode/Assets/Scripts/SceneContext.cs) |
| UI | 씬 UI와 전역 UI 분리, 팝업 스택과 뒤로가기 처리 | [UIManager](MirinesStudio_BaseCode/Assets/Scripts/Manager/UIManager.cs), [UIPopupBase](MirinesStudio_BaseCode/Assets/Scripts/UI/UIPopupBase.cs) |
| UI 연출 | 대상 UI 강조, Canvas 간 좌표 변환, 대상 위치 추적 | [TutorialHighlighter](MirinesStudio_BaseCode/Assets/Scripts/Utility/TutorialHighlighter.cs) |
| 데이터 | Excel → CSV/TSV 변환, 변환 전 검증, 독립 검증 메뉴 | [데이터 도구](MirinesStudio_BaseCode/Assets/Scripts/Utility/Excel/Editor) |
| 로컬라이징 | 키 기반 문자열 조회, 언어 변경 이벤트에 따른 텍스트 갱신 | [LocalizationManager](MirinesStudio_BaseCode/Assets/Scripts/Manager/LocalizationManager.cs), [LocalizeText](MirinesStudio_BaseCode/Assets/Scripts/UI/LocalizeText.cs) |
| 사운드 | BGM 페이드, SFX 재사용, 타입별 중복 재생 제한, 광고 후 BGM 복구 | [SoundManager](MirinesStudio_BaseCode/Assets/Scripts/Manager/SoundManager.cs) |
| 오브젝트 풀 | 프리팹별 사전 생성·대여·반환, 반환 전 처리 인터페이스 | [ObjectPool](MirinesStudio_BaseCode/Assets/Scripts/Utility/ObjectPool.cs) |
| 개발 도구 | 코어·씬·UI 상태, FPS, 배속 제어를 제공하는 IMGUI 패널 | [DevelopmentDebugPanel](MirinesStudio_BaseCode/Assets/Scripts/UI/DevelopmentDebugPanel.cs) |
| 설정·유틸리티 | 가벼운 로컬 설정 저장, Safe Area, 진동, 버튼 피드백 | [Utility](MirinesStudio_BaseCode/Assets/Scripts/Utility) |

## 실행 환경과 시작 방법

| 항목 | 프로젝트 기준 |
|---|---|
| Unity Editor | **6000.3.12f1** |
| 렌더 파이프라인 | Universal Render Pipeline **17.3.0** |
| 입력 | Input System **1.19.0** |
| 비동기 처리 | UniTask |
| UI | uGUI / TextMesh Pro |
| 트윈 | DOTween / DOTween Pro 관련 파일 포함 |

패키지 구성은 [manifest.json](MirinesStudio_BaseCode/Packages/manifest.json), 에디터 버전은 [ProjectVersion.txt](MirinesStudio_BaseCode/ProjectSettings/ProjectVersion.txt)를 기준으로 합니다.

1. Unity Hub에서 `Assets`, `Packages`, `ProjectSettings`가 들어 있는 이 폴더를 프로젝트로 엽니다.
2. 패키지 복원과 에셋 임포트가 완료될 때까지 기다립니다.
3. [Init 씬](MirinesStudio_BaseCode/Assets/Scenes/Init.unity)을 열고 Play를 실행합니다.
4. `GameCore`가 매니저를 초기화한 뒤 기본 첫 씬인 `Lobby`로 이동합니다.

현재 씬 등록 순서는 **Init → Lobby → InGame**입니다. 새 씬을 추가할 때는 씬 목록에 등록하고 `SceneContext`, 씬 카메라, `CanvasRoot`를 준비합니다. 프리팹을 새 프로젝트에 옮길 때는 매니저 목록과 각 Inspector 참조도 확인해야 합니다.

> Addressables는 기본 패키지 목록에 포함되어 있지 않습니다. UI 로딩은 `ADDRESSABLES` 심볼이 없으면 Resources 경로를 사용합니다. Addressables를 사용할 때는 패키지, 주소 설정, 심볼을 함께 준비해야 합니다.

## 코어와 씬의 역할

```text
Init
 └─ GameCore
     ├─ 매니저 등록
     ├─ Task_Init() 순차 실행
     ├─ 공통 데이터 및 매니저 Init()
     └─ CoreReady
         └─ LoadingScene
             ├─ 대상 씬 로드
             ├─ SceneContext.InitializeAsync()
             └─ Ready
```

**코어 준비와 씬 준비를 구분합니다.** 매니저 초기화가 끝났더라도 씬의 카메라와 UI 부모가 아직 준비되지 않았을 수 있기 때문입니다. 씬별 참조 연결은 `SceneContext`가 담당하고, 다음 씬으로 이동할 때 이전 씬의 참조를 정리합니다.

`SceneLoadingManager`에는 Additive 로딩 API도 있습니다. 위의 `SceneContext` 초기화 흐름은 Single 씬 전환 기준이며, Additive 씬의 초기화 정책은 별도로 연결해야 합니다.

## 씬 UI와 전역 UI

| 구분 | 수명 | 용도 |
|---|---|---|
| `CanvasRoot` | 해당 씬과 함께 생성·제거 | 화면 UI, HUD, 씬 팝업 |
| `GlobalCanvasRoot` | GameCore와 함께 유지 | 전역 팝업, 로딩 화면, 하이라이트 |

로딩 화면을 이동 전후의 씬이 소유하면 씬이 제거되는 순간 UI도 사라질 수 있습니다. 전역 UI를 GameCore 아래에 두어 씬 전환을 가로질러 유지하도록 했습니다.

전역 Canvas는 **Screen Space - Overlay**를 사용합니다. 하이라이터는 강조 대상 Canvas의 좌표를 화면 좌표로 바꾼 뒤 전역 Canvas 좌표로 변환하므로, 대상이 Camera 방식의 UI여도 처리할 수 있습니다.

뒤로가기는 **전역 팝업 → 씬 팝업** 순서로 처리하며, 씬 전환 중에는 소비합니다. 입력 진입점은 `GameCore.Update()`의 Input System Escape 키 처리입니다. 팝업은 `UIPopupBase`를 상속해 연결합니다.

> UI는 `UIType`당 하나의 인스턴스를 재사용합니다. 같은 타입을 다른 위치로 다시 열면 기존 인스턴스를 옮깁니다. 전역 팝업의 데이터도 전역 수명에 맞게 관리해야 합니다.

## 사용 예제

아래 코드는 코어와 대상 씬이 준비된 이후의 호출 예시입니다. 사용 클래스는 `MirinesStudio` 네임스페이스에 있습니다.

### 팝업 열기

```csharp
// 현재 씬에 속하는 팝업
GameCore.UI.OpenUI<PopupUI_Common>(UIType.PopupUI_Common, Canvas_SortOrder.POPUP);

// 씬 전환 후에도 유지되는 전역 팝업
GameCore.UI.OpenUI<PopupUI_Common>(UIType.PopupUI_Common, Canvas_SortOrder.GLOBAL);
```

### 로딩 UI를 사용한 씬 전환

```csharp
// UniTask 비동기 메서드 내부에서 호출
await GameCore.SCENE.SceneLoadingWithAsyncUI("InGame");
```

`GlobalCanvasRoot`의 `loadingCanvas`와 진행률 표시를 연결해 사용합니다. 포인터 입력 차단막은 `inputBlockerCanvas`에 별도로 연결해야 하며, 현재 프리팹에서는 해당 참조가 비어 있습니다.

### BGM 전환

```csharp
// bgmClips에 등록된 인덱스 사용
GameCore.SOUND.PlayBGMWithFade(1, fadeDuration: 0.5f);
GameCore.SOUND.StopBGMWithFade(1f);
```

페이드 시간은 각 구간의 시간입니다. 기존 곡이 재생 중이면 **0.5초 페이드아웃 → 곡 교체 → 0.5초 페이드인**으로 동작합니다. 기존 `PlayBGM()`과 `StopBGM()`은 즉시 처리합니다.

효과음은 `SoundDataType`에 필요한 값을 추가하고 Inspector의 `Sfx Entries`에서 타입과 클립을 연결합니다. 목록 순서와 식별자를 분리하며, 중복 타입·빈 클립·잘못된 타입을 검사합니다. 현재 enum에는 `None`만 정의되어 있습니다.

### 언어 변경

```csharp
GameCore.LOCAL.ChangeLanguage(Language.en);
```

활성화된 `LocalizeText`는 변경 이벤트를 받아 표시를 갱신합니다. 현재 문자열 조회는 **한국어와 영어**가 구현되어 있으며, 추가 언어는 메타데이터와 조회 로직을 함께 확장해야 합니다.

## 데이터 테이블 워크플로

```text
Assets/DataTable의 Excel
    → 첫 번째 시트 검증
    → CSV 또는 TSV 변환
    → Assets/Resources/CSV 또는 Assets/Resources/TSV
```

- `MirinesStudio/Validate All DataTable`: 변환 없이 검증만 실행합니다.
- `MirinesStudio/Convert All DataTable`: 변환 창에서 CSV/TSV 출력을 실행합니다.
- 변환 과정에서도 검증하며, 오류가 있는 파일은 변환하지 않습니다.
- **첫 번째 시트만 사용**합니다. 두 번째 이후 시트는 임시 작업 공간으로 둘 수 있으며, 시트가 추가되어 있다는 이유로 경고하지 않습니다.
- CSV는 쉼표와 따옴표를 이스케이프하며, 셀 내부 줄바꿈은 `\n` 텍스트로 변환합니다.

## 개발용 디버그 패널

다음 조건을 모두 충족하면 GameCore에 패널이 자동으로 추가됩니다.

```csharp
#if UNITY_EDITOR && SHOW_LOG
```

현재 플랫폼의 **Scripting Define Symbols**에 `SHOW_LOG`를 추가하고, Game 뷰에서 **F1**을 눌러 사용합니다. 일반 빌드와 Development Build에는 포함되지 않습니다.

| 조작·표시 | 내용 |
|---|---|
| F1 / X | 열기·닫기 |
| Collapse / Expand | 접기·펼치기 |
| 상단 드래그 | 위치 이동 |
| 상태 | 코어 상태, 활성 씬, 로딩 여부 |
| UI | 열린 순서, 씬·전역 구분, 뒤로가기 대상 |
| 성능 | 최근 구간 평균 FPS와 프레임 시간 |
| 시간 배속 | ×0.5 / ×1 / ×2, 닫을 때 변경 전 배속 복원 |

패널이 열려 있을 때만 0.5초 간격으로 상태를 갱신합니다. 기존 팝업 스택에 등록하지 않으며, ESC는 게임의 팝업 동작을 확인하는 데 사용할 수 있습니다. 성능 표시는 에디터 실행 중의 간단한 관찰용 수치입니다.

## 선택 SDK와 설정 저장

광고, 결제, Firebase, GPGS, Analytics, GDPR 관련 코드는 **대부분 파일 전체가 주석 처리된 선택 적용 코드**입니다. 기본 개발 단계에서는 SDK를 강제로 연결하지 않습니다.

필요한 서비스만 SDK를 설치한 뒤 관련 코드를 활성화하고, 해당 SDK 버전에 맞는 API와 플랫폼 설정을 확인해 연결합니다. 이 저장소에 관련 파일이 있다는 것이 서비스 연결 완료를 의미하지는 않습니다.

`GameConfig`는 BGM·효과음·진동 등의 가벼운 사용자 설정을 위한 저장 용도입니다. 중요한 게임 진행 데이터나 재화의 서버 저장 기능을 대신하지 않습니다.

## 코드 탐색

```text
Assets/
├─ Scenes/                 Init · Lobby · InGame
├─ Prefab/                 GameCore와 Canvas 프리팹
└─ Scripts/
   ├─ GameCore.cs          공통 초기화와 매니저 접근
   ├─ SceneContext.cs      씬별 초기화와 참조 정리
   ├─ Manager/             UI · 씬 · 사운드 · 데이터 등
   ├─ UI/                  화면 기반 클래스와 전역 UI
   ├─ MetaData/            데이터 모델
   └─ Utility/
      └─ Excel/Editor/     데이터 변환·검증 도구
```

## 개발 방향

완성된 게임 대신 **게임 개발을 시작할 수 있는 기반과 개선 과정**을 담고 있습니다. 실제 게임에 적용하면서 필요한 기능을 보완하고, 구조를 복잡하게 만드는 확장은 필요성이 확인된 뒤 추가합니다.

<!-- 실제 실행 화면이나 GIF를 준비한 뒤 이 문서에 추가합니다. 존재하지 않는 시연 이미지는 연결하지 않습니다. -->
