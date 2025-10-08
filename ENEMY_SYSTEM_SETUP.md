# Enemy System Setup Guide

這份文件詳細說明如何在 Unity Editor 中設定新增的敵人系統，以及如何將 Level 4 改為消滅敵人的關卡。

## 一、程式修改總覽

已完成的程式修改包含以下幾個核心檔案：

### 1. Enemy.cs
這是敵人的核心類別，繼承自 `Gem` 類別。敵人具有以下特性：
- **生命值系統**：可設定最大生命值（MaxHealth），被 bonus gem 攻擊時會扣血
- **回合計數機制**：追蹤玩家移動次數，在特定回合數後會自動移動
- **移動邏輯**：會隨機選擇相鄰的空格子或包含普通 gem 的格子進行移動
- **視覺狀態**：根據剩餘生命值顯示不同的 sprite（透過 HealthStates 陣列）
- **特效系統**：支援受傷特效（DamageEffect）、死亡特效（DeathEffect）和移動特效（MoveEffect）

### 2. Board.cs
新增了敵人追蹤和通知系統：
- **敵人註冊系統**：`RegisterEnemy()` 和 `UnregisterEnemy()` 靜態方法
- **回合通知機制**：每當玩家完成一次有效移動，會呼叫 `NotifyEnemiesOfPlayerMove()` 通知所有敵人
- **敵人清單管理**：內部維護一個 `m_Enemies` 清單來追蹤所有場上的敵人

### 3. LevelData.cs
新增了敵人目標系統：
- **EnemyGoal 類別**：新的目標類型，記錄需要擊敗的敵人數量
- **EnemyDefeated() 方法**：當敵人被擊敗時呼叫，更新目標進度並檢查是否完成關卡
- **整合到現有目標系統**：敵人目標會計入總目標數（GoalLeft）

### 4. EnemyPlacer.cs
這是一個 Tilemap Tile 類別，用於在 Unity Editor 中視覺化地放置敵人：
- 在 Editor 模式下顯示預覽圖示（PreviewEditorSprite）
- 在執行時自動實例化敵人 prefab 並初始化
- 遵循與 ObstaclePlacer 相同的設計模式

## 二、Unity Editor 設定步驟

### 步驟 1：創建敵人 Prefab

#### 1.1 建立基礎 GameObject
1. 在 Hierarchy 中右鍵點選 → Create Empty
2. 命名為 `Enemy`
3. 在 Inspector 中點選 Add Component
4. 搜尋並添加 `Enemy` script（剛才創建的 Enemy.cs）

#### 1.2 添加必要的 Component
1. 添加 `Sprite Renderer`：
   - 右鍵 Add Component → Rendering → Sprite Renderer
   - 設定 Order in Layer 為適當的值（例如 1 或 2，確保會顯示在背景之上）
   
2. （可選）添加 Animator：
   - 如果你想要敵人有動畫效果，可以添加 Animator component

#### 1.3 設定 Enemy Script 參數

在 Inspector 中找到 Enemy (Script) component，設定以下參數：

**Enemy Stats:**
- `Max Health`: 設定敵人的最大生命值（例如 3）
- `Moves Before First Move`: 敵人第一次移動前需要經過的玩家回合數（建議 3）
- `Move Interval`: 敵人之後每次移動的間隔回合數（建議 2）

**Visual:**
- `Health States`: 設定陣列大小為生命值階段數
  - 例如：3 個生命值可以設定 3 個 sprite（滿血、半血、瀕死）
  - 將對應的 sprite 拖曳到陣列欄位中
  - 系統會根據剩餘生命值自動切換 sprite

**Effects:**
- `Damaged Clip`: 受傷時的音效
- `Moved Clip`: 移動時的音效
- `Damage Effect`: 受傷時的 VFX（Visual Effect）
- `Death Effect`: 死亡時的 VFX
- `Move Effect`: 移動時的 VFX

**Gem Settings (繼承自 Gem 類別):**
- `Gem Type`: **建議設定為 -1**（與 LevelData → Enemy Goals → Gem Type 一致）
  - 使用 -1 可以避免與普通寶石的 GemType 衝突
  - 如果你想使用其他值（例如 100），確保 LevelData 中的 Enemy Goals → Gem Type 也設定為相同值
