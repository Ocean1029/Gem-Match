# WARP.md

This file provides comprehensive guidance for AI assistants and developers working with this Unity Match-3 game codebase.

## Project Overview
This is a Unity Match-3 gem puzzle game built with Unity 6000.2.2f1. The game features gem matching mechanics, bonus gems, various obstacles, level progression, and WebGL deployment to itch.io.

**Namespace:** All game code is within the `Match3` namespace.

## Project Structure

### Key Directories
```
Assets/GemHunterMatch/
├── Scripts/              # All C# game logic
│   ├── Authoring/       # Editor-time tile system for level design
│   ├── BonusGem/        # Special gem implementations
│   ├── BonusItem/       # Usable bonus items
│   ├── ShopItem/        # Shop system items
│   ├── UI/              # UI controllers
│   └── Editor/          # Custom Unity editor tools
├── Scenes/              # Game scenes (Init, Menu, Levels)
├── Prefabs/             # Reusable game objects
├── Tiles/               # Tilemap assets for level authoring
└── Resources/           # Runtime-loadable assets (GameManager)
```

## Core Architecture

### 1. Singleton Systems

#### GameManager (Global Persistent Singleton)
**Location:** `GameManager.cs`  
**Execution Order:** -9999 (runs very early)  
**Lifecycle:** Persists across all scenes via `DontDestroyOnLoad`

**Responsibilities:**
- Central coordinator for all game systems
- Audio management (music crossfading, SFX pooling)
- VFX pooling system (`VFXPoolSystem`)
- Player progression data (coins, lives, stars)
- Bonus item inventory management
- Level initialization and transitions
- Settings management (`GameSettings`)

**Key Properties:**
```csharp
public static GameManager Instance { get; }  // Singleton access
public Board Board;                          // Current level's board
public InputAction ClickAction;              // Input system actions
public GameSettings Settings;                // Game configuration
public int Coins/Stars/Lives { get; }        // Player resources
public VFXPoolSystem PoolSystem { get; }     // Visual effects pool
```

**Editor vs Build Behavior:**
- **Editor:** Automatically instantiates from Resources folder if missing (allows testing from any scene)
- **Build:** Must be initialized by Init scene, guaranteed to exist

**Important Methods:**
- `StartLevel()`: Called by LevelData when level loads
- `MainMenuOpened()`: Cleanup when returning to menu
- `ComputeCamera()`: Adjusts camera to fit board

#### Board (Scene-Local Singleton)
**Location:** `Board.cs`  
**Execution Order:** -9999  
**Lifecycle:** Created/destroyed with each level scene

**Responsibilities:**
- Grid-based game board management
- Gem placement, movement, and physics
- Match detection and validation
- Input handling (swipe, double-tap)
- Hint system and possible move detection
- Bonus item activation
- Board actions (IBoardAction interface for timed effects)

**Key Data Structures:**
```csharp
public Dictionary<Vector3Int, BoardCell> CellContent;  // All board cells
public List<Vector3Int> SpawnerPosition;               // Gem spawn points
public Gem[] ExistingGems;                             // Available gem types in this level
```

**Why Scene-Local Singleton:**
Each level has unique board configuration (shape, size, obstacles). When switching levels, old Board is destroyed and new one is created, automatically cleaning up all level-specific data.

**Initialization Flow:**
1. Tilemap tiles call static registration methods (`RegisterCell`, `RegisterSpawner`, `AddObstacle`)
2. `LevelData.Awake()` triggers `GameManager.StartLevel()`
3. GameManager calls `Board.Init()` after one frame delay
4. `GenerateBoard()` creates initial gem layout (ensuring no starting matches)
5. `FindAllPossibleMatch()` calculates valid moves

#### UIHandler (Singleton)
**Location:** `UI/UIHandler.cs`  
**Execution Order:** -9000  
**Lifecycle:** Scene-dependent (exists in level scenes)

**Responsibilities:**
- UI Toolkit-based interface management
- Goal display and updates
- Move counter
- End game screens (win/lose)
- Character animations
- Shop interface
- Settings menu
- Bonus item bar

