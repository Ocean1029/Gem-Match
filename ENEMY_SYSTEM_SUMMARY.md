# Enemy System Implementation Summary

## 完成的程式修改

### 新增檔案
1. **Enemy.cs** - 敵人核心類別
   - 位置：`Assets/GemHunterMatch/Scripts/Enemy.cs`
   - 功能：生命值、回合計數、自動移動、受傷與死亡邏輯

2. **EnemyPlacer.cs** - Tilemap 編輯器工具
   - 位置：`Assets/GemHunterMatch/Scripts/Authoring/EnemyPlacer.cs`
   - 功能：在 Unity Editor 中視覺化放置敵人

### 修改檔案
1. **Board.cs**
   - 新增敵人追蹤系統（`m_Enemies` 清單）
   - 新增 `RegisterEnemy()` 和 `UnregisterEnemy()` 方法
   - 新增 `NotifyEnemiesOfPlayerMove()` 方法
   - 在玩家完成移動時通知所有敵人

2. **LevelData.cs**
   - 新增 `EnemyGoal` 類別
   - 新增 `EnemyGoals` 欄位
   - 新增 `EnemyDefeated()` 方法
   - 整合敵人目標到現有目標系統

## Unity Editor 快速設定流程

### A. 程式部分的修改（已完成）
✅ 所有程式碼已經寫好，包含敵人系統和血條系統，無需額外修改

**新增檔案**：
- Enemy.cs - 敵人核心類別
- EnemyPlacer.cs - Tilemap 編輯工具
- EnemyHealthBar.cs - 血條 UI 組件

**修改檔案**：
- Board.cs - 敵人追蹤、回合通知、空格子標記
- LevelData.cs - 敵人目標系統
- UIHandler.cs - 敵人目標 UI 顯示
- GameManager.cs - Bonus item 使用後的清理
- BonusGem.cs - 處理敵人死亡的安全檢查
- LineRocket.cs - 處理敵人死亡的安全檢查

### B. Unity Editor 上面的修改

#### 1. 創建血條 Prefab（可選但建議）
- [ ] Hierarchy → UI → Canvas，命名為 "EnemyHealthBarCanvas"
- [ ] **刪除自動創建的 EventSystem**（場景中已經有了）
- [ ] 設定 Canvas：Render Mode = World Space
- [ ] 添加 UI → Slider，命名為 "HealthSlider"
- [ ] 設定 Slider：Min=0, Max=1, Value=1, Interactable=取消勾選
- [ ] 刪除 "Handle Slide Area"
- [ ] 設定 Fill 顏色（綠色）和 Background 顏色（深灰）
- [ ] Add Component → Enemy Health Bar (Script)
- [ ] 設定 EnemyHealthBar：
  - Canvas: 拖曳 Canvas component
  - Health Slider: 拖曳 HealthSlider
  - Fill Image: 拖曳 Fill Area → Fill 的 Image
  - Offset: (0, 0.6, 0)
  - Health Color Gradient: 設定綠→黃→紅漸變
- [ ] 儲存為 Prefab "EnemyHealthBar"

#### 2. 創建敵人 Prefab
- [ ] Hierarchy → Create Empty → 命名為 "Enemy"
- [ ] Add Component → Enemy (Script)
- [ ] Add Component → Sprite Renderer
- [ ] 設定 Enemy 參數：
  - Max Health: 3
  - Moves Before First Move: 3
  - Move Interval: 2
  - Health States: 設定不同生命值的 sprites
  - Gem Type: -1（與 LevelData → Enemy Goals 一致）
  - **Health Bar Prefab: 拖曳 EnemyHealthBar prefab**
- [ ] 儲存為 Prefab 到 `Assets/GemHunterMatch/Prefabs/`