- `Match Effect Prefabs`: 可以留空或設定消滅時的特效
- `UI Sprite`: 用於 UI 顯示的 icon（不太需要，因為敵人目標使用 Enemy Goals → UI Sprite）

#### 1.4 儲存為 Prefab
1. 在 Project 視窗中，導航到 `Assets/GemHunterMatch/Prefabs/` 資料夾
2. 將 Hierarchy 中的 Enemy GameObject 拖曳到 Prefabs 資料夾
3. 刪除 Hierarchy 中的原始 GameObject（因為已經存成 prefab）

### 步驟 2：創建 EnemyPlacer Tile

#### 2.1 建立 Tile Asset
1. 在 Project 視窗中，導航到 `Assets/GemHunterMatch/Tiles/` 資料夾
2. 右鍵點選 → Create → 2D Match → Tile → Enemy Placer
3. 命名為 `EnemyPlacer`

#### 2.2 設定 Tile 參數
在 Inspector 中設定：
- `Preview Editor Sprite`: 選擇一個用於編輯器預覽的 sprite（讓你在繪製關卡時能看到敵人位置）
- `Enemy Prefab`: 拖曳剛才創建的 Enemy prefab 到這個欄位

### 步驟 3：將 EnemyPlacer 加入 Tile Palette

#### 3.1 開啟 Tile Palette
1. 在 Unity 上方選單選擇 Window → 2D → Tile Palette

#### 3.2 添加 Tile 到 Palette
1. 在 Tile Palette 視窗中，確認你正在編輯正確的 palette（通常是 Logic Palette）
2. 將剛創建的 `EnemyPlacer` tile asset 拖曳到 palette 中
3. 現在你應該能在 palette 中看到敵人的預覽圖示

### 步驟 4：修改 Level 4 場景

#### 4.1 開啟 Level 4 場景
1. 在 Project 視窗中找到 Level 4 的場景檔案
   - 路徑可能是：`Assets/GemHunterMatch/Scenes/Level4.unity`
2. 雙擊開啟場景

#### 4.2 設定 LevelData Component

在 Hierarchy 中找到包含 `LevelData` component 的 GameObject：

1. 找到 `Goals` 陣列，清空或移除現有的 gem goals
   - 因為這關的目標是消滅敵人，不是收集 gem
   - 如果想要混合目標（例如：收集 gem + 消滅敵人），可以保留部分 gem goals

2. 設定 `Enemy Goals`：
   - 展開 Enemy Goals 欄位
   - 設定 `Count` 為你想要放置的敵人數量（例如 5）
   - **設定 `Enemy Prefab`**：拖曳你創建的 Enemy prefab 到這個欄位
     - 系統會自動從這個 prefab 的 SpriteRenderer 取得 sprite 用於 UI 顯示
   - `UI Sprite`（可選）：
     - 如果留空，會自動使用 Enemy Prefab 的 sprite
     - 如果想使用不同的圖示，可以在這裡設定來覆蓋
   - **設定 `Gem Type`**：
     - 預設值為 -1（建議保持預設值）
     - 這個 ID 用來追蹤敵人目標，必須與你的 Enemy prefab 的 GemType 一致
     - 使用 -1 可以避免與普通寶石的 GemType 衝突

3. 調整其他參數：
   - `Max Move`: 設定允許的最大移動次數（建議 20-30，取決於難度）
   - `Low Move Trigger`: 剩餘移動數的警告閾值（建議 10）

#### 4.3 使用 Tile Palette 放置敵人

1. 確保 Tile Palette 視窗已開啟（Window → 2D → Tile Palette）

2. 在 Hierarchy 中選擇 Level 的 Grid → Logic Tilemap
   - 這是用於放置遊戲邏輯元素的 tilemap

3. 在 Tile Palette 中選擇剛才添加的 EnemyPlacer tile

4. 在 Scene 視窗中繪製敵人位置：
   - 點選你想要放置敵人的格子
   - 敵人會顯示為預覽圖示
   - 放置與 Enemy Goals Count 相同數量的敵人

5. 設計建議：
   - 將敵人分散在棋盤上，不要集中在一起
   - 避免放在 gem spawner 正下方（否則可能影響 gem 掉落）
   - 考慮敵人的移動範圍，在周圍留些空間

