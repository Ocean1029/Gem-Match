# Enemy Health Bar Setup Guide

這份文件說明如何為敵人添加血條 UI 顯示系統。

## 程式修改總覽（已完成）

### 1. EnemyHealthBar.cs
新增的血條組件，功能包括：
- 使用 World Space Canvas 顯示血條
- 自動跟隨敵人位置（在敵人上方）
- 根據血量百分比更新
- 支援顏色漸變（滿血→綠色，瀕死→紅色）
- 可選擇始終顯示或只在受傷時顯示

### 2. Enemy.cs 整合
- 新增 `HealthBarPrefab` 欄位
- 在 `Init()` 中自動實例化血條
- 在 `Damage()` 中更新血條
- 在 `OnDeath()` 中隱藏血條

## Unity Editor 設定步驟

### 步驟 1：創建血條 UI Prefab

#### 1.1 創建 Canvas GameObject
1. Hierarchy → 右鍵 → UI → Canvas
2. 命名為 "EnemyHealthBarCanvas"
3. **重要：刪除自動創建的 EventSystem**
   - Unity 會自動創建 "EventSystem" GameObject
   - 在 Hierarchy 中找到並刪除它（場景中已經有 EventSystem 了）
4. 在 Inspector 中設定 Canvas 參數：
   - Render Mode: **World Space**
   - Dynamic Pixels Per Unit: 100
   - Width: 100（或你想要的寬度）
   - Height: 20（或你想要的高度）
   - Scale: (0.01, 0.01, 0.01) - 縮小以配合遊戲比例（程式會自動設定）

#### 1.2 創建 Slider（血條本體）
1. 選擇剛創建的 Canvas
2. 右鍵 → UI → Slider
3. 命名為 "HealthSlider"
4. 在 Inspector 中設定 Slider 參數：
   - Min Value: 0
   - Max Value: 1
   - Value: 1（滿血狀態）
   - Interactable: **取消勾選**（不需要互動）

#### 1.3 設定血條視覺
1. 展開 HealthSlider 的子物件
2. 刪除 "Handle Slide Area"（不需要拖曳控制）
3. 選擇 "Fill Area" → "Fill"
4. 設定 Fill 的 Image component：
   - Color: 綠色（例如 #00FF00）或使用你喜歡的顏色
   - Image Type: Filled（已經是預設）

5. 選擇 "Background"（可選）：
   - Color: 深灰色或黑色（例如 #333333）
   - 這會顯示血條的背景框

#### 1.4 調整血條位置和大小
1. 選擇 HealthSlider
2. 在 Rect Transform 中設定：
   - Anchors: 設定為中心錨點
   - Width: 80-100（血條寬度）
   - Height: 10-15（血條高度）
   - Pos Y: 0（置中於 Canvas）

#### 1.5 添加 EnemyHealthBar Script
1. 選擇最外層的 Canvas GameObject
2. Add Component → 搜尋 "Enemy Health Bar"
3. 在 Inspector 中設定 EnemyHealthBar script：
   - **Canvas**: 拖曳 Canvas component 到這個欄位
   - **Health Slider**: 拖曳 HealthSlider GameObject 到這個欄位
   - **Fill Image**: 拖曳 Fill Area → Fill 的 Image component 到這個欄位
   - **Offset**: (0, 0.6, 0) - 血條在敵人上方的偏移
   - **Always Show**: 勾選（始終顯示）或取消勾選（只在受傷時顯示）

#### 1.6 設定顏色漸變（可選但建議）
1. 在 Health Color Gradient 欄位中設定：
   - 點擊欄位展開 Gradient 編輯器
   - 左邊（0%）設定為紅色（瀕死）
   - 右邊（100%）設定為綠色（滿血）
   - 可以加入中間點（例如 50% 黃色）

#### 1.7 儲存為 Prefab
1. 在 Project 視窗導航到 `Assets/GemHunterMatch/Prefabs/`
2. 將 Canvas GameObject 從 Hierarchy 拖曳到這個資料夾
3. 命名為 "EnemyHealthBar"
4. 刪除 Hierarchy 中的原始 GameObject

### 步驟 2：將血條加入 Enemy Prefab

1. 在 Project 視窗中找到你的 Enemy prefab
2. 雙擊開啟 Prefab 編輯模式（或在 Hierarchy 中選擇）
3. 在 Inspector 中找到 Enemy (Script) component
4. 在 **Health Bar** 區塊：
   - **Health Bar Prefab**: 拖曳剛創建的 "EnemyHealthBar" prefab 到這個欄位

