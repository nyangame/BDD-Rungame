using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace RunGame
{
    /// <summary>
    /// ステージを生成するクラス
    /// BDD仕様: spec/code/StageCreator.md
    /// </summary>
    public class StageCreator : MonoBehaviour
    {
        [Header("Stage Settings")]
        [SerializeField] private int generateAheadDistance = 300; // 先読み生成距離
        [SerializeField] private int maxActiveBlocks = 5; // 同時に存在する最大ブロック数

        [Header("Addressable Settings")]
        [SerializeField] private string stageDataLabel = "StageData";
        [SerializeField] private List<AssetReference> stageBlockPrefabs = new List<AssetReference>();

        // ステージデータ管理
        private List<StageData> loadedStageData = new List<StageData>();
        private Dictionary<int, StageData> stageDataMap = new Dictionary<int, StageData>();

        // ステージブロック管理
        private Queue<GameObject> activeStageBlocks = new Queue<GameObject>();
        private Dictionary<int, GameObject> stageBlockInstances = new Dictionary<int, GameObject>();

        // オブジェクトプール管理
        private Dictionary<int, ObjectPool<GameObject>> placementObjectPools = new Dictionary<int, ObjectPool<GameObject>>();
        private Dictionary<int, GameObject> placementObjectPrefabs = new Dictionary<int, GameObject>();

        // 生成状態管理
        private float lastPlayerDistance = 0f;
        private int lastGeneratedBlock = -1;

        #region Properties

        /// <summary>
        /// 読み込み完了済みかどうか
        /// </summary>
        public bool IsLoaded => loadedStageData.Count > 0;

        #endregion

        #region Unity Lifecycle

        private async void Start()
        {
            // BDD仕様: StartのタイミングでステージブロックのPrefabデータを読み込む
            await LoadStageBlockData();
        }

        private void Update()
        {
            // BDD仕様: プレイヤーの距離が一定以上進んでいた場合に次のステージブロックを作る
            // この処理はStageBehaviourからのUpdatePlayerDistanceで実行される
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// プレイヤー距離の更新通知
        /// </summary>
        /// <param name="playerDistance">プレイヤーの現在距離</param>
        public void UpdatePlayerDistance(float playerDistance)
        {
            lastPlayerDistance = playerDistance;

            // 先読み生成の判定
            int currentBlock = Mathf.FloorToInt(playerDistance / 100f); // 100mブロック単位
            int generateUntilBlock = currentBlock + Mathf.CeilToInt(generateAheadDistance / 100f);

            // 新しいブロックが必要かチェック
            if (generateUntilBlock > lastGeneratedBlock)
            {
                GenerateStageBlocks(lastGeneratedBlock + 1, generateUntilBlock);
                lastGeneratedBlock = generateUntilBlock;
            }

            // 古いブロックの削除
            CleanupOldBlocks(currentBlock);
        }

        /// <summary>
        /// 指定位置の配置物IDを取得
        /// </summary>
        /// <param name="distance">距離</param>
        /// <param name="lane">レーン番号</param>
        /// <returns>配置物ID</returns>
        public int GetPlacementAt(int distance, int lane)
        {
            int blockIndex = distance / 100; // 100mブロック単位

            if (stageDataMap.TryGetValue(blockIndex, out StageData stageData))
            {
                return stageData.GetPlacementId(distance, lane);
            }

            return 0; // None
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ステージブロックデータの読み込み
        /// BDD仕様: PrefabデータはAddressablesにより管理されている
        /// </summary>
        private async UniTask LoadStageBlockData()
        {
            try
            {
                Debug.Log("Loading stage block data...");

                // Addressablesからステージデータを読み込み
                var loadHandle = Addressables.LoadAssetsAsync<StageData>(stageDataLabel, null);
                var stageDataList = await loadHandle.ToUniTask();

                loadedStageData.AddRange(stageDataList);

                // プールの初期化
                await InitializeObjectPools();

                Debug.Log($"Loaded {loadedStageData.Count} stage data assets");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load stage data: {e.Message}");

                // フォールバック: デフォルトステージデータを作成
                CreateFallbackStageData();
            }
        }

        /// <summary>
        /// オブジェクトプールの初期化
        /// BDD仕様: 配置物の管理にはObject Poolを使用すること
        /// </summary>
        private async UniTask InitializeObjectPools()
        {
            // 配置物プレハブの読み込み（仮実装）
            // TODO: 実際のプレハブをAddressablesから読み込み
            await LoadPlacementObjectPrefabs();

            // 各配置物タイプ用のプールを作成
            foreach (var kvp in placementObjectPrefabs)
            {
                int placementId = kvp.Key;
                GameObject prefab = kvp.Value;

                var pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );

                placementObjectPools[placementId] = pool;
            }

            Debug.Log($"Initialized {placementObjectPools.Count} object pools");
        }

        /// <summary>
        /// 配置物プレハブの読み込み
        /// </summary>
        private async UniTask LoadPlacementObjectPrefabs()
        {
            // TODO: 実際のAddressables読み込み実装
            // 現在は仮のプレハブを作成

            // CoinItem (ID: 1)
            var coinPrefab = CreateDummyPrefab("CoinItem", Color.yellow);
            placementObjectPrefabs[1] = coinPrefab;

            // BasicObstacle (ID: 2)  
            var obstaclePrefab = CreateDummyPrefab("BasicObstacle", Color.red);
            placementObjectPrefabs[2] = obstaclePrefab;

            // BasicEnemy (ID: 3)
            var enemyPrefab = CreateDummyPrefab("BasicEnemy", Color.magenta);
            placementObjectPrefabs[3] = enemyPrefab;

            await UniTask.Yield(); // 非同期処理のシミュレート
        }

        /// <summary>
        /// ダミープレハブの作成（開発用）
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="color">色</param>
        /// <returns>作成されたプレハブ</returns>
        private GameObject CreateDummyPrefab(string name, Color color)
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = name;
            prefab.GetComponent<Renderer>().material.color = color;
            prefab.SetActive(false);

            // IPlacementObjectコンポーネントの追加
            // TODO: 実際の配置物クラスを追加

            return prefab;
        }

        /// <summary>
        /// ステージブロックの生成
        /// </summary>
        /// <param name="startBlock">開始ブロック番号</param>
        /// <param name="endBlock">終了ブロック番号</param>
        private void GenerateStageBlocks(int startBlock, int endBlock)
        {
            for (int blockIndex = startBlock; blockIndex <= endBlock; blockIndex++)
            {
                GenerateSingleStageBlock(blockIndex);
            }
        }

        /// <summary>
        /// 単一ステージブロックの生成
        /// </summary>
        /// <param name="blockIndex">ブロック番号</param>
        private void GenerateSingleStageBlock(int blockIndex)
        {
            // ランダムにステージデータを選択
            if (loadedStageData.Count == 0) return;

            StageData selectedData = loadedStageData[blockIndex % loadedStageData.Count];
            stageDataMap[blockIndex] = selectedData;

            // ステージブロックの位置計算
            Vector3 blockPosition = new Vector3(0, 0, -blockIndex * 100f);

            // ステージブロックオブジェクトの作成
            GameObject stageBlock = CreateStageBlockObject(blockIndex, blockPosition);
            stageBlockInstances[blockIndex] = stageBlock;
            activeStageBlocks.Enqueue(stageBlock);

            // 配置物の生成
            GeneratePlacementObjects(selectedData, blockIndex, blockPosition);

            Debug.Log($"Generated stage block {blockIndex} at position {blockPosition}");
        }

        /// <summary>
        /// ステージブロックオブジェクトの作成
        /// </summary>
        /// <param name="blockIndex">ブロック番号</param>
        /// <param name="position">位置</param>
        /// <returns>作成されたステージブロック</returns>
        private GameObject CreateStageBlockObject(int blockIndex, Vector3 position)
        {
            var blockObject = new GameObject($"StageBlock_{blockIndex}");
            blockObject.transform.position = position;

            // 親オブジェクトの設定
            var stageParent = GameObject.Find("StageParent");
            if (stageParent != null)
            {
                blockObject.transform.SetParent(stageParent.transform);
            }

            return blockObject;
        }

        /// <summary>
        /// 配置物オブジェクトの生成
        /// BDD仕様: 読み込んだステージブロックデータにあわせて配置物を設定すること
        /// </summary>
        /// <param name="stageData">ステージデータ</param>
        /// <param name="blockIndex">ブロック番号</param>
        /// <param name="blockPosition">ブロック位置</param>
        private void GeneratePlacementObjects(StageData stageData, int blockIndex, Vector3 blockPosition)
        {
            int blockSize = stageData.BlockSize;
            int laneNum = stageData.LaneNum;

            for (int distance = 0; distance < blockSize; distance++)
            {
                for (int lane = 0; lane < laneNum; lane++)
                {
                    int placementId = stageData.GetPlacementId(distance, lane);
                    if (placementId == 0) continue; // 配置物なし

                    // 配置物の位置計算
                    Vector3 placementPosition = blockPosition + new Vector3(
                        (lane - 1) * 2f, // レーン位置
                        0f,
                        -distance // 距離
                    );

                    // オブジェクトプールから取得
                    GameObject placementObject = GetPlacementObjectFromPool(placementId);
                    if (placementObject != null)
                    {
                        placementObject.transform.position = placementPosition;
                    }
                }
            }
        }

        /// <summary>
        /// オブジェクトプールから配置物を取得
        /// </summary>
        /// <param name="placementId">配置物ID</param>
        /// <returns>配置物オブジェクト</returns>
        private GameObject GetPlacementObjectFromPool(int placementId)
        {
            if (placementObjectPools.TryGetValue(placementId, out ObjectPool<GameObject> pool))
            {
                return pool.Get();
            }

            Debug.LogWarning($"Object pool not found for placement ID: {placementId}");
            return null;
        }

        /// <summary>
        /// 古いブロックのクリーンアップ
        /// </summary>
        /// <param name="currentBlock">現在のブロック番号</param>
        private void CleanupOldBlocks(int currentBlock)
        {
            int removeThreshold = currentBlock - 2; // 2ブロック前まで保持

            var blocksToRemove = stageBlockInstances
                .Where(kvp => kvp.Key < removeThreshold)
                .ToList();

            foreach (var kvp in blocksToRemove)
            {
                int blockIndex = kvp.Key;
                GameObject blockObject = kvp.Value;

                // ブロック削除
                if (blockObject != null)
                {
                    Destroy(blockObject);
                }

                stageBlockInstances.Remove(blockIndex);
                stageDataMap.Remove(blockIndex);
            }

            if (blocksToRemove.Count > 0)
            {
                Debug.Log($"Cleaned up {blocksToRemove.Count} old stage blocks");
            }
        }

        /// <summary>
        /// フォールバックステージデータの作成
        /// </summary>
        private void CreateFallbackStageData()
        {
            var fallbackData = ScriptableObject.CreateInstance<StageData>();
            // TODO: デフォルト値の設定
            loadedStageData.Add(fallbackData);

            Debug.Log("Created fallback stage data");
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            if (generateAheadDistance <= 0) generateAheadDistance = 300;
            if (maxActiveBlocks <= 0) maxActiveBlocks = 5;
        }

        private void OnDrawGizmosSelected()
        {
            // 生成範囲の可視化
            Gizmos.color = Color.green;
            Vector3 currentPos = new Vector3(0, 1, -lastPlayerDistance);
            Vector3 generatePos = new Vector3(0, 1, -lastPlayerDistance - generateAheadDistance);

            Gizmos.DrawLine(currentPos, generatePos);
            Gizmos.DrawWireSphere(generatePos, 10f);

            // アクティブブロックの可視化
            Gizmos.color = Color.blue;
            foreach (var kvp in stageBlockInstances)
            {
                int blockIndex = kvp.Key;
                Vector3 blockPos = new Vector3(0, 0.5f, -blockIndex * 100f);
                Gizmos.DrawWireCube(blockPos, new Vector3(6f, 1f, 100f));
            }
        }

        #endregion
    }
}