#### 4.4 測試關卡
1. 儲存場景（Ctrl+S 或 Cmd+S）
2. 進入 Play Mode（點選上方的 Play 按鈕）
3. 測試以下功能：
   - 敵人是否正確顯示
   - 經過設定的回合數後敵人是否會移動
   - Bonus gem 是否能對敵人造成傷害
   - 敵人血量降到 0 時是否正確消失
   - 目標計數器是否正確更新
   - 消滅所有敵人後是否觸發勝利

## 三、進階設定與調整

### 敵人行為參數調整

根據遊戲測試，你可能需要調整以下參數來平衡難度：

**如果敵人太強：**
- 減少 `Max Health`（讓敵人更容易被擊殺）
- 增加 `Moves Before First Move` 或 `Move Interval`（讓敵人移動較慢）
- 減少關卡中的敵人總數

**如果敵人太弱：**
- 增加 `Max Health`
- 減少 `Moves Before First Move` 或 `Move Interval`（讓敵人移動更頻繁）
- 增加關卡中的敵人總數

### 視覺效果建議

為了讓敵人系統更有趣，建議創建以下視覺元素：

1. **生命值 Sprites**（設定在 Enemy Prefab → Health States 陣列）：
   - 為不同生命值階段設計獨特的外觀
   - 可以使用顏色變化（滿血→綠色，半血→黃色，瀕死→紅色）
   - 或是加入視覺損傷效果（裂痕、破損等）
   - 這些 sprite 會在敵人受傷時自動切換

2. **特效 (VFX)**：
   - 受傷特效：閃光、粒子爆發等
   - 死亡特效：爆炸、消散動畫等
   - 移動特效：軌跡、塵土等

3. **音效 (SFX)**：
   - 受傷音效：可以是痛苦的聲音或撞擊聲
   - 移動音效：腳步聲或移動音效
   - 死亡音效：爆炸或消失的聲音

### UI 整合注意事項

目前的實作中，敵人目標使用特殊的 gem type ID `-1` 來通知 UI。如果你需要在 UI 中顯示敵人擊殺進度，可能需要額外修改 `UIHandler.cs` 來：

1. 識別 gem type `-1` 為敵人目標
2. 使用特殊的 icon 或樣式顯示敵人目標
3. 更新目標計數器的視覺呈現

這部分如果需要，可以根據現有的 UI 系統進行擴展。

## 四、疑難排解

### 問題 1：敵人沒有出現在遊戲中

**可能原因與解決方法：**
- 檢查 EnemyPlacer tile 是否正確設定了 Enemy Prefab
- 確認 Logic Tilemap 上確實有繪製 EnemyPlacer tile
- 檢查 Enemy prefab 的 Sprite Renderer 是否有設定 sprite
- 確認 Sprite Renderer 的 Order in Layer 數值足夠高

### 問題 2：敵人不會移動

**可能原因與解決方法：**
- 檢查 `Moves Before First Move` 和 `Move Interval` 參數是否設定正確
- 確認玩家的移動有正確觸發（檢查是否成功配對 gem）
- 查看 Console 視窗是否有錯誤訊息
- 確認敵人周圍有空格子或普通 gem（沒有可移動位置時敵人不會移動）

### 問題 3：Bonus gem 無法傷害敵人

**可能原因與解決方法：**
- 檢查 bonus gem 是否正確實作了傷害邏輯
- 確認 Enemy 的 `Damage()` 方法有被正確呼叫
- 檢查 Enemy prefab 的 Max Health 設定是否正確

### 問題 4：擊敗敵人後目標沒有更新

**可能原因與解決方法：**
- 確認 LevelData 中的 Enemy Goals 有正確設定
- 檢查 `EnemyDefeated()` 方法是否被正確呼叫
- 查看 Console 視窗的錯誤訊息

### 問題 5：遊戲在敵人移動時卡住或崩潰

**可能原因與解決方法：**
- 檢查是否有無限迴圈（例如敵人移動邏輯出錯）
- 確認 Coroutine 的使用是否正確
- 查看 Console 視窗的錯誤堆疊資訊

## 五、擴展建議

完成基本的敵人系統後，你可以考慮以下擴展方向：