### 2. Level Design System (Authoring)

The game uses Unity's Tilemap system for visual level design. Tiles are **only used at edit/load time** and convert to runtime data structures.

#### Authoring Tiles (Assets/GemHunterMatch/Scripts/Authoring/)

**GemPlacerTile.cs**
- Defines cells where gems can exist
- `PlacedGem = null`: Random gem spawned
- `PlacedGem = specific gem`: Exact gem type placed
- Calls `Board.RegisterCell(position, PlacedGem)` on startup

**GemSpawner.cs**
- Defines where new gems spawn (usually top of board)
- Calls `Board.RegisterSpawner(position)` on startup
- New gems fall from these positions when board needs refilling

**ObstaclePlacer.cs**
- Places obstacles (crates, ice blocks, etc.)
- Instantiates `ObstaclePrefab` at runtime
- Calls `Board.AddObstacle(cell, obstacle)` on startup

**Key Pattern:** All authoring tiles implement:
```csharp
public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
{
    // Show preview sprite in editor, null (invisible) in play mode
    tileData.sprite = !Application.isPlaying ? PreviewEditorSprite : null;
}

public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
{
    // Register with Board system at runtime
    Board.RegisterXXX(position, ...);
}
```

**Why This Design:**
- Visual, intuitive level design in Unity Editor
- No manual coordinate entry needed
- Tilemap data converts to efficient runtime Dictionary
- Clean separation: Tilemap for authoring, Dictionary for gameplay

### 3. Level Management

#### LevelData.cs
**Execution Order:** 12000 (runs after Board initialization)

Per-level configuration stored in each level scene:
```csharp
public string LevelName;              // Display name
public int MaxMove;                   // Move limit
public int LowMoveTrigger;            // Warning threshold
public GemGoal[] Goals;               // Win conditions (gem types + counts)
public float BorderMargin;            // Camera padding
public SpriteRenderer Background;     // Visual background
public AudioClip Music;               // Level music
```

**Lifecycle:**
1. `Awake()`: Sets static Instance, calls `GameManager.StartLevel()`
2. Listens to board events via callbacks:
   - `OnAllGoalFinished`: Triggered when all goals met
   - `OnNoMoveLeft`: Triggered when moves exhausted
   - `OnGoalChanged`: Updates UI when goal progress changes
   - `OnMoveHappened`: Updates move counter

**Goal System:**
```csharp
public class GemGoal
{
    public Gem Gem;    // Target gem type
    public int Count;  // Required quantity
}
```

`Matched(Gem gem)` method decrements goal count and checks win condition.

#### LevelList.cs
**Type:** ScriptableObject

Manages level progression and build settings:
- **Editor:** Array of `SceneAsset[]` for direct scene references
- **Build:** Array of `int[]` scene indices in build settings
- `LoadLevel(int levelNumber)`: Loads level scene (different implementation for editor vs build)
- `BuildLevelList` class: IPreprocessBuildWithReport that auto-updates build settings

**Build Process:**
1. Scans for LevelList assets
2. Ensures all levels in list are in Build Settings
3. Updates scene indices for runtime use
4. Displays dialog if changes made (requires rebuild)

### 4. Gem System

#### Base Class Hierarchy
```
Gem (base)
├── Regular gems (colored gems for matching)
├── BonusGem (abstract) - special powered gems
│   ├── LineRocket - clears row/column
│   ├── SmallBomb - 3x3 area
│   ├── LargeBomb - 5x5 area
│   └── ColorClean - all gems of same color
└── Obstacle - blocks that need destruction
    └── Crate - wooden box obstacle
```

#### Gem.cs (Base Class)
**State Machine:**
```csharp
public enum State {
    Still,        // Stationary, can be matched
    Falling,      // Moving to new position
    Bouncing,     // Landing animation
    Disappearing  // Being destroyed
}
```