#### 3. 創建 EnemyPlacer Tile
- [ ] Project → `Assets/GemHunterMatch/Tiles/`
- [ ] 右鍵 → Create → 2D Match → Tile → Enemy Placer
- [ ] 設定：
  - Preview Editor Sprite: 選擇預覽用 sprite
  - Enemy Prefab: 拖曳剛才的 Enemy prefab

#### 4. 加入 Tile Palette
- [ ] Window → 2D → Tile Palette
- [ ] 將 EnemyPlacer tile 拖曳到 Logic Palette

#### 5. 修改 Level 4
- [ ] 開啟 Level 4 場景
- [ ] 選擇 LevelData GameObject
- [ ] 設定 Enemy Goals：
  - Count: 5（或你想要的敵人數量）
  - **Enemy Prefab: 拖曳 Enemy prefab**（會自動使用其 sprite 顯示在 UI）
  - UI Sprite: 留空（自動使用 prefab sprite）或設定自訂圖示
  - Gem Type: -1（預設值，與 Enemy prefab 的 GemType 保持一致）
- [ ] 清空或移除 Goals 陣列（因為目標改為擊敗敵人）
- [ ] 使用 Tile Palette 在 Logic Tilemap 上繪製 5 個敵人
- [ ] 儲存場景

#### 6. 測試
- [ ] 進入 Play Mode
- [ ] 確認敵人正確顯示
- [ ] 確認敵人會在設定回合後移動
- [ ] 確認 Bonus gem 可以傷害敵人
- [ ] 確認擊敗所有敵人後勝利

### C. 其他需要做的事情

#### 美術資源（必要）
- [ ] 敵人的 sprite 圖片（至少 1 張，建議 3 張代表不同生命值）
  - 會自動用於 UI 目標顯示
- [ ] 敵人的 Editor 預覽 sprite（用於 Tile Palette）

#### 美術資源（可選）
- [ ] 敵人受傷 VFX
- [ ] 敵人死亡 VFX
- [ ] 敵人移動 VFX
- [ ] 敵人相關音效（受傷、死亡、移動）

#### 測試與調整
- [ ] 調整敵人參數以平衡難度
- [ ] 測試不同的敵人擺放位置
- [ ] 確認與其他 gem 和 bonus 的互動正確

## 重要提醒

### Board 訪問方式
在這個專案中，`Board` **沒有公開的 Instance 屬性**。正確訪問方式：
```csharp
// ✅ 正確
GameManager.Instance.Board.CellContent[position]

// ❌ 錯誤（會編譯失敗）
Board.Instance.CellContent[position]
```

## 關鍵設計決策

1. **Enemy 繼承 Gem 而非 Obstacle**
   - 原因：Gem 系統已經完整支援格子佔據、傷害、移動等功能
   - 優點：可以直接使用現有的 BoardCell.ContainingGem 系統
   - 參考：Crate 也是用同樣的方式實作

2. **回合計數在 Enemy 內部**
   - 每個敵人獨立追蹤自己的回合數
   - Board 只負責通知，不管理個別敵人狀態
   - 允許未來擴展不同移動模式的敵人

3. **敵人目標使用 gem type -1**
   - 與現有的 gem goal 系統相容
   - UI 可以透過這個特殊 ID 識別敵人目標
   - 未來可以擴展支援多種敵人類型

## 疑難排解速查

| 問題 | 可能原因 | 解決方法 |
|------|---------|---------|
| 敵人沒出現 | Prefab 沒設定好 | 檢查 EnemyPlacer tile 是否有設定 Enemy Prefab |
| 敵人不移動 | 參數設定錯誤 | 檢查 Moves Before First Move 和 Move Interval |
| 無法傷害敵人 | Bonus gem 實作問題 | 確認 bonus gem 有呼叫 Damage() 方法 |
| 目標不更新 | LevelData 沒設定 | 確認 Enemy Goals 有正確設定 Count |

## 詳細說明

完整的設定教學和進階功能說明，請參考 `ENEMY_SYSTEM_SETUP.md`。

