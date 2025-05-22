using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// ステージデータのScriptableObject
    /// BDD仕様: spec/data/stage.md
    /// </summary>
    [CreateAssetMenu(fileName = "New Stage Data", menuName = "RunGame/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Stage Model")]
        [SerializeField] private Mesh mapModel;
        
        [Header("Stage Configuration")]
        [SerializeField] private int blockSize = 100;
        [SerializeField] private int laneNum = 3;
        
        [Header("Placement Data")]
        [SerializeField] private int[] placements;

        #region Properties

        /// <summary>
        /// マップのモデル参照
        /// </summary>
        public Mesh MapModel => mapModel;

        /// <summary>
        /// ブロックサイズ（m）
        /// </summary>
        public int BlockSize => blockSize;

        /// <summary>
        /// レーン数
        /// </summary>
        public int LaneNum => laneNum;

        /// <summary>
        /// 配置物の設置情報
        /// BDD仕様: int[BlockSize*LaneNum] Placements
        /// </summary>
        public int[] Placements => placements;

        /// <summary>
        /// 配置データの総サイズ
        /// </summary>
        public int TotalGridSize => blockSize * laneNum;

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定位置の配置物IDを取得
        /// </summary>
        /// <param name="distance">距離（m）</param>
        /// <param name="lane">レーン番号（0-2）</param>
        /// <returns>配置物ID</returns>
        public int GetPlacementId(int distance, int lane)
        {
            if (placements == null || placements.Length == 0)
                return 0;

            // 距離とレーンからインデックスを計算
            int index = (distance % blockSize) * laneNum + lane;
            
            if (index >= 0 && index < placements.Length)
            {
                return placements[index];
            }
            
            return 0; // None
        }

        /// <summary>
        /// 指定範囲の配置物データを取得
        /// </summary>
        /// <param name="startDistance">開始距離</param>
        /// <param name="endDistance">終了距離</param>
        /// <param name="lane">レーン番号</param>
        /// <returns>配置物IDの配列</returns>
        public int[] GetPlacementRange(int startDistance, int endDistance, int lane)
        {
            int count = endDistance - startDistance + 1;
            int[] result = new int[count];
            
            for (int i = 0; i < count; i++)
            {
                result[i] = GetPlacementId(startDistance + i, lane);
            }
            
            return result;
        }

        /// <summary>
        /// 指定位置に配置物があるかどうか
        /// </summary>
        /// <param name="distance">距離</param>
        /// <param name="lane">レーン番号</param>
        /// <returns>配置物があればtrue</returns>
        public bool HasPlacement(int distance, int lane)
        {
            return GetPlacementId(distance, lane) != 0;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // パラメータの検証
            if (blockSize <= 0) blockSize = 100;
            if (laneNum <= 0) laneNum = 3;
            
            // 配置データのサイズ調整
            int requiredSize = blockSize * laneNum;
            if (placements == null || placements.Length != requiredSize)
            {
                System.Array.Resize(ref placements, requiredSize);
                Debug.Log($"Resized placements array to {requiredSize} elements");
            }
        }

        [ContextMenu("Initialize Placement Data")]
        private void InitializePlacementData()
        {
            int requiredSize = blockSize * laneNum;
            placements = new int[requiredSize];
            
            // すべて0（None）で初期化
            for (int i = 0; i < placements.Length; i++)
            {
                placements[i] = 0;
            }
            
            Debug.Log($"Initialized placement data with {requiredSize} elements");
        }

        [ContextMenu("Generate Sample Data")]
        private void GenerateSampleData()
        {
            InitializePlacementData();
            
            // サンプルデータの生成
            for (int distance = 0; distance < blockSize; distance++)
            {
                for (int lane = 0; lane < laneNum; lane++)
                {
                    // 10%の確率でコイン、5%の確率で障害物
                    float rand = Random.value;
                    int placementId = 0;
                    
                    if (rand < 0.05f) // 5% - 障害物
                    {
                        placementId = 2; // BasicObstacle
                    }
                    else if (rand < 0.15f) // 10% - コイン
                    {
                        placementId = 1; // CoinItem
                    }
                    
                    int index = distance * laneNum + lane;
                    if (index < placements.Length)
                    {
                        placements[index] = placementId;
                    }
                }
            }
            
            Debug.Log("Generated sample placement data");
        }

        #endregion
    }
}