5. 儲存 Prefab（Ctrl+S 或 Cmd+S）

### 步驟 3：測試

1. 進入 Play Mode
2. 觀察敵人：
   - 應該會在敵人上方看到綠色的血條
   - 血條應該始終面向相機
   - 血條應該跟隨敵人移動

3. 使用 Bonus gem 攻擊敵人：
   - 血條應該減少
   - 顏色應該從綠色漸變到黃色再到紅色（如果有設定 gradient）

4. 擊敗敵人：
   - 血條應該消失
   - 格子應該可以被填充

## 進階自訂選項

### 血條樣式調整

#### 選項 1：改變血條外觀
- **圓角血條**：使用帶圓角的 sprite 作為 Fill 的 Source Image
- **邊框**：在 Background 使用有邊框的 sprite
- **漸層填充**：在 Fill Image 使用漸層 sprite

#### 選項 2：添加文字顯示
1. 在 Canvas 中添加 Text 元素（UI → Legacy → Text）
2. 修改 `EnemyHealthBar.cs` 添加：
   ```csharp
   public Text HealthText;
   ```
3. 在 `UpdateHealthBar()` 中更新文字：
   ```csharp
   if (HealthText != null)
   {
       HealthText.text = $"{m_CurrentHealth}/{m_MaxHealth}";
   }
   ```

#### 選項 3：動畫效果
在 Canvas 上添加 Animator component，在受傷時觸發震動或閃爍動畫。

### 效能優化

如果關卡中有很多敵人，可以考慮：
1. **使用 Object Pooling**：預先創建血條物件池
2. **降低更新頻率**：不要每幀都更新位置
3. **使用 Sprite 而非 Canvas**：更輕量但靈活度較低

### 視覺建議

**血條大小與比例**：
- 血條寬度建議為敵人 sprite 寬度的 80-100%
- 血條高度建議為 10-15 像素
- Offset Y 建議為 0.5-0.8（取決於敵人 sprite 大小）

**顏色方案**：
- 經典：綠色（滿血）→ 黃色（半血）→ 紅色（瀕死）
- 簡約：白色單色，透過透明度或長度表示血量
- 主題化：配合遊戲風格的顏色（例如藍色魔法能量）

**背景設定**：
- 使用半透明黑色背景（alpha 0.5-0.8）提高可讀性
- 或使用有外框的 sprite 讓血條更突出

## 疑難排解

### 問題 1：血條沒有出現
**檢查事項**：
- Enemy prefab → Health Bar Prefab 是否有設定
- EnemyHealthBar prefab 是否正確設定所有參數
- Canvas 的 Render Mode 是否為 World Space
- Canvas 的 Scale 是否太小或太大

### 問題 2：血條位置不對
**調整方法**：
- 修改 EnemyHealthBar → Offset 的 Y 值
- 檢查 Canvas 的 Width/Height 設定
- 調整 Canvas 的 Scale

### 問題 3：血條不跟隨敵人
**檢查事項**：
- EnemyHealthBar.cs 的 LateUpdate() 是否正常執行
- m_EnemyTransform 是否正確設定

### 問題 4：血條沒有更新
**檢查事項**：
- Health Slider 參數是否正確設定在 EnemyHealthBar script
- Fill Image 參數是否正確設定
- 檢查 Console 是否有錯誤訊息

### 問題 5：血條顏色不變
**檢查事項**：
- Health Color Gradient 是否有設定
- Fill Image 是否正確拖曳到 script 欄位

## 替代方案：使用 Sprite-based 血條

如果你想要更簡單的實作（不使用 Canvas），可以使用兩個 SpriteRenderer：

1. 背景 sprite（完整的血條框）
2. 填充 sprite（使用 sprite masking 或 scale 來調整長度）

這種方式效能更好但靈活度較低。如果需要這個方案，請告訴我，我可以提供對應的程式碼。

## 總結

血條系統已經完全整合到 Enemy 類別中。你只需要：
1. 在 Unity Editor 中創建血條 UI prefab
2. 將 prefab 設定到 Enemy prefab 的 Health Bar Prefab 欄位
3. 測試並調整視覺效果

血條會自動：
- 在敵人生成時創建
- 跟隨敵人移動
- 根據受傷更新
- 在敵人死亡時消失