**Key Properties:**
```csharp
public int GemType;                     // Unique identifier for matching
public bool CanMove;                    // Can participate in physics
public Vector3Int CurrentIndex;         // Current grid position
public Match CurrentMatch;              // Current match participation
public int HitPoint;                    // Health (for multi-hit gems)
protected bool m_Usable;                // Can be activated by player
```

**Lifecycle Methods:**
- `Init(Vector3Int startIdx)`: Initialize at grid position
- `Use(Gem swappedGem, bool isBonus)`: Activate special ability (for BonusGem)
- `Damage(int damage)`: Reduce hit points, returns true if still alive
- `MoveTo(Vector3Int newCell)`: Update grid position
- Movement state transitions: `StartMoveTimer()` → `StopFalling()` → `StopBouncing()`

#### BonusGem.cs (Abstract)
Special gems created by matching specific patterns.

**MatchShape System:**
```csharp
public class MatchShape
{
    public List<Vector3Int> Cells;     // Pattern cells
    public bool CanMirror;             // Allow horizontal/vertical flip
    public bool CanRotate;             // Allow 90/180/270 rotation
}
```

Each BonusGem has `List<MatchShape> Shapes` defining what match patterns spawn it.

**Pattern Matching Algorithm:**
- Pre-computes rotated and mirrored variants on deserialization
- `FitIn(cellList, matchedCells)`: Tests if shape fits in match
- Tries all rotations/mirrors if enabled
- Returns matched cells if successful

**Common Methods:**
- `HandleContent(cell, receivingMatch)`: Helper to destroy gems/obstacles in area
- `BonusTriggerEffect()`: Play VFX when activated

**Implementations:**
- **LineRocket:** Clears entire row or column
- **SmallBomb:** 3x3 area clear
- **LargeBomb:** 5x5 area clear  
- **ColorClean:** Removes all gems of one color

#### Obstacle.cs (Abstract)
Blocks that occupy cells and resist destruction.

```csharp
public class LockStateData
{
    public Sprite Sprite;          // Visual for this state
    public VisualEffect UndoneVFX; // Effect when state broken
}
public LockStateData[] LockState;  // Multi-stage obstacles
```

**Lifecycle:**
- `Init(Vector3Int cell)`: Register with board, set initial sprite
- `Damage(int amount)`: Progress through states
- `Clear()`: Called when fully destroyed

### 5. Match System

#### Match.cs
Represents a collection of matched gems pending deletion.

```csharp
public class Match
{
    public List<Vector3Int> MatchingGem;    // Grid positions of matched gems
    public Vector3Int OriginPoint;          // Where match originated
    public float DeletionTimer;             // Delay before deletion
    public BonusGem SpawnedBonus;           // Bonus gem to create
    public bool ForcedDeletion;             // From bonus (affects obstacles)
    public int DeletedCount;                // For coin spawning (4+ = coins)
}
```

**Match Detection Flow (in Board.cs):**
1. After swap or gem settle, check affected cells
2. `FindMatch(Vector3Int cell)`: Detect horizontal/vertical lines of 3+
3. Check if match pattern fits any BonusGem MatchShape
4. Create Match object, set `SpawnedBonus` if pattern matches
5. Add to `m_TickingMatch` list with timer
6. When timer expires, delete gems and spawn bonus

#### BoardCell.cs
Represents a single grid cell's state.

```csharp
public class BoardCell
{
    public Gem ContainingGem;      // Current gem in cell
    public Gem IncomingGem;        // Gem falling into cell
    public Obstacle Obstacle;      // Obstacle blocking cell
    public bool Locked;            // Cannot be modified
    
    // Computed properties
    public bool CanFall;           // Can accept falling gem
    public bool BlockFall;         // Blocks gems above from falling
    public bool CanBeMoved;        // Can participate in swap
}
```

**Neighbor System:**
```csharp
public static readonly Vector3Int[] Neighbours = {
    Vector3Int.up,
    Vector3Int.right,
    Vector3Int.down,
    Vector3Int.left
};
```

### 6. Input System

**Implementation:** Unity's new Input System (InputAction)

**Interaction Modes:**
1. **Swipe:** Drag from one gem to adjacent gem
2. **Double-tap:** Activate usable gem (BonusGem)
3. **Bonus Item Mode:** Click on target cell after selecting bonus item

