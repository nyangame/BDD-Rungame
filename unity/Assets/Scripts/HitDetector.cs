using UnityEngine;
using System.Collections.Generic;

namespace RunGame
{
    /// <summary>
    /// グリッドベースの当たり判定クラス
    /// BDD仕様: spec/code/HitDetector.md
    /// </summary>
    public class HitDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 2f;
        
        // コンポーネント参照
        private StageBehaviour stageBehaviour;
        private Player player;
        private GameManager gameManager;
        
        // 判定状態管理
        private Vector2Int previousPlayerCell = new Vector2Int(-1, -1);
        private Dictionary<int, IPlacementObject> placementObjectCache = new Dictionary<int, IPlacementObject>();

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// あたりを確認する
        /// BDD仕様: HitCheck: あたりを確認する
        /// </summary>
        public void HitCheck()
        {
            if (player == null || stageBehaviour == null) return;

            // BDD仕様: StageBehaviourから現在のセルを取得
            Vector2Int currentPlayerCell = GetCurrentPlayerCell();
            
            // BDD仕様: 記録していた過去のセルと現在のセルを比較し、移動したマスを取得する
            if (HasPlayerMoved(currentPlayerCell))
            {
                ProcessMovedCells(previousPlayerCell, currentPlayerCell);
                previousPlayerCell = currentPlayerCell;
            }
        }

        /// <summary>
        /// プレイヤーの攻撃イベント処理
        /// </summary>
        public void OnPlayerAttack()
        {
            Vector2Int playerCell = GetCurrentPlayerCell();
            ProcessAttackHit(playerCell);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            stageBehaviour = FindObjectOfType<StageBehaviour>();
            player = FindObjectOfType<Player>();
            gameManager = FindObjectOfType<GameManager>();
            
            if (stageBehaviour == null)
                Debug.LogError("StageBehaviour not found!");
            if (player == null)
                Debug.LogError("Player not found!");
            if (gameManager == null)
                Debug.LogError("GameManager not found!");
                
            Debug.Log("HitDetector initialized");
        }

        /// <summary>
        /// 現在のプレイヤーセルを取得
        /// BDD仕様: 今いるプレイヤー位置のグリッドで評価
        /// </summary>
        /// <returns>プレイヤーの現在セル座標</returns>
        private Vector2Int GetCurrentPlayerCell()
        {
            if (player == null || stageBehaviour == null)
                return new Vector2Int(-1, -1);
            
            // 距離の計算
            int distance = Mathf.FloorToInt(stageBehaviour.TotalDistance);
            
            // レーンの取得
            int lane = player.CurrentLane;
            
            return new Vector2Int(distance, lane);
        }

        /// <summary>
        /// プレイヤーが移動したかどうか判定
        /// </summary>
        /// <param name="currentCell">現在のセル</param>
        /// <returns>移動していればtrue</returns>
        private bool HasPlayerMoved(Vector2Int currentCell)
        {
            return previousPlayerCell != currentCell;
        }

        /// <summary>
        /// 移動したセルの処理
        /// BDD仕様: Playerの速度が1f1mを超えるときを想定して、移動は全てのグリッドを通過するような処理
        /// </summary>
        /// <param name="fromCell">移動前のセル</param>
        /// <param name="toCell">移動後のセル</param>
        private void ProcessMovedCells(Vector2Int fromCell, Vector2Int toCell)
        {
            // 初回の場合は現在のセルのみ処理
            if (fromCell.x < 0)
            {
                ProcessCellHit(toCell);
                return;
            }
            
            // 距離の移動量を計算
            int distanceDelta = toCell.x - fromCell.x;
            
            // 高速移動時に中間のセルも処理
            if (distanceDelta > 1)
            {
                // 中間のセルをすべて処理
                for (int d = fromCell.x + 1; d <= toCell.x; d++)
                {
                    Vector2Int intermediateCell = new Vector2Int(d, toCell.y);
                    ProcessCellHit(intermediateCell);
                }
            }
            else
            {
                // 通常移動の場合は現在のセルのみ処理
                ProcessCellHit(toCell);
            }
        }

        /// <summary>
        /// 指定セルでの衝突処理
        /// BDD仕様: 配置物があれば、IPlacementObject.Action()を呼び出す
        /// </summary>
        /// <param name="cell">処理するセル</param>
        private void ProcessCellHit(Vector2Int cell)
        {
            // セルの配置物情報を取得
            int placementId = stageBehaviour.GetPlacementAt(cell.x, cell.y);
            
            if (placementId == 0) return; // 配置物なし
            
            // 配置物オブジェクトを取得
            IPlacementObject placementObject = GetPlacementObject(placementId);
            if (placementObject == null) return;
            
            // プレイヤーのアクション状態を考慮した判定
            if (ShouldProcessHit(placementObject))
            {
                Debug.Log($"Hit detected at cell ({cell.x}, {cell.y}) with placement ID: {placementId}");
                
                // BDD仕様: IPlacementObject.Action()を呼び出す
                placementObject.Action();
                
                // 追加処理
                ProcessHitResult(placementObject);
            }
        }

