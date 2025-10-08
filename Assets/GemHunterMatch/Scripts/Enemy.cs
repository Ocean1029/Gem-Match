using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Match3
{
    /// <summary>
    /// Enemy class that occupies cells on the board, moves after certain turns, and takes damage from bonus gems.
    /// Inherits from Gem to integrate with the existing board system.
    /// </summary>
    public class Enemy : Gem
    {
        [Header("Enemy Stats")]
        public int MaxHealth = 3;
        public int MovesBeforeFirstMove = 3;  // Number of player moves before enemy starts moving
        public int MoveInterval = 2;          // Number of player moves between enemy movements
        
        [Header("Visual")]
        public Sprite[] HealthStates;         // Different sprites for different health states
        
        [Header("Effects")]
        public AudioClip DamagedClip;
        public AudioClip MovedClip;
        public VisualEffect DamageEffect;
        public VisualEffect DeathEffect;
        public VisualEffect MoveEffect;

        // Internal state
        private SpriteRenderer m_Renderer;
        private int m_MovesCounter = 0;       // Tracks player moves
        private bool m_HasMoved = false;      // Whether enemy has moved at least once

        private void Awake()
        {
            m_CanMove = false;  // Enemies don't fall with gravity
            m_Renderer = GetComponent<SpriteRenderer>();
            m_HitPoints = MaxHealth;
        }

        public override void Init(Vector3Int startIdx)
        {
            base.Init(startIdx);
            
            // Register VFX instances in pool (similar to how Crate does it)
            if (DamageEffect != null)
                GameManager.Instance.PoolSystem.AddNewInstance(DamageEffect, 4);
            if (DeathEffect != null)
                GameManager.Instance.PoolSystem.AddNewInstance(DeathEffect, 4);
            if (MoveEffect != null)
                GameManager.Instance.PoolSystem.AddNewInstance(MoveEffect, 4);
            
            UpdateVisualState();
            
            // Register to board's enemy tracking system
            Board.RegisterEnemy(this);
        }

        public override bool Damage(int damage)
        {
            if (m_HitPoints <= 0)
                return false;
                
            GameManager.Instance.PlaySFX(DamagedClip);
            
            if (DamageEffect != null)
                GameManager.Instance.PoolSystem.PlayInstanceAt(DamageEffect, transform.position);
            
            var stillAlive = base.Damage(damage);
            UpdateVisualState();
            
            if (!stillAlive)
            {
                OnDeath();
            }
            
            return stillAlive;
        }

        /// <summary>
        /// Called by Board when a player makes a move
        /// </summary>
        public void OnPlayerMove()
        {
            m_MovesCounter++;
            
            // Check if it's time to move
            bool shouldMove = false;
            if (!m_HasMoved && m_MovesCounter >= MovesBeforeFirstMove)
            {
                shouldMove = true;
                m_HasMoved = true;
            }
            else if (m_HasMoved && m_MovesCounter >= MoveInterval)
            {
                shouldMove = true;
            }
            
            if (shouldMove)
            {
                m_MovesCounter = 0;
                AttemptMove();
            }
        }

        /// <summary>
        /// Attempts to move the enemy to an adjacent cell
        /// </summary>
        private void AttemptMove()
        {
            List<Vector3Int> validMoves = new List<Vector3Int>();
            
            // Check all adjacent cells for valid move positions
            foreach (var direction in BoardCell.Neighbours)
            {
                Vector3Int targetPos = m_CurrentIndex + direction;
                
                if (GameManager.Instance.Board.CellContent.TryGetValue(targetPos, out var cell))
                {
                    // Enemy can move to empty cells or cells with regular gems (not usable, not in a match)
                    if (cell.ContainingGem == null || 
                        (cell.ContainingGem.CanMove && 
                         !cell.ContainingGem.Usable && 
                         cell.ContainingGem.CurrentMatch == null &&
                         cell.ContainingGem.CurrentState == Gem.State.Still))
                    {
                        validMoves.Add(targetPos);
                    }
                }
            }
            
            // If there are valid moves, pick one randomly
            if (validMoves.Count > 0)
            {
                Vector3Int targetCell = validMoves[Random.Range(0, validMoves.Count)];
                MoveToCell(targetCell);
            }
        }

        /// <summary>
        /// Moves the enemy to a target cell
        /// </summary>
        private void MoveToCell(Vector3Int targetCell)
        {
            Debug.Log($"[Enemy] Moving from {m_CurrentIndex} to {targetCell}");
            
            var board = GameManager.Instance.Board;
            var targetCellData = board.CellContent[targetCell];
            var currentCellData = board.CellContent[m_CurrentIndex];
            
            // Destroy gem at target position if it exists (using Board's system)
            if (targetCellData.ContainingGem != null && targetCellData.ContainingGem != this)
            {
                var targetGem = targetCellData.ContainingGem;
                Debug.Log($"[Enemy] Target cell has gem: {targetGem.name}, eating it");
                
                // Play match effects
                if (targetGem.MatchEffectPrefabs != null && targetGem.MatchEffectPrefabs.Length > 0)
                {
                    foreach (var vfx in targetGem.MatchEffectPrefabs)
                    {
                        GameManager.Instance.PoolSystem.PlayInstanceAt(vfx, targetGem.transform.position);
                    }
                }
                
                // Disable the gem immediately (safer than Destroy)
                targetGem.gameObject.SetActive(false);
                targetGem.Destroyed();
                
                // Clear cell
                targetCellData.ContainingGem = null;
                
                // Schedule actual destruction on next frame to avoid reference issues
                StartCoroutine(DestroyGemNextFrame(targetGem.gameObject));
            }
            
            // Remove enemy from current cell
            Debug.Log($"[Enemy] Clearing old cell {m_CurrentIndex}");
            Vector3Int oldCell = m_CurrentIndex;
            currentCellData.ContainingGem = null;
            
            // Update to new position
            Debug.Log($"[Enemy] Setting new cell {targetCell}");
            targetCellData.ContainingGem = this;
            m_CurrentIndex = targetCell;
            
            // Mark the old cell as empty so Board will refill it
            Board.MarkCellAsEmpty(oldCell);
            
            // Play effects
            if (MovedClip != null)
                GameManager.Instance.PlaySFX(MovedClip);
            if (MoveEffect != null)
                GameManager.Instance.PoolSystem.PlayInstanceAt(MoveEffect, transform.position);
            
            // Animate movement
            Vector3 targetPosition = board.Grid.GetCellCenterWorld(targetCell);
            StartCoroutine(AnimateMovement(targetPosition));
        }
        
        private System.Collections.IEnumerator DestroyGemNextFrame(GameObject gem)
        {
            yield return null;
            if (gem != null)
            {
                Destroy(gem);
            }
        }

        private System.Collections.IEnumerator AnimateMovement(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            
            transform.position = targetPosition;
        }

        private void OnDeath()
        {
            Debug.Log($"[Enemy] OnDeath called at position {m_CurrentIndex}");
            
            if (DeathEffect != null)
                GameManager.Instance.PoolSystem.PlayInstanceAt(DeathEffect, transform.position);
            
            // Clear the cell reference before destruction
            var board = GameManager.Instance.Board;
            if (board != null && board.CellContent.ContainsKey(m_CurrentIndex))
            {
                var cell = board.CellContent[m_CurrentIndex];
                if (cell.ContainingGem == this)
                {
                    cell.ContainingGem = null;
                    Debug.Log($"[Enemy] Cleared cell {m_CurrentIndex} on death");
                    
                    // Mark this cell as empty so Board will refill it
                    Board.MarkCellAsEmpty(m_CurrentIndex);
                }
            }
            
            // Notify LevelData that an enemy was defeated
            LevelData.Instance.EnemyDefeated();
            
            // Unregister from board
            Board.UnregisterEnemy(this);
            
            // Hide the sprite immediately (like other gems do)
            gameObject.SetActive(false);
            
            // Destroy the GameObject after a short delay to ensure all systems can handle it
            Destroy(gameObject, 0.1f);
        }

        private void UpdateVisualState()
        {
            if (HealthStates == null || HealthStates.Length == 0)
                return;
                
            float healthRatio = m_HitPoints / (float)MaxHealth;
            int stateIndex = Mathf.RoundToInt((1.0f - healthRatio) * (HealthStates.Length - 1));
            stateIndex = Mathf.Clamp(stateIndex, 0, HealthStates.Length - 1);
            
            if (m_Renderer != null && HealthStates[stateIndex] != null)
                m_Renderer.sprite = HealthStates[stateIndex];
        }

        private void OnDestroy()
        {
            // Final cleanup: make sure the cell is cleared
            var board = GameManager.Instance?.Board;
            if (board != null && board.CellContent.ContainsKey(m_CurrentIndex))
            {
                var cell = board.CellContent[m_CurrentIndex];
                if (cell.ContainingGem == this)
                {
                    cell.ContainingGem = null;
                    Debug.Log($"[Enemy] Final cleanup: cleared cell {m_CurrentIndex} on destroy");
                }
            }
            
            // Unregister from board when destroyed
            Board.UnregisterEnemy(this);
        }
    }
}