**Input Flow:**
```
Player Input (touch/mouse)
    ↓
GameManager.ClickAction/ClickPosition
    ↓
Board.Update() processes input
    ↓
Convert screen position to grid cell
    ↓
Validate interaction (valid swap, usable gem, bonus target)
    ↓
Execute action (swap, use, apply bonus)
```

**Validation:**
- Swaps only allowed between adjacent cells
- Must create valid match OR involve usable gem
- Invalid swaps animate and return to original position
- Input disabled during animations and special states

### 7. Visual Effects & Audio

#### VFXPoolSystem.cs
Object pooling for Visual Effect Graph instances.

**Why Pooling:** VFX instantiation is expensive. Pool reuses instances for performance.

```csharp
public class VFXPoolSystem
{
    // Pre-instantiate VFX instances
    public void AddNewInstance(VisualEffect prefab, int count);
    
    // Get instance, move to position, play
    public VisualEffect PlayInstanceAt(VisualEffect prefab, Vector3 position);
    
    // Auto-disable VFX when particles finished (GPU event handling)
    public void Update();  // Called by GameManager
}
```

**VFX Lifecycle:**
1. Pre-instantiate pool on level start
2. `PlayInstanceAt()`: Dequeue → Position → Play
3. Auto-detect completion via `aliveParticleCount == 0`
4. Disable and return to pool

**Common VFX:**
- Match effects (per gem type)
- Coin collection animation
- Win/lose effects
- Bonus gem activation effects
- Hint indicator

#### Audio System (in GameManager)
**Music:** Dual AudioSource system for crossfading
```csharp
private AudioSource MusicSourceActive;      // Currently playing
private AudioSource MusicSourceBackground;  // Fade-in target
```

`SwitchMusic(AudioClip)`: Swaps sources and crossfades in Update loop.

**SFX:** Pool of 16 AudioSources (queue-based)
```csharp
private Queue<AudioSource> m_SFXSourceQueue;
```

`PlaySFX(AudioClip)`: Dequeue → Set clip → Play → Enqueue (circular buffer).

**Volume Management:**
```csharp
public class SoundData
{
    public float MainVolume;
    public float MusicVolume;
    public float SFXVolume;
}
```

- Persisted to JSON at `Application.persistentDataPath + "/sounds.json"`
- Applied to AudioMixer parameters (logarithmic scale)

### 8. Bonus Item System

**BonusItem.cs** (Abstract ScriptableObject)
Items usable during gameplay, displayed in bottom bar.

```csharp
public abstract class BonusItem : ScriptableObject
{
    public Sprite DisplaySprite;
    public bool NeedTarget;           // Requires player to click target cell
    public abstract void Use(Vector3Int target);
}
```

**Example: BonusGemBonusItem**
Allows using a BonusGem as consumable item.
```csharp
public BonusGem BonusGem;

public override void Use(Vector3Int target)
{
    // Create temporary gem instance at target
    var tempGem = Instantiate(BonusGem, position, Quaternion.identity);
    tempGem.Init(target);
    
    // Trigger bonus effect
    tempGem.BonusTriggerEffect();
    tempGem.Use(null, isBonus: true);
    
    // Cleanup
    Destroy(tempGem.gameObject);
}
```

**Inventory System (in GameManager):**
```csharp
public class BonusItemEntry
{
    public BonusItem Item;
    public int Amount;
}
public List<BonusItemEntry> BonusItems;
```

### 9. Settings & Configuration

#### GameSettings.cs
Central configuration ScriptableObject stored on GameManager prefab.

```csharp
public class GameSettings
{
    public float InactivityBeforeHint;  // Seconds before showing hint
    
    public VisualSetting VisualSettings;
    public BonusSetting BonusSettings;
    public ShopSetting ShopSettings;
    public SoundSetting SoundSettings;
}
```

**VisualSetting:**
- Fall speed, acceleration curves, bounce curves
- VFX prefabs (coin, win, lose, bonus mode, hint)
- Animation curves for UI effects