        /// <summary>
        /// 攻撃時の衝突処理
        /// </summary>
        /// <param name="cell">攻撃対象のセル</param>
        private void ProcessAttackHit(Vector2Int cell)
        {
            int placementId = stageBehaviour.GetPlacementAt(cell.x, cell.y);
            if (placementId == 0) return;
            
            IPlacementObject placementObject = GetPlacementObject(placementId);
            if (placementObject == null) return;
            
            // 攻撃で倒せるオブジェクト（敵など）のみ処理
            if (placementObject.ObjType == PlacementObjectType.DamageObject)
            {
                Debug.Log($"Attack hit enemy at cell ({cell.x}, {cell.y})");
                placementObject.Action();
                
                // 敵を倒した場合のスコア加算など
                if (gameManager != null)
                {
                    gameManager.AddScore(100); // 敵撃破ボーナス
                }
            }
        }

        /// <summary>
        /// 配置物オブジェクトを取得
        /// </summary>
        /// <param name="placementId">配置物ID</param>
        /// <returns>配置物オブジェクト</returns>
        private IPlacementObject GetPlacementObject(int placementId)
        {
            // キャッシュから取得を試行
            if (placementObjectCache.TryGetValue(placementId, out IPlacementObject cachedObject))
            {
                return cachedObject;
            }
            
            // 新しく生成または検索
            IPlacementObject placementObject = CreatePlacementObject(placementId);
            if (placementObject != null)
            {
                placementObjectCache[placementId] = placementObject;
            }
            
            return placementObject;
        }

        /// <summary>
        /// 配置物オブジェクトの生成
        /// </summary>
        /// <param name="placementId">配置物ID</param>
        /// <returns>生成された配置物オブジェクト</returns>
        private IPlacementObject CreatePlacementObject(int placementId)
        {
            // TODO: StageCreatorまたはObjectPoolから実際のオブジェクトを取得
            // 現在は仮実装として基本的なオブジェクトを生成
            
            switch (placementId)
            {
                case 1: // CoinItem
                    // TODO: CoinItemのインスタンスを取得
                    Debug.Log("CoinItem detected (placeholder)");
                    return null;
                    
                case 2: // BasicObstacle
                    // TODO: Obstacleのインスタンスを取得
                    Debug.Log("Obstacle detected (placeholder)");
                    return null;
                    
                case 3: // BasicEnemy
                    // TODO: Enemyのインスタンスを取得
                    Debug.Log("Enemy detected (placeholder)");
                    return null;
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// ヒット処理を実行すべきかどうか判定
        /// </summary>
        /// <param name="placementObject">配置物オブジェクト</param>
        /// <returns>処理すべきならtrue</returns>
        private bool ShouldProcessHit(IPlacementObject placementObject)
        {
            if (player == null) return true;
            
            // プレイヤーのアクション状態を考慮
            var playerAction = player.CurrentAction;
            
            // ジャンプ中の場合の特別処理
            if (playerAction is JumpAction jumpAction)
            {
                // 空中配置物はジャンプ中のみ取得可能
                // 地上配置物はジャンプ中は当たらない
                bool isJumping = jumpAction.IsInJumpState();
                
                // TODO: 配置物が空中にあるかどうかの判定
                // 現在は全ての配置物を地上として扱う
                return !isJumping;
            }
            
            return true;
        }

        /// <summary>
        /// ヒット結果の処理
        /// </summary>
        /// <param name="placementObject">ヒットした配置物</param>
        private void ProcessHitResult(IPlacementObject placementObject)
        {
            switch (placementObject.ObjType)
            {
                case PlacementObjectType.SafeObject:
                    // アイテム取得など
                    Debug.Log("Safe object collected");
                    break;
                    
                case PlacementObjectType.DamageObject:
                    // ダメージ処理
                    Debug.Log("Damage object hit - Game Over");
                    if (gameManager != null)
                    {
                        gameManager.GameOver();
                    }
                    break;
            }
        }

        #endregion

        #region Editor Support

        private void OnDrawGizmosSelected()
        {
            if (player == null) return;
            
            // 現在のプレイヤーセルを可視化
            Vector2Int currentCell = GetCurrentPlayerCell();
            Vector3 cellWorldPos = new Vector3(
                (currentCell.y - 1) * 2f, // レーン位置
                0.5f,
                -currentCell.x
            );
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(cellWorldPos, new Vector3(1.8f, 1f, 0.8f));
            
            // 判定範囲の可視化
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.Position, detectionRange);
        }

        #endregion
    }
}