📅 2024.06.10 ~ 2024.06.24 · 업데이트 2026.09
| [🎮플레이](https://sprout-farm-beta.vercel.app)

**※ PC 환경에서만 작동합니다.** (방향키·Shift·Space 조작)

# *SproutFarm 새싹 농장*이란?

![logo](https://github.com/user-attachments/assets/fc979162-de28-4332-b904-ac35ae4890b1)

타일맵을 이용한 `탑다운` 2D `픽셀` 게임으로, 간단한 조작으로 짧은 시간에 누구나 쉽게 즐길 수 있는 `캐주얼` 미니 게임입니다.👾

## 어떤 게임인가요?

`파니`는 농장을 운영하고 있습니다. 평화롭던 어느날... 한눈을 판 사이에 키우던 동물들이 도망가 농장의 평화가 깨져버렸습니다!! 날이 지나면 동물들을 찾기가 어려워져요😱 자정이 되기 전에 동물들을 찾으러 갑시다..!

### 조작 방법

상하좌우 `방향키`로 움직이고 `Shift 키`로 달립니다. `Space 바`는 대화 넘기기, 울타리에 동물 넣기, 집에서 잠자기 같은 특수 행동에 씁니다. 자정이 지나기 전에 동물 20마리를 모두 잡아 울타리에 넣으면 승리합니다.

### 규칙

- **패배 조건** — 자정이 되거나 체력이 0%가 되면 집니다.
- **동물** — 도망 상태에서는 주변을 배회하다 플레이어가 다가오면 도망칩니다. 잡히면 플레이어를 따라다니고, 울타리에 넣으면 그 안을 돌아다닙니다.
- **체력** — 걸으면 천천히, 달리면 빠르게 닳습니다. 쉬지 않고 계속 움직이면 피로가 쌓여 더 빨리 닳고 이동 속도도 느려집니다. 멈춰 있으면 회복됩니다.
- **열매** — 들판의 열매를 먹으면 체력이 차고 잠시 빨라집니다.
- **잠자기** — 집 침대에서 `Space`로 자면 체력이 가득 차고 게임 시계가 1시간 지나갑니다. 체력이 70% 이상이면 잠이 오지 않습니다.
- **흙길** — 길 위에서는 체력이 평소의 35%만 닳고 1.25배 빨리 움직입니다.

### 점수와 랭킹

한 판이 끝나면 아래 공식으로 점수를 매기고, 이름을 넣어 랭킹에 올릴 수 있습니다. 진행 중에도 화면 왼쪽 아래에 실시간 점수가 표시됩니다.

| 항목 | 점수 |
|------|------|
| 잡은 동물 (따라오는 중 포함) | 마리당 100 |
| 먹은 열매 | 개당 20 |
| 남은 체력 | 1%당 5 |
| 자정까지 남은 게임 시간 | 1분당 2 × (잡은 동물 ÷ 20) |
| 클리어 보너스 | 1000 |

랭킹은 TOP 10을 보여주고, 클리어한 기록에는 걸린 시간도 함께 표시됩니다. 점수는 서버에서 같은 공식으로 다시 계산해 저장하므로 임의의 점수를 올릴 수 없습니다.

### 맵 구성

- 픽셀 아트로 된 평화로운 자연 배경입니다. 타일맵으로 되어 있으며, 크기에 제한을 두지 않는 **무한 맵**입니다.
- 집에서 멀어지면 같은 벌판이 이어지지 않도록, 플레이어 주위를 12칸짜리 구역으로 나눠 **숲 · 바위밭 · 꽃밭 · 과수원 · 연못 · 빈터** 중 하나를 깔아 둡니다. 구역의 위치로 풍경이 정해지므로 같은 곳에 다시 가면 같은 풍경이 나옵니다.
- **연못**에는 물가와 수련잎이 생기고 물은 지나갈 수 없습니다. **흙길**은 24칸 간격으로 구불구불 이어지며, 집 현관 앞에서 남쪽 큰길까지 진입로가 놓여 있습니다.
- 소와 닭은 플레이어가 추적하는 대상입니다. 맵 내부를 자유롭게 돌아다니다가 플레이어가 가까이 다가오면 플레이어를 피해 도망갑니다.

### 어떤 기술을 사용했나요?

- **Unity** (C#) — 게임 본체. WebGL로 빌드합니다.
- **Vercel** — 정적 호스팅 + 서버리스 함수(`api/scores.js`), 랭킹 저장은 Redis.

# Features

## [Assets](https://cupnooble.itch.io/sprout-lands-asset-pack)

[![SproutLands](https://github.com/user-attachments/assets/0941fd8d-3f3f-4455-ab62-5005f1a8e1c7)](https://cupnooble.itch.io/sprout-lands-asset-pack)

| Player | Particle |
|----------|----------|
| ![Player](https://github.com/user-attachments/assets/7da04708-c3df-45ae-8c23-a7bc27b4e1cc) | ![Particle](https://github.com/user-attachments/assets/e7074117-e30b-4c9d-8d07-62a2a0d20e66) |

| Cow | Chicken |
|----------|----------|
|![Cow](https://github.com/user-attachments/assets/024a9351-d22a-4dad-bbf2-bd48372e2dea)|![Chicken](https://github.com/user-attachments/assets/f3c5a766-0737-4305-adfc-1fc5e93b23aa)|

| House | Animal House | Fences |
|----------|----------|----------|
|![House](https://github.com/user-attachments/assets/d6db1877-b7bd-4263-99f3-2ada179ae0db)|![AnimalHouse](https://github.com/user-attachments/assets/bf753bf1-3cf3-4ef5-aa88-829dbf51d8ba)|![Fences](https://github.com/user-attachments/assets/4a66c249-a66c-45f8-b43a-e055c1f3c7f8)|

| Plants | Grass |
|----------|----------|
|![Plants](https://github.com/user-attachments/assets/f8fa32ca-cc22-404e-b822-55f7894338f7)|![Grass](https://github.com/user-attachments/assets/5711979b-eb80-4422-a03c-34fca31aa3c7)|

| Water | Dirt |
|----------|----------|
|![Water](https://github.com/user-attachments/assets/d13f2419-13f6-4b6d-b09e-ff169d29c7c3)|![Dirt](https://github.com/user-attachments/assets/ec2f7d5f-bcad-4121-96ad-19acba2ca08a)|

| Dialogue Boxes | Emote |
|----------|----------|
|![Dialogue](https://github.com/user-attachments/assets/f2a92892-6c61-406f-a34b-c2058353798d)|![Emote](https://github.com/user-attachments/assets/4a5aa013-ab27-4591-81f1-5e9ef3a91d0a)|

| SFX | Font |
|----------|----------|
|![SFX](https://github.com/user-attachments/assets/d00a2e1d-7060-4a93-ad5c-580635ca3c32)|![Font](https://github.com/user-attachments/assets/586f9cf9-626e-4128-917e-1f5ab24e8e66)|

## Scripts

![Scripts](https://github.com/user-attachments/assets/b864d4f4-ef08-49d9-80b6-6b873b59ef80)

### 씬 구성

| Intro Scene | Game Scene | Clear Scene |
|----------|----------|----------|
| ![IntroScene](https://github.com/user-attachments/assets/74bec275-2956-4f9a-9f5b-0490694e5042) | ![GameScene](https://github.com/user-attachments/assets/a63912f9-5814-4d88-a991-060a0ed80f25) | ![ClearScene](https://github.com/user-attachments/assets/6be3aab2-37d3-4845-9bab-9afe7373d9cf) |
| 게임을 시작할 때 띄우는 도입 화면입니다. | 실제 게임을 플레이 하는 화면입니다. | 게임 종료 조건 달성 시 승리, 또는 패배 결과와 랭킹을 띄우는 화면입니다. |

### Level Design

![Level](https://github.com/user-attachments/assets/e7325062-a45e-4743-8953-a2333b113c8c)

## 구현 메모

### 무한 맵과 지형 생성

배경으로 쓰는 4개의 타일맵이 플레이어의 움직임에 따라 재배치되어 맵이 끝없이 이어지는 것처럼 보입니다. 그 위에 `WildTerrain`이 플레이어 주위 3×3 구역을 만들고 지웁니다.

- 구역의 풍경은 좌표로 정해집니다(같은 자리 = 같은 풍경). 한 프레임에 48칸씩만 꾸며서 새 구역이 만들어질 때 화면이 끊기지 않게 했고, 나무·바위·물처럼 길을 막는 것이 생겼을 때만 길찾기 그래프를 다시 읽습니다.
- 연못의 물가와 흙길 가장자리는 47조각 블롭 타일셋을 이웃 8칸 모양으로 골라 그립니다.
- 길은 좌표만으로 정해지는 함수라서 구역 경계에서도 끊기지 않고 이어집니다.

### 그리는 순서(레이어)

캐릭터·동물·나무·꽃이 모두 같은 정렬 층에서 **그림 밑동의 높이**로 앞뒤가 정해집니다. 밑동이 더 아래(화면에서 더 앞)면 앞에 그려집니다.

- 스프라이트 시트는 칸 전체를 경계로 삼도록(FullRect) 불러오고, 그림을 칸 바닥에 맞춰 둬서 모든 오브젝트가 같은 기준을 갖습니다.
- 꽃·새싹은 그림틀을 한 칸 더 높게 만들어 기준점을 올렸습니다. 그래서 캐릭터 발보다 반 칸 이상 앞에 있을 때만 앞으로 나오고, 몸통이나 머리를 가리지 않습니다.
- 꽃·새싹은 위아래로 붙여 놓지 않습니다(아래 칸에 선 캐릭터의 몸에 위 칸 그림이 걸치기 때문).

### 동물 AI

동물이 플레이어를 쫓거나 피할 때 경로를 직선으로만 계산하면 장애물에 걸려 멈춥니다. A* 길찾기를 적용해 장애물을 피해 움직이며, 게임 시작 때는 울타리에서 사방으로 흩어져 도망가는 연출이 나옵니다.

### 랭킹 서버

`api/scores.js`가 점수를 다시 계산해 Redis에 저장하고 TOP 10을 돌려줍니다. 연속 제출 제한(10초)과 이름 길이 제한이 있습니다.

## Demo

[🎮플레이](https://sprout-farm-beta.vercel.app)

**※ PC 환경에서만 작동합니다.**

[![sproutfarm](https://github.com/user-attachments/assets/60f9de6f-f882-451a-8c2c-1ab0cd4dbcfe)](https://youtu.be/XFgvLMcFpRo?si=RFJwEilEIvX2IQv9)

<details>
    <summary>영상 설명</summary>
    <div>
        <p>시연 영상입니다. (업데이트 전 버전이라 연못·흙길·랭킹은 나오지 않습니다.)<br>
        빠른 시간 안에 모든 기능을 보여주기 위해 플레이 타임이 짧아지도록 설정을 바꾼 상태입니다.<br>
        <ol>
            <li>잡아야 하는 동물 수를 줄였습니다.</li>
            <li>원래는 1시간에 30초로 총 8분의 플레이 타임을 가지고 있습니다.<br>
            → 이를 1시간에 5초로 변경해 플레이 타임을 대폭 줄였습니다.</li>
        </ol>
        [UI]
        <ol>
            <li>좌측 상단 시간에 따라서 화면의 밝기가 달라집니다. 새벽, 한낮, 노을, 밤으로 구성되어 있습니다.</li>
            <li>우측 상단 말풍선은 체력을 나타냅니다.</li>
            <li>우측 상단 화살표는 가장 가까운 동물의 방향을 알려주는 나침반입니다.<br>
            → 동물을 다 포획한 경우, 또는 체력이 얼마 남지 않은 경우 나침반은 집을 가리킵니다.</li>
            <li>좌측 하단에 실시간 점수, 우측 하단에 남은 동물 수를 표시합니다.</li>
            <li>플레이 방법을 안내하는 대화 상자가 있고, 도입부 대화는 SKIP 버튼으로 건너뛸 수 있습니다.</li>
        </ol>
        </p>
    </div>
</details>

### Team members
- 🧑‍💻 김정현(Jeong-Hyeon Kim) `codingkirby0@gmail.com`