**BonusSetting:**
- Array of all BonusGem types in game
- Used for match pattern detection

**ShopSetting:**
- Array of purchasable items
- Base class `ShopItem` with `Buy()` method

**SoundSetting:**
- AudioMixer reference
- Audio prefabs (music/SFX sources)
- Sound clips (menu, swipe, fall, coin, win, lose, voices)

### 10. Shop System

**ShopItem** (Abstract, in GameSettings.cs)
```csharp
public abstract class ShopItem : ScriptableObject
{
    public Sprite ItemSprite;
    public string ItemName;
    public int Price;
    
    public virtual bool CanBeBought();  // Check affordability
    public abstract void Buy();         // Purchase logic
}
```

**Implementations:**
- **ShopItemCoin:** Purchase coins (usually for real money integration)
- **ShopItemLive:** Purchase extra lives
- **ShopItemBonusItem:** Purchase bonus items

### 11. UI System (UI Toolkit)

#### UIHandler.cs
Manages all UI using Unity's UI Toolkit (formerly UIElements).

**Key UI Screens:**
- **Game UI:** Goal display, move counter, bonus item bar
- **End Screen:** Win/lose overlay with results
- **Settings Menu:** Volume sliders, audio settings
- **Shop:** Purchasable items display
- **Debug Menu:** Development tools (F12, editor/dev builds only)

**Character Animation System:**
```csharp
public enum CharacterAnimation
{
    Match,    // Successful match
    Win,      // Level complete
    LowMove,  // Running out of moves
    Lose      // Level failed
}
```

Triggers Animator states for character reactions.

**UI Animation System:**
Handles flying gem/coin animations to goal counters.
```csharp
class UIAnimationEntry
{
    public VisualElement UIElement;   // Visual being animated
    public Vector3 WorldPosition;     // Start in world space
    public Vector3 StartToEnd;        // Animation vector
    public float Time;                // Animation progress
    public AnimationCurve Curve;      // Movement curve
    public AudioClip EndClip;         // Sound when reaching target
}
```

### 12. Camera System

**Dynamic Adjustment (in GameManager.ComputeCamera()):**
```csharp
public void ComputeCamera()
{
    // Center camera on board bounds
    var bounds = Board.Bounds;
    Vector3 center = Board.Grid.CellToLocalInterpolated(bounds.center);
    
    // Calculate orthographic size based on orientation
    if (Screen.height > Screen.width)
        halfSize = ((bounds.width + 1) * 0.5f + margin) * screenRatio;  // Portrait
    else
        halfSize = (bounds.height + 3) * 0.5f + margin;  // Landscape
    
    Camera.main.orthographicSize = halfSize;
}
```

Called when:
- Level starts
- Screen resolution/orientation changes (detected in LevelData.Update())

## Development Patterns & Best Practices

### Singleton Pattern Usage

**Two Types of Singletons:**

1. **Global Persistent (GameManager)**
   - Survives scene changes (`DontDestroyOnLoad`)
   - Manages cross-scene data
   - Auto-instantiates in editor for testing convenience

2. **Scene-Local (Board, UIHandler, LevelData)**
   - Destroyed with scene
   - Level-specific data
   - Cleaner memory management

**Implementation Pattern:**
```csharp
private static ClassName s_Instance;

public static ClassName Instance
{
    get
    {
#if UNITY_EDITOR
        // Editor: Auto-create if needed
        if (s_Instance == null && !s_IsShuttingDown)
            s_Instance = /* instantiate */;
#endif
        return s_Instance;
    }
}

private void Awake()
{
    s_Instance = this;
    // ... initialization
}

private void OnDestroy()
{
    if (s_Instance == this) 
        s_IsShuttingDown = true;  // Prevent re-creation during shutdown
}
```

### Observer Pattern (Events & Callbacks)

**Delegates in LevelData:**
```csharp
public delegate void GoalChangeDelegate(int gemType, int newAmount);
public delegate void MoveNotificationDelegate(int moveRemaining);

public Action OnAllGoalFinished;
public Action OnNoMoveLeft;
public GoalChangeDelegate OnGoalChanged;
public MoveNotificationDelegate OnMoveHappened;
```

