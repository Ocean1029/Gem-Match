using System;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Contains all the data for the Level in which this is : Goals and max number of Moves. This will also  notify the
    /// GameManager that we loaded a level
    /// </summary>
    [DefaultExecutionOrder(12000)]
    public class LevelData : MonoBehaviour
    {
        public static LevelData Instance { get; private set; }
    
        [Serializable]
        public class GemGoal
        {
            public Gem Gem;
            public int Count;
        }
        
        [Serializable]
        public class EnemyGoal
        {
            public int Count; // Number of enemies to defeat
            public Enemy EnemyPrefab; // Reference to enemy prefab (will use its sprite for UI)
            public Sprite UISprite; // Optional: Override sprite for UI (if null, uses EnemyPrefab's sprite)
            public int GemType = -1; // Gem type ID for enemy (use -1 to avoid conflicts with regular gems)
        }

        public string LevelName = "Level";
        public int MaxMove;
        public int LowMoveTrigger = 10;
        public GemGoal[] Goals;
        public EnemyGoal EnemyGoals; // Enemy defeat objective
        
        [Header("Visuals")]
        public float BorderMargin = 0.3f;
        public SpriteRenderer Background;
        
        [Header("Audio")] 
        public AudioClip Music;

        public delegate void GoalChangeDelegate(int gemType,int newAmount);
        public delegate void MoveNotificationDelegate(int moveRemaining);

        public Action OnAllGoalFinished;
        public Action OnNoMoveLeft;
    
        public GoalChangeDelegate OnGoalChanged;
        public MoveNotificationDelegate OnMoveHappened;

        public int RemainingMove { get; private set; }
        public int GoalLeft { get; private set; }

        private int m_StartingWidth;
        private int m_StartingHeight;

        private void Awake()
        {
            Instance = this;
            RemainingMove = MaxMove;
            GoalLeft = Goals.Length;
            
            Debug.Log($"[LevelData] Awake - Initial GoalLeft from Goals: {GoalLeft}");
            
            // Add enemy goals to total goal count if they exist
            if (EnemyGoals != null && EnemyGoals.Count > 0)
            {
                GoalLeft += 1; // Enemy goal counts as one overall goal
                Debug.Log($"[LevelData] Awake - Enemy goal added, new GoalLeft: {GoalLeft}");
            }
            
            Debug.Log($"[LevelData] Awake - Final GoalLeft: {GoalLeft}");
            
            GameManager.Instance.StartLevel();
        }

        void Start()
        {
            m_StartingWidth = Screen.width;
            m_StartingHeight = Screen.height;

            if (Background != null)
                Background.gameObject.SetActive(false);
        }

        void Update()
        {
            //to detect device orientation change or resolution change, we check if the screen change since since init
            //and recompute camera zoom
            if (Screen.width != m_StartingWidth || Screen.height != m_StartingHeight)
            {
                GameManager.Instance.ComputeCamera();
            }
        }

        public bool Matched(Gem gem)
        {
            foreach (var goal in Goals)
            {
                if (goal.Gem.GemType == gem.GemType)
                {
                    if (goal.Count == 0)
                        return false;
                
                    UIHandler.Instance.AddMatchEffect(gem);
                
                    goal.Count -= 1;
                    OnGoalChanged?.Invoke(gem.GemType, goal.Count);

                    if (goal.Count == 0)
                    {
                        GoalLeft -= 1;
                        Debug.Log($"[LevelData] Gem goal {gem.GemType} completed! GoalLeft is now: {GoalLeft}");
                        
                        if (GoalLeft == 0)
                        {
                            Debug.Log("[LevelData] ALL GOALS COMPLETED! Triggering victory!");
                            GameManager.Instance.WinStar();
                            GameManager.Instance.Board.ToggleInput(false);
                            OnAllGoalFinished?.Invoke();
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public void DarkenBackground(bool darken)
        {
            if (Background == null)
                return;

            Background.gameObject.SetActive(darken);
        }

        public void Moved()
        {
            var prev = RemainingMove;
        
        
            RemainingMove = Mathf.Max(0, RemainingMove - 1);
            OnMoveHappened?.Invoke(RemainingMove);

            if (prev > LowMoveTrigger && RemainingMove <= LowMoveTrigger)
            {
                UIHandler.Instance.TriggerCharacterAnimation(UIHandler.CharacterAnimation.LowMove);
            }

            if (RemainingMove <= 0)
            {
                OnNoMoveLeft();
            }
        }
        
        /// <summary>
        /// Called when an enemy is defeated
        /// </summary>
        public void EnemyDefeated()
        {
            if (EnemyGoals == null || EnemyGoals.Count <= 0)
                return;
            
            EnemyGoals.Count -= 1;
            
            Debug.Log($"[LevelData] Enemy defeated! Remaining: {EnemyGoals.Count}, GemType: {EnemyGoals.GemType}");
            
            // Notify UI about enemy goal progress
            // Use the GemType defined in EnemyGoals
            OnGoalChanged?.Invoke(EnemyGoals.GemType, EnemyGoals.Count);
            
            if (EnemyGoals.Count == 0)
            {
                GoalLeft -= 1;
                Debug.Log($"[LevelData] All enemies defeated! GoalLeft is now: {GoalLeft}");
                
                if (GoalLeft == 0)
                {
                    Debug.Log("[LevelData] ALL GOALS COMPLETED! Triggering victory!");
                    GameManager.Instance.WinStar();
                    GameManager.Instance.Board.ToggleInput(false);
                    OnAllGoalFinished?.Invoke();
                }
                else
                {
                    Debug.Log($"[LevelData] Still have {GoalLeft} goals remaining. Victory not triggered yet.");
                }
            }
        }
    }
}