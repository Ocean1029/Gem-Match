using UnityEngine;
using UnityEngine.Tilemaps;

namespace Match3
{
    /// <summary>
    /// Tilemap tile used for placing enemies in the level editor.
    /// Similar to ObstaclePlacer but specifically for Enemy instances.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPlacer", menuName = "2D Match/Tile/Enemy Placer")]
    public class EnemyPlacer : TileBase
    {
        [Header("Editor Preview")]
        public Sprite PreviewEditorSprite;
        
        [Header("Enemy Configuration")]
        public Enemy EnemyPrefab;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            // Show preview sprite in editor, hide in play mode
            tileData.sprite = !Application.isPlaying ? PreviewEditorSprite : null;
        }

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
#if UNITY_EDITOR
            // Don't instantiate in editor mode, only at runtime
            if (!Application.isPlaying)
                return false;
#endif

            if (EnemyPrefab == null)
            {
                Debug.LogError($"EnemyPlacer at position {position} has no Enemy prefab assigned!");
                return false;
            }

            // Get the grid and board references (handling early initialization like Board.RegisterCell does)
            var gridObject = GameObject.Find("Grid");
            var grid = gridObject.GetComponent<Grid>();
            var board = gridObject.GetComponent<Board>();
            
            // Register the cell first (similar to how Obstacle and Gem placers work)
            Board.RegisterCell(position);
            
            // Instantiate the enemy at the correct world position (like Board.NewGemAt does)
            var newEnemy = Instantiate(EnemyPrefab, grid.GetCellCenterWorld(position), Quaternion.identity);
            
            // Set the enemy as the containing gem before Init (like Board.NewGemAt does)
            board.CellContent[position].ContainingGem = newEnemy;
            
            // Initialize the enemy (it won't need to set position or CellContent now)
            newEnemy.Init(position);

            return true;
        }
    }
}