**Cell-Specific Callbacks in Board:**
```csharp
private Dictionary<Vector3Int, Action> m_CellsCallbacks;      // On deletion
private Dictionary<Vector3Int, Action> m_MatchedCallback;     // On match

public static void RegisterDeletedCallback(Vector3Int cell, Action callback);
public static void RegisterMatchedCallback(Vector3Int cell, Action callback);
```

Used by obstacles and special tiles to react to events at specific positions.

### State Machine Pattern

**Gem States:**
```csharp
public enum State { Still, Falling, Bouncing, Disappearing }
```

Clear state transitions prevent invalid operations (e.g., can't swap falling gems).

**Board Swap Stages:**
```csharp
private enum SwapStage { None, Forward, Return }
```

Manages swap animation and rollback for invalid moves.

### Object Pooling

**Why:** Frequent instantiation/destruction causes:
- Garbage collection spikes (frame drops)
- Loading hitches

**What to Pool:**
- Visual Effects (VFXPoolSystem)
- Audio Sources (SFX queue in GameManager)
- UI Elements (for animations)

**Pattern:**
1. Pre-instantiate pool on initialization
2. Dequeue on use, enqueue when done
3. Auto-expand if pool exhausted
4. Disable instead of destroy

### Conditional Compilation

**Editor-Only Code:**
```csharp
#if UNITY_EDITOR
    // Editor tools, auto-instantiation, scene references
#endif
```

**Development Builds:**
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Debug menu, F12 tools, development features
#endif
```

**Build-Only Code:**
```csharp
#if !UNITY_EDITOR
    // Production optimizations, scene index loading
#endif
```

### Execution Order Management

**Critical Timings:**
```csharp
[DefaultExecutionOrder(-9999)]  // GameManager, Board - very early
[DefaultExecutionOrder(-9000)]  // UIHandler - after managers
[DefaultExecutionOrder(12000)]  // LevelData - after everything initialized
```

**Why:** Ensures dependencies exist before use (e.g., Board exists before LevelData needs it).

**Special Case:** Tilemap.StartUp() runs before Awake(), so static registration methods handle `s_Instance == null` with fallback: `GameObject.Find("Grid").GetComponent<Board>()`.

## Common Development Commands

### Unity Operations
```bash
# Open project in Unity
# Use Unity Hub or: /Applications/Unity/Hub/Editor/[version]/Unity.app/Contents/MacOS/Unity -projectPath [path]

# Build for WebGL (via Unity Editor)
# File > Build Settings > WebGL > Build

# Run tests
# Window > General > Test Runner
```

### CI/CD Pipeline
```bash
# Trigger build and deploy to itch.io
git push origin main

# Manual artifact download
# GitHub Actions > Latest Workflow > Artifacts
```

### Creating New Levels

**Process:**
1. **Create Scene:** Use level scene template (contains Grid, UI, Camera, LevelData)
2. **Configure LevelData:**
   - Set `MaxMove`, `Goals`, `Music`
   - Adjust `BorderMargin` if needed
3. **Design Board:**
   - Open Tile Palette (Window > 2D > Tile Palette)
   - Paint Logic tilemap with:
     - GemPlacerTile (normal cells)
     - GemSpawner (top of columns)
     - ObstaclePlacer (obstacles)
4. **Test:** Press Play in editor (GameManager auto-loads)
5. **Add to Build:** 
   - Add scene to LevelList ScriptableObject
   - Build system auto-updates Build Settings

**Tilemap Layers:**
- **Background Tilemap:** Visual decoration only
- **Logic Tilemap:** Game logic tiles (converted to data at runtime)

### Debug Features

**F12 Debug Menu** (Editor/Development builds only)
- Spawn any gem type at any position
- Test specific match patterns
- Verify bonus gem spawning

**Console Logging:**
Key systems log initialization and state changes. Check Console for:
- Board initialization (cell count, spawner positions)
- Match detection results
- VFX pool status

### Testing Approach

**Unit Testing:** Not extensively implemented (Unity Test Framework available)

**Playtesting Focus:**
- Level completion within move limit
- All bonus gem patterns spawn correctly
- No impossible board states (always has valid moves)
- Hint system works correctly
- UI updates properly on all events

## Platform-Specific Notes

### WebGL Target

**Optimizations:**
- Object pooling reduces GC pressure
- Conditional compilation removes debug code
- Tilemap refresh in builds ensures correct sprites

**Limitations:**
- No multi-threading (limits particle system complexity)
- AudioSource pool size tuned for web performance (16 sources)

**Build Settings:**
- Compression: Brotli or Gzip
- Memory size: Tuned based on level complexity
- Auto-graphics API (WebGL 2.0 preferred)

### Mobile Considerations

**Input:** Touch and mouse both supported via Input System

**UI:** Responsive camera adjusts to portrait/landscape

**Performance:**
- VFX complexity kept reasonable
- Draw call batching via sprite atlases
- Audio compression settings for mobile

## Common Modification Scenarios

### Adding New Gem Type

1. Create Gem prefab (set `GemType` to unique ID)
2. Add to `Board.ExistingGems` array in level scene
3. Add to `GameSettings.BonusSettings.Bonuses` if BonusGem
4. Create matching VisualEffect prefabs

### Adding New Bonus Gem

1. Extend `BonusGem` class
2. Implement `Use(Gem swappedGem, bool isBonus)` method
3. Define `Shapes` (MatchShape list) in editor
4. Add to GameSettings.BonusSettings.Bonuses array

**MatchShape Editor:** Custom inspector allows visual pattern design.

### Adding New Obstacle

1. Extend `Obstacle` class
2. Define `LockStateData[]` (sprites + VFX per state)
3. Implement `Clear()` for destruction behavior
4. Create ObstaclePlacer asset referencing prefab
5. Paint in level using Tile Palette

### Adding New Level

See "Creating New Levels" section above.

### Modifying UI

**UI Toolkit Workflow:**
1. Edit `.uxml` file (UI structure)
2. Edit `.uss` file (styling)
3. Update `UIHandler.cs` to query/manipulate elements
4. Test in Game view (UI Toolkit Debugger available)

**Common UI Elements:**
- `VisualElement`: Container/layout
- `Label`: Text display
- `Button`: Interactive button
- `Slider`: Value selection
- `Image`: Sprite display

### Modifying Audio

**Adding New Sound:**
1. Import AudioClip to project
2. Add field to `SoundSetting` in GameSettings.cs
3. Use `GameManager.Instance.PlaySFX(clip)` in code

**Adding New Music:**
1. Import AudioClip
2. Set in `LevelData.Music` for level-specific
3. Or use `GameManager.SwitchMusic(clip)` dynamically

## Performance Optimization Checklist

- [ ] VFX instances pooled (no runtime instantiation)
- [ ] Audio sources pooled (circular buffer)
- [ ] Sprite atlases used (auto-generated)
- [ ] Minimal Update() loops (only active systems)
- [ ] Board operations use Dictionary lookups (O(1))
- [ ] Match detection only on affected cells (not entire board)
- [ ] Conditional compilation removes debug code from builds
- [ ] Fixed timestep at 60 FPS (`Application.targetFrameRate = 60`)

## Architecture Decision Records

### Why Tilemap for Authoring?
**Decision:** Use Unity Tilemap purely for level design, convert to Dictionary at runtime.

**Rationale:**
- Visual, intuitive design workflow
- No manual coordinate entry
- Leverages Unity's built-in tools
- Runtime Dictionary more efficient than Tilemap queries

**Trade-offs:**
- Tilemap.StartUp timing issues (solved with fallback instantiation)
- Two representations (authoring vs runtime) add complexity

### Why Scene-Local Board Singleton?
**Decision:** Board is singleton but tied to scene lifecycle.

**Rationale:**
- Each level has unique configuration
- Automatic cleanup on scene change
- No manual data clearing needed
- Memory efficiency

**Trade-offs:**
- Two singleton patterns in same codebase (can confuse newcomers)
- Static reference changes each level (requires understanding)

### Why Property Instead of Method for Singleton?
**Decision:** `GameManager.Instance` is a property, not `GetInstance()` method.

**Rationale:**
- Cleaner syntax: `GameManager.Instance.Method()` vs `GameManager.GetInstance().Method()`
- Semantic meaning: "accessing a value" not "performing an action"
- Consistent with C# conventions
- Private setter enables encapsulation

### Why VFX Pooling Instead of On-Demand?
**Decision:** Pre-instantiate VFX pools, reuse instances.

**Rationale:**
- VFX instantiation expensive (shader compilation, GPU setup)
- Frequent spawn/destroy causes GC spikes
- Predictable memory usage
- Better frame pacing

**Trade-offs:**
- Initial memory overhead
- Pool size tuning needed per VFX type
- Complex lifetime management (auto-disable system)

## Known Issues & Limitations

1. **Tilemap StartUp Timing:** Tiles initialize before Awake(), requiring fallback instantiation. Not ideal but necessary for Tilemap integration.

2. **No Undo System:** Board state changes are irreversible. Would require complex state snapshotting.

3. **No Networked Multiplayer:** Architecture is single-player focused. Multiplayer would need significant refactoring.

4. **Limited Save System:** Only audio settings persist. Level progress/stars not saved (would need SaveGame system).

5. **Fixed Target Framerate:** Hardcoded to 60 FPS. Different targets require code change.

## Future Extension Points

**Designed for Extension:**
- New BonusGem types (inherit BonusGem)
- New Obstacle types (inherit Obstacle)
- New BonusItem types (inherit BonusItem)
- New ShopItem types (inherit ShopItem)
- New MatchShape patterns (add to BonusGem.Shapes)

**Requires Refactoring:**
- Networked multiplayer
- Procedural level generation (would need different authoring approach)
- 3D version (assumes 2D grid system)
- Different game modes (currently hard-coded goal system)

## Glossary

**Terms:**
- **Authoring:** Edit-time design process (vs runtime gameplay)
- **Board:** Grid-based game area containing gems
- **Cell:** Single grid position (Vector3Int coordinates)
- **Match:** Group of 3+ identical gems in line
- **Bonus Gem:** Special gem with activated ability
- **Bonus Item:** Consumable powerup from inventory
- **Obstacle:** Non-gem blocking element (crate, ice, etc.)
- **Spawner:** Top-of-board position where new gems appear
- **ScriptableObject:** Unity data asset (separate from scene)
- **Singleton:** Class with single instance, globally accessible
- **VFX:** Visual Effect Graph particle system
- **UI Toolkit:** Unity's modern UI system (formerly UIElements)
- **Tilemap:** Unity 2D grid-based editing system

## Quick Reference

**Singleton Access:**
```csharp
GameManager.Instance     // Global manager
Board.Instance          // Current level board (changes per scene)
UIHandler.Instance      // UI controller
LevelData.Instance      // Current level settings
```

**Common Patterns:**
```csharp
// Play VFX
GameManager.Instance.PoolSystem.PlayInstanceAt(vfxPrefab, position);

// Play Sound
GameManager.Instance.PlaySFX(audioClip);

// Get cell content
BoardCell cell = Board.Instance.CellContent[gridPosition];

// Register callback
Board.RegisterDeletedCallback(cellPosition, () => { /* ... */ });

// Update goal
LevelData.Instance.Matched(gem);
```

**File Locations:**
- Managers: `Assets/GemHunterMatch/Scripts/`
- Level scenes: `Assets/GemHunterMatch/Scenes/`
- Settings: `Assets/GemHunterMatch/Resources/GameManager.prefab`
- UI Documents: `Assets/GemHunterMatch/UI/`
- Authoring tiles: `Assets/GemHunterMatch/Tiles/`

---

**Document Version:** 2.0  
**Last Updated:** 2025  
**Maintained By:** Project development team