### 1. 多種敵人類型
創建不同的敵人變種，各有不同的特性：
- **快速敵人**：移動間隔較短（Move Interval = 1）
- **坦克敵人**：生命值較高（Max Health = 5）
- **遠程敵人**：可以攻擊周圍的 gem
- **分裂敵人**：被擊敗後分裂成多個小敵人

實作方式：繼承 `Enemy` 類別並覆寫相關方法。

### 2. 敵人攻擊能力
讓敵人不只是移動，還能對玩家造成影響：
- 在 `OnPlayerMove()` 中加入攻擊邏輯
- 破壞周圍的 gem
- 鎖定某些格子
- 減少玩家的移動次數

### 3. 敵人移動模式
改善移動邏輯，讓敵人更有策略性：
- 追蹤玩家（朝向某個目標移動）
- 預設的移動路徑
- 群體行為（多個敵人協同移動）

### 4. 視覺回饋增強
- 添加回合計數器顯示（顯示敵人還有幾回合會移動）
- 敵人移動前的警告動畫
- 更豐富的受傷反應（顏色閃爍、震動等）

## 六、程式架構說明

### 重要：如何訪問 Board

在這個專案中，`Board` 類別是 scene-local singleton，但它**沒有公開的 Instance 屬性**。正確的訪問方式是：

```csharp
// 正確：透過 GameManager 訪問 Board
GameManager.Instance.Board.CellContent[position]

// 錯誤：Board 沒有公開的 Instance
Board.Instance.CellContent[position]  // 編譯錯誤！
```

這是因為 `Board` 使用 `private static Board s_Instance`，而 `GameManager` 有一個 `public Board Board` 欄位來提供訪問。

為了幫助你理解系統運作方式，以下是關鍵的程式流程：

### 敵人初始化流程
```
1. Level 場景載入
2. Tilemap.StartUp() 被呼叫（在所有 Awake 之前）
3. EnemyPlacer.StartUp() 實例化 Enemy prefab
4. Enemy.Init() 註冊到 Board
5. Board.RegisterEnemy() 將敵人加入追蹤清單
6. Enemy 被放置到對應的 BoardCell.ContainingGem
```

### 回合推進流程
```
1. 玩家完成一次有效的 gem 交換
2. Board 呼叫 LevelData.Instance.Moved()
3. Board 呼叫 NotifyEnemiesOfPlayerMove()
4. 每個 Enemy.OnPlayerMove() 被呼叫
5. Enemy 內部計數器遞增
6. 如果達到移動條件，呼叫 AttemptMove()
7. Enemy 選擇目標位置並執行移動動畫
```

### 敵人受傷流程
```
1. Bonus gem 的效果範圍包含敵人所在格子
2. BonusGem.HandleContent() 或類似方法呼叫 Enemy.Damage()
3. Enemy.Damage() 扣除生命值並更新視覺
4. 如果生命值歸零，呼叫 OnDeath()
5. OnDeath() 呼叫 LevelData.Instance.EnemyDefeated()
6. Board.UnregisterEnemy() 移除敵人追蹤
7. Enemy GameObject 被銷毀
```

### 目標檢查流程
```
1. LevelData.EnemyDefeated() 被呼叫
2. Enemy Goals Count 遞減
3. 透過 OnGoalChanged 事件通知 UI 更新
4. 如果 Count 歸零且所有其他目標也完成
5. 觸發 OnAllGoalFinished 事件
6. 顯示勝利畫面
```

## 七、總結

這個敵人系統已經完整整合到現有的 Match-3 遊戲架構中，遵循專案的設計模式：

- **繼承體系**：Enemy 繼承 Gem，可以無縫整合到現有的格子系統
- **Singleton 模式**：透過 Board.Instance 進行全域管理
- **Tilemap Authoring**：使用視覺化的方式設計關卡
- **事件驅動**：透過 delegate 和 callback 進行系統間通訊
- **物件池考量**：VFX 使用 GameManager 的 PoolSystem

按照以上步驟設定後，你應該能夠成功地將 Level 4 轉換為消滅敵人的關卡。如果遇到任何問題，請檢查 Console 視窗的錯誤訊息，並參考疑難排解章節。

祝你遊戲開發順利！

