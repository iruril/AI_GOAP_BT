# Tactical Operations: P2P Multiplayer PVP Shooter

![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity) ![C#](https://img.shields.io/badge/Language-C%23-blue?style=flat&logo=csharp) ![Mirror](https://img.shields.io/badge/Network-Mirror-green?style=flat) ![Steamworks](https://img.shields.io/badge/Platform-Steamworks-blue?style=flat&logo=steam) ![JobSystem](https://img.shields.io/badge/Tech-Job%20System-red)
![BurstCompile](https://img.shields.io/badge/Tech-Burst%20Compile-red) ![Async](https://img.shields.io/badge/Tech-Addressables-blue) ![Async](https://img.shields.io/badge/Tech-Async%2FTask-blueviolet)

> **"Steamworks.NET과 Mirror를 활용해 제작한 P2P 멀티플레이어 전술 슈팅 TPS 게임"**
>
> **Unity DOTS**를 활용한 **대규모 물리 투사체 연산**, 확장 가능한 **커스텀 GOAP** 시스템이 적용된 AI(BOT), 그리고 **Mirror** 기반의 **Host-Authoritative** 구조의 공정성 및 **Steam P2P**를 통한 안정적인 매치메이킹 환경을 제공합니다. 또한 **Unity Addressables**와 **LRU 캐싱** 기반의 사운드 리소스 관리 시스템을 통합하여 최적화된 자체 구현 게임 프로젝트입니다.

---

## 목차
1. [프로젝트 개요 (Overview)](#프로젝트-개요-overview)
2. [핵심 기술 및 구현 (Key Implementation)](#핵심-기술-및-구현-key-implementation)
    - [1. Hybrid AI System (GOAP + BT + FSM)](#1-hybrid-ai-system-goap--bt--fsm)
    - [2. Data-Oriented Bullet Simulation (Job & Burst)](#2-data-oriented-bullet-simulation-job--burst)
    - [3. Resource Management (LRU Cache)](#3-resource-management-lru-cache)
    - [4. P2P Networking (Steamworks & Mirror)](#4-p2p-networking-steamworks--mirror)
3. [기술적 도전 및 해결 (Troubleshooting & Optimization)](#기술적-도전-및-해결-troubleshooting--optimization)
    - [1. Bullet 물리 및 판정 연산 최적화: 왜 독립적인 Bullet Simulation인가?](#1-bullet-물리-및-판정-연산-최적화-왜-독립적인-bullet-simulation인가)
    - [2. AI 설계: 전략(GOAP)과 전술(BT)의 분리](#2-ai-설계-전략goap과-전술bt의-분리)
    - [3. 네트워크 대역폭: Packet Batching](#3-네트워크-대역폭-packet-batching)
4. [성능 최적화 성과 (Performance Optimization)](#성능-최적화-성과-performance-optimization)
5. [설치 및 사용법 (Installation)](#설치-및-사용법-installation)

---

## 프로젝트 개요 (Overview)
단순한 슈팅 메커니즘 구현을 넘어, 실제 유저들이 즐길 수 있는 **PvP/PvE 멀티플레이어 환경**을 구축하는 것을 목표로 했습니다. 2,000개 이상의 발사체가 오가는 전장에서도 60FPS를 방어하며, 봇(AI)과 플레이어가 함께 전투하는 하이브리드 매치를 지향합니다.

### 주요 목표
* **Steam Matchmaking:** 스팀 친구 초대 및 로비 시스템을 통한 간편한 P2P 접속.
* **High Performance Projectile:** 실시간 게임플레이를 저해하지 않는 정밀한 고성능 투사체.
* **Host Authority:** 클라이언트 변조 방지를 위한 서버 권한 검증 구조 (데미지, AI, 탄약 등).
* **Tactical AI:** 상황에 따라 유연하게 전략을 수립하고 정교하게 전투를 수행하는 지능형 봇.

---

## Demo

<p align="center">
  <a href="https://youtu.be/geRJJtGc5Wg">
    <img src="http://img.youtube.com/vi/geRJJtGc5Wg/0.jpg" width="60%">
  </a>
  <br>
  <em>Click to Watch Demo Video</em>
</p>

---

## 핵심 기술 및 구현 (Key Implementation)

### 1. Hybrid AI System (GOAP + BT + FSM)
단일 AI 알고리즘의 한계를 극복하기 위해, 역할에 따라 세 가지 레이어로 분리된 **계층적 AI 아키텍처**를 구축했습니다.
GOAP의 경우, 필요한 기능만을 구현하기 위해 직접 Generic 형태로 Goal, Action, Brain을 구축했습니다.

* **Layer 1: Strategy (Custom GOAP)**
    * **역할:** 최상위 의사 결정 (Root Decision Making).
    * **기능:** "생존(Survive)", "거점 점령(Capture)", "교전(Combat)"과 같은 거시적인 목표를 설정합니다. 현재 AI의 체력, 탄약, 전황을 분석하여 가장 적합한 `Action`을 도출합니다.
* **Layer 2: Tactics (Behavior Designer - BT)**
    * **역할:** 구체적인 전투 수행 (Execution).
    * **기능:** GOAP가 "교전" 상태를 결정하면, Behavior Tree가 활성화되어 **추적, 사격, 엄폐** 등 세부적인 전술 행동을 제어합니다.
* **Layer 3: Animation Control (Custom FSM)**
    * **역할:** 시각적 표현 (Visual Animation).
    * **기능:** 다양한 Action 상황에서 BOT이 수행하는 행동을 자연스럽게 동기화합니다.

```csharp
// AssaultBrain.cs (Partial)
protected override void RegisterGoals()
{
    // 람다식을 활용하여 상태 체크 로직을 간결하게 정의
    Goals.Add(AssaultGoal.SURVIVE, new GoapGoal<AssaultGoal>
    {
        Type = AssaultGoal.SURVIVE,
        Priority = 100,
        IsSatisfied = () => 
        {
            // 체력이 30% 이하이거나 탄약이 없으면 생존 목표 활성화
            bool hpLow = Sensor.MyStat.CurrentHP <= Sensor.MyStat.MaxHP * 0.3f;
            bool noAmmo = GunController.CurrentRounds <= 0;
            return !hpLow && !noAmmo; 
        }
    });

    /* ... */
}

protected override void RegisterActions()
{
    // GoapAction Class를 상속해 구현한 Action을 등록
    Actions.Add(AssaultAction.IDLE, new IdleAction(this, AssaultAction.IDLE, 50));
    Actions.Add(AssaultAction.MOVE_TO_CAPTURE, new MoveToCaptureAction(this, AssaultAction.MOVE_TO_CAPTURE, 20));

    /* ... */
}
```

### 2. Data-Oriented Bullet Simulation (Job & Burst)
Unity의 무거운 `Rigidbody`, `GameObject` 중심의 Bullet 구조를 탈피하고, **데이터 지향(DOD)** 설계를 적용하여 `Job`과 `Burst`를 사용해 독자적인 Bullet 시뮬레이터를 구현했습니다.
초기에는 `Rigidbody`만 제거한 후 Bullet마다 FixedUpdate주기로 RaycastNonAlloc하는 방식을 사용했으나, 여전히 성능이 좋지 않아 다음과 같이 리팩토링했습니다.

* **Architecture:** `BulletSimulator`가 매니저 역할을 하며, 실제 데이터는 `NativeArray<BulletData>`로 관리합니다.
* **Processing:**
    * **Job System:** 이동, 중력, 항력 계산을 워커 스레드로 분산.
    * **Burst Compiler:** C# 코드를 네이티브 기계어로 컴파일하여 연산 속도 극대화.
    * **RaycastBatch:** `RaycastCommand`를 사용하여 물리 엔진 오버헤드 최소화.

```csharp
// BulletSimulator.cs (Partial)
[BurstCompile]
public struct BulletMovementJob : IJobParallelFor
{
    public float DeltaTime;
    public NativeArray<BulletData> Bullets;
    [WriteOnly] public NativeArray<RaycastCommand> Commands;
    public LayerMask HitMask;

    public void Execute(int i)
    {
        BulletData bullet = Bullets[i]; // 데이터 지향 설계: 객체 참조 없이 순수 데이터(struct)만으로 연산

        /* ... 물리 연산 ... */

        // Raycast 명령 생성 (실제 Raycast는 메인 스레드 병목 없이 엔진 내부에서 일괄 처리됨)
        QueryParameters queryParams = new QueryParameters(HitMask, false, QueryTriggerInteraction.Collide, false);
        Commands[i] = new RaycastCommand(prevPos, rayDir, queryParams, dist);

        bullet.Position = nextPos;
        Bullets[i] = bullet; // 값 타입이므로 다시 배열에 저장
    }
}
```

### 3. Resource Management (LRU Cache)
추후 추가될 많은 양의 총기 사운드와 환경음 리소스들을 런타임 중 효율적으로 사용 및 관리하기 위해 **LRU(Least Recently Used)** 알고리즘을 적용했습니다.

* **Async Loading:** `Addressables`를 활용한 비동기 로딩으로 프레임 드랍 방지. 중복 로드를 막기 위한 안전장치 포함.
* **Cache Policy:** 메모리 한계 도달 시, 참조 빈도가 가장 낮은 리소스를 우선 해제하여 OOM(Out of Memory) 방지.

```csharp
// SoundManager.cs (Partial)
public async Task<AudioClip> LoadSound(string key)
{
    // 1. 메모리 캐시 적중 (Cache Hit)
    if (_soundPool.TryGetValue(key, out var clip))
    {
        _clipUsageTime[key] = Time.time; // LRU 갱신
        return clip;
    }

    // 2. 중복 로드 방지
    if (_loadingTasks.TryGetValue(key, out var existingTask))
    {
       return await existingTask;
    }

    // 3. 비동기 로딩 (Cache Miss)
    var handle = Addressables.LoadAssetAsync<AudioClip>(key);
    await handle.Task;

    /* ... */
}

private void CleanupLRUCache()
{
    // 사용 시간 기준으로 정렬하여 가장 오래된 리소스 해제
    var sorted = _clipUsageTime.OrderBy(x => x.Value).ToList();

    /* ... */
}
```

### 4. P2P Networking (Steamworks & Mirror)
* **Steam Integration:** `SteamLobby` 클래스를 통해 로비 생성, 데이터 동기화, 친구 초대 기능을 완벽하게 지원합니다.
* **Host Authority:** 데미지 판정, AI 로직 등 핵심 연산은 호스트(서버)에서만 수행하고 결과만 클라이언트에 `[SyncVar]`나 `[ClientRpc]`에 바인딩된 메소드를 통해 동기화됩니다.

```csharp
// SteamLobby.cs (Partial)
private void OnLobbyEntered(LobbyEnter_t callback) //로비 입장 성공 시 호출되는 콜백
{
    // 1. 호스트 여부 및 유효성 검증
    if (NetworkServer.active) return; // 호스트는 접속 로직 생략

    CurrentLobbyID = callback.m_ulSteamIDLobby;

    // 2. Steam 로비 데이터에서 호스트의 P2P 주소(SteamID) 추출
    //    (HostAddressKey는 호스트가 로비 생성 시 SetLobbyData로 저장해둔 값)
    string hostAddress = SteamMatchmaking.GetLobbyData(
        new CSteamID(CurrentLobbyID),
        HostAddressKey
    );

    // 3. Mirror NetworkManager에게 주소 주입 및 클라이언트 시작
    var manager = NetworkManager.singleton;
    manager.networkAddress = hostAddress; // 예: "steam://123456789..."
    manager.StartClient();
}
```
---

## 기술적 도전 및 해결 (Troubleshooting & Optimization)

### 1. Bullet 물리 및 판정 연산 최적화: 왜 독립적인 Bullet Simulation인가?
* **Problem (문제 상황):**
    * 초기 `GameObject`및`Rigidbody` 기반 총알은 100발 이상 동시 생성 시 메인 스레드 병목으로 인해 심각한 프레임 저하 및 고속에서의 Hit 신뢰성을 보장하지 못하는 문제가 있었습니다.
    * 그러므로 `Rigidbody`를 제거하고 직접 FixedUpdate주기로 RaycastNonAlloc으로 Hit를 감지함과 동시에 Gravity와 Drag 데이터를 사용해 물리 연산을 최소화했지만, 그럼에도 불구하고 병목현상은 해결하지 못했습니다. 
* **Decision (의사결정):**
    * **"다수의 Bullet들을 독립적인 Simulation에서 Batching하여 처리할 순 없을까?"**
    * 메인스레드는 이미 많은 작업을 수행하고 있으므로, 연산 파트를 독립적으로 다른 스레드에서 병렬적으로 연산한 후, 결과만 모아서 한번에 묶어 처리하는것이 유리할 것이라 판단했습니다.
    * 따라서 **Raycast 및 Physics 연산** 부분에 필요한 데이터들을 구조체로 정의한 후 **NativeContainer**로 관리하고, 메인스레드의 연산 작업을 **Job System**과 **Burst**로 분리하는 하이브리드 방식을 채택했습니다.
* **Solution (해결 방안):**
    * 총알의 로직을 `Struct` 기반 데이터로 변환하여 메모리 레이아웃을 최적화했습니다.
    * 이동 및 충돌 처리를 `IJobParallelFor`로 병렬화하여, 대규모 투사체를 무리없이 실시간 처리할 수 있도록 했습니다.

### 2. AI 설계: 전략(GOAP)과 전술(BT)의 분리
* **Problem (문제 상황):**
    * 초기에는 Behavior Tree(BT)만으로 AI를 구성했으나, "재장전", "엄폐", "점령" 등 상황 판단 로직이 추가될수록 트리가 지나치게 비대해지고, 유지보수가 어려워졌습니다.
* **Decision (의사결정):**
    * **"큰 그림은 GOAP가 그리고, 디테일은 BT가 맡도록 하면 어떨까?"**
    * 상황에 따른 유동적인 목표 설정은 GOAP가 가장 강력하므로 이를 최상위 두뇌(Brain)로 사용하고, 정해진 절차대로 움직여야 하는 전투 행동은 BT에 위임하기로 결정했습니다.
* **Solution (해결 방안):**
    * **Custom GOAP:** 현재 Sensor의 상태(HP, 탄약, 거리)를 기반으로 `Combat`, `Cover`, `Reload`, `Capture` 중 가장 비용이 낮은 `Action`을 선택합니다.
    * **Behavior Designer:** `Combat` 액션이 선택되면, BT가 활성화되어 `Fire`(사격), `Positioning`(위치 선정) 등 세밀한 의사결정을 수행합니다.
    * **Custom FSM:** AI의 의사결정에 맞추어 애니메이션을 자연스럽게 연동하도록 적합한 State를 선택합니다.
    * 이 구조를 통해 AI 로직의 가독성과 확장성을 동시에 확보했습니다.

### 3. 네트워크 대역폭: Packet Batching
* **Problem (문제 상황):**
    * 만일 다수의 탄환을 한꺼번에 발사하는 총기가 추가된다면, 각 펠릿(Pellet)마다 개별적인 RPC를 호출하게 되므로 순간적으로 네트워크 패킷량이 폭주할 것으로 예측되었습니다.
* **Decision (의사결정):**
    * 1프레임 내에서 발생한 사건들을 개별 전송할 경우 **패킷 헤더 오버헤드가 실제 데이터보다 커지는 비효율**이 발생합니다. 0.02초 미만의 배칭 지연은 사용자 경험(UX)을 해치지 않으므로, **'즉시성'보다는 '통신 효율성'** 을 선택했습니다.
* **Solution (해결 방안):**
    * **Network Batching System:** `FixedUpdate` 주기 동안 발생한 모든 타격 이벤트를 버퍼에 수집하고, 프레임 말단에 **단 하나의 배열(Array) 패킷**으로 직렬화하여 전송했습니다. 이를 통해 네트워크 호출 빈도를 (1/탄환 수)로 줄여 잠재적인 대역폭 낭비를 막았습니다.

---

## 성능 최적화 성과 (Performance Optimization)

가장 큰 성능 병목이었던 **물리 시뮬레이션** 파트에서 `Job System`과 `Burst Compiler` 도입 전후를 비교한 결과입니다.

### Hybrid Data-Oriented Bullet Simulation Benchmark

| 최적화 항목 | 최적화 전 (MonoBehaviour) | 최적화 후 (Job + Burst) | 개선 결과 |
|:---:|:---:|:---:|:---|
| **Frame Rate** | 6.7 ~ 75 FPS (Unstable) | **144 FPS (Stable)** | **약 50% 부하 감소 및 안정화** |
| **처리 방식** | 직렬 처리 (Sequential) | **병렬 처리 (Parallel)** | 멀티코어 활용 극대화 |
| **메모리 관리** | GC Allocation 발생 | **NativeArray (Zero Alloc)** | GC Spike 제거 |

> *테스트 환경: 탄속 250m/s의 Bullet을 0.1초 간격으로 사방으로 180개 지속적으로 발사.*
<br>

<table>
  <tr>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/abef75ea-4c37-429d-b3fa-96af21fca74c" alt="Before Optimization" width="450px" />
      <br />
      <strong>📉 최적화 전 (Before)</strong><br>
      FPS: 6.7 (CPU: 149.6ms) - 불안정
    </td>
    <td align="center">
      <img src="https://github.com/user-attachments/assets/85413d9b-682f-47d8-bd6e-2f7624d20224" alt="After Optimization" width="450px" />
      <br />
      <strong>📈 최적화 후 (After)</strong><br>
      FPS: 144 (CPU: 6.9ms) - 안정적
    </td>
  </tr>
</table>

</br>

---

## 설치 및 사용법 (Installation)

1. **빌드 및 설정 (Build & Setup)**
   - 프로젝트를 빌드합니다.
   - 빌드된 폴더 안에 `steam_appid.txt`를 생성한 후, 내용에 `480`을 기입한 후 저장합니다.

2. **Steam 외부게임 등록 (Register .exe on Steam Client)**
   - Steam 클라이언트 좌하단의 '외부 게임 등록'을 통해 빌드된 폴더 안의 `.exe`를 등록합니다.
   - 내부적으로 'Spacewar'라는 이름의 게임으로 Steam에 인식됩니다.
   - - *(참고: 480은 Steamworks 테스트용 공용 AppID인 'Spacewar'의 ID입니다.)*
   
3. **실행 (Start)**
   - 라이브러리에 'Spacewar'과 'AI_GOAP_BT'라는 이름의 게임이 두가지 추가되었을 것입니다.
   - 이 중에서 'AI_GOAP_BT'라는 이름의 게임을 선택하고 실행합니다.

---

### Contact
* **GitHub:** [https://github.com/iruril](https://github.com/iruril)
* **Email:** [gksxodnr99@gmail.com](mailto:gksxodnr99@gmail.com)

---
*Developed with Unity 6000.0.62f1 LTS & Mirror Networking & Steamworks.NET*
