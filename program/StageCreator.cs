using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// ステージを動的に生成するクラス
/// 仕様に基づき、プレイヤーは原点に固定され、マップ側を動かす設計
/// </summary>
public class StageCreator : MonoBehaviour
{
    #region Inspector Variables
    [Header("Stage Generation Settings")]
    [Tooltip("プレイヤーからの距離がこの値より小さくなると新しいステージブロックを生成します")]
    [SerializeField] private float generationThreshold = 150f;
    
    [Tooltip("ステージブロックの長さ（メートル）")]
    [SerializeField] private float stageBlockLength = 100f;
    
    [Tooltip("生成済みステージブロックの最大数")]
    [SerializeField] private int maxStageBlocks = 5;
    
    [Tooltip("ステージブロックのアドレッサブルキー")]
    [SerializeField] private List<string> stageBlockKeys = new List<string>();

    [Header("Background Settings")]
    [Tooltip("遠景オブジェクト")]
    [SerializeField] private GameObject backgroundObject;
    
    [Tooltip("遠景の移動速度比率（プレイヤー速度に対する割合）")]
    [SerializeField] private float backgroundMoveRatio = 0.5f;
    
    [Header("Object Pooling Settings")]
    [Tooltip("アイテムのプール設定")]
    [SerializeField] private List<PoolableObjectConfig> itemPoolConfigs = new List<PoolableObjectConfig>();
    
    [Tooltip("障害物のプール設定")]
    [SerializeField] private List<PoolableObjectConfig> obstaclePoolConfigs = new List<PoolableObjectConfig>();
    
    [Tooltip("敵のプール設定")]
    [SerializeField] private List<PoolableObjectConfig> enemyPoolConfigs = new List<PoolableObjectConfig>();
    #endregion

    #region Private Variables
    // 生成したステージブロックの管理リスト
    private List<GameObject> stageBlocks = new List<GameObject>();
    
    // ステージブロックのプレハブ
    private Dictionary<string, GameObject> stageBlockPrefabs = new Dictionary<string, GameObject>();
    
    // オブジェクトプール
    private Dictionary<string, ObjectPool> objectPools = new Dictionary<string, ObjectPool>();
    
    // 現在のステージブロックの開始位置（z座標）
    private float currentStagePosition = 0f;
    
    // 遠景の参照
    private Transform backgroundTransform;
    
    // プレイヤーの参照
    private Player playerReference;
    
    // ゲームマネージャーの参照
    private GameManager gameManager;
    
    // 前回のプレイヤー速度
    private float lastPlayerSpeed = 0f;
    
    // ロード済みかどうか
    private bool isLoaded = false;
    
    // ステージ移動用の一時変数
    private Vector3 stageMovement = Vector3.zero;
    private Vector3 backgroundMovement = Vector3.zero;
    #endregion

    #region Unity Methods
    /// <summary>
    /// 開始時の処理
    /// </summary>
    private void Start()
    {
        // プレイヤーの参照を取得
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerReference = playerObject.GetComponent<Player>();
        }
        
        if (playerReference == null)
        {
            Debug.LogError("プレイヤーが見つかりません。Playerタグが正しく設定されているか確認してください。");
            return;
        }
        
        // ゲームマネージャーの参照を取得
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("GameManagerが見つかりません。");
            return;
        }
        
        // 遠景の設定
        if (backgroundObject != null)
        {
            backgroundTransform = backgroundObject.transform;
        }
        
        // ステージブロックのPrefabデータを読み込む
        StartCoroutine(LoadStageBlockPrefabs());
        
        // オブジェクトプールを初期化
        InitializeObjectPools();
    }

    /// <summary>
    /// 毎フレームの処理
    /// </summary>
    private void Update()
    {
        if (!isLoaded || playerReference == null || gameManager == null) return;
        
        // プレイヤーの現在速度を取得
        float playerSpeed = gameManager.GameSpeed;
        
        // ステージの移動処理
        MoveStage(playerSpeed);
        
        // 遠景の移動処理
        MoveBackground(playerSpeed);
        
        // 新しいステージブロックの生成判定
        CheckForNewStageBlock();
        
        // 前回の速度を保存
        lastPlayerSpeed = playerSpeed;
    }

    /// <summary>
    /// アプリケーション終了時の処理
    /// </summary>
    private void OnDestroy()
    {
        // Addressablesのリソースを解放
        foreach (var prefab in stageBlockPrefabs.Values)
        {
            Addressables.Release(prefab);
        }
    }
    #endregion

    #region Stage Movement Methods
    /// <summary>
    /// ステージを移動する
    /// </summary>
    private void MoveStage(float playerSpeed)
    {
        // プレイヤーの速度に応じてステージを後ろに移動
        float moveDistance = playerSpeed * Time.deltaTime;
        
        // 各ステージブロックを移動
        foreach (GameObject block in stageBlocks)
        {
            stageMovement.z = -moveDistance;
            block.transform.position += stageMovement;
        }
        
        // 現在位置を更新
        currentStagePosition += moveDistance;
    }

    /// <summary>
    /// 遠景を移動する
    /// </summary>
    private void MoveBackground(float playerSpeed)
    {
        if (backgroundTransform == null) return;
        
        // 遠景は比率に応じてゆっくり移動
        float moveDistance = playerSpeed * backgroundMoveRatio * Time.deltaTime;
        
        backgroundMovement.z = -moveDistance;
        backgroundTransform.position += backgroundMovement;
        
        // 遠景が一定距離離れたらループ
        if (Mathf.Abs(backgroundTransform.position.z) > 1000f)
        {
            Vector3 resetPos = backgroundTransform.position;
            resetPos.z = 0f;
            backgroundTransform.position = resetPos;
        }
    }

    /// <summary>
    /// 新しいステージブロックが必要か確認する
    /// </summary>
    private void CheckForNewStageBlock()
    {
        // 現在のステージの最後尾のz座標を計算
        float lastBlockEndZ = 0f;
        if (stageBlocks.Count > 0)
        {
            GameObject lastBlock = stageBlocks[stageBlocks.Count - 1];
            StageBlockData blockData = lastBlock.GetComponent<StageBlockData>();
            
            if (blockData != null)
            {
                lastBlockEndZ = blockData.GetEndPosition().z;
            }
            else
            {
                lastBlockEndZ = lastBlock.transform.position.z - stageBlockLength;
            }
        }
        
        // プレイヤーから見て前方にあるステージの長さが閾値以下になったら新しいブロックを生成
        if (lastBlockEndZ > -generationThreshold)
        {
            GenerateNextStageBlock();
            
            // 最大数を超えたら古いステージブロックを削除
            if (stageBlocks.Count > maxStageBlocks)
            {
                RemoveOldestStageBlock();
            }
        }
    }
    #endregion

    #region Stage Generation Methods
    /// <summary>
    /// ステージブロックのPrefabをロードする
    /// </summary>
    private IEnumerator LoadStageBlockPrefabs()
    {
        if (stageBlockKeys.Count == 0)
        {
            Debug.LogError("ステージブロックのアドレッサブルキーが設定されていません。");
            yield break;
        }
        
        foreach (string key in stageBlockKeys)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
            yield return handle;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                stageBlockPrefabs[key] = handle.Result;
                Debug.Log($"ステージブロック '{key}' をロードしました。");
            }
            else
            {
                Debug.LogError($"ステージブロック '{key}' のロードに失敗しました。");
            }
        }
        
        isLoaded = true;
        
        // 初期ステージブロックを生成
        for (int i = 0; i < 3; i++)
        {
            GenerateNextStageBlock();
        }
    }

    /// <summary>
    /// 次のステージブロックを生成する
    /// </summary>
    private void GenerateNextStageBlock()
    {
        if (stageBlockPrefabs.Count == 0) return;
        
        // ランダムにステージブロックを選択
        string randomKey = stageBlockKeys[Random.Range(0, stageBlockKeys.Count)];
        GameObject prefab = stageBlockPrefabs[randomKey];
        
        // 次のブロックの開始位置を計算
        Vector3 newBlockPosition = Vector3.zero;
        
        if (stageBlocks.Count > 0)
        {
            GameObject lastBlock = stageBlocks[stageBlocks.Count - 1];
            StageBlockData lastBlockData = lastBlock.GetComponent<StageBlockData>();
            
            if (lastBlockData != null)
            {
                // 前のブロックの終点に合わせて配置
                Vector3 endPos = lastBlockData.GetEndPosition();
                newBlockPosition = new Vector3(0, 0, endPos.z);
            }
            else
            {
                // 前のブロックから一定距離後ろに配置
                newBlockPosition = new Vector3(0, 0, lastBlock.transform.position.z - stageBlockLength);
            }
        }
        else
        {
            // 最初のブロックはプレイヤーの位置から開始
            newBlockPosition = new Vector3(0, 0, -stageBlockLength);
        }
        
        // プレハブからステージブロックをインスタンス化
        GameObject stageBlock = Instantiate(prefab, newBlockPosition, Quaternion.identity, transform);
        stageBlocks.Add(stageBlock);
        
        // ステージブロックのデータを取得
        StageBlockData blockData = stageBlock.GetComponent<StageBlockData>();
        if (blockData == null)
        {
            Debug.LogWarning($"ステージブロック '{randomKey}' にStageBlockDataコンポーネントがありません。");
        }
        else
        {
            // ステージブロックに配置物を設定
            SetupStageBlockObjects(blockData);
        }
    }

    /// <summary>
    /// 最も古いステージブロックを削除する
    /// </summary>
    private void RemoveOldestStageBlock()
    {
        if (stageBlocks.Count == 0) return;
        
        GameObject oldestBlock = stageBlocks[0];
        stageBlocks.RemoveAt(0);
        
        // ステージブロック内の配置物をプールに戻す
        StageBlockData blockData = oldestBlock.GetComponent<StageBlockData>();
        if (blockData != null)
        {
            blockData.ReturnObjectsToPool();
        }
        
        Destroy(oldestBlock);
    }

    /// <summary>
    /// ステージブロックに配置物を設定する
    /// </summary>
    private void SetupStageBlockObjects(StageBlockData blockData)
    {
        // ステージブロックの配置データを取得
        PlacementData[] placementData = blockData.GetPlacementData();
        
        if (placementData == null || placementData.Length == 0)
        {
            Debug.LogWarning("配置データがありません。");
            return;
        }
        
        // 各配置データに基づいてオブジェクトを配置
        foreach (PlacementData data in placementData)
        {
            // オブジェクトプールからオブジェクトを取得
            if (objectPools.TryGetValue(data.prefabKey, out ObjectPool pool))
            {
                // 世界座標に変換
                Vector3 worldPosition = blockData.transform.TransformPoint(data.localPosition);
                Quaternion worldRotation = blockData.transform.rotation * data.localRotation;
                
                // プールからオブジェクトを取得して配置
                GameObject obj = pool.GetObject();
                if (obj != null)
                {
                    obj.transform.position = worldPosition;
                    obj.transform.rotation = worldRotation;
                    obj.transform.localScale = data.scale;
                    obj.SetActive(true);
                    
                    // グリッド位置情報を設定（PlayerがCollisionではなくGridPositionで判定するため）
                    GridPositionData gridPosData = obj.GetComponent<GridPositionData>();
                    if (gridPosData == null)
                    {
                        gridPosData = obj.AddComponent<GridPositionData>();
                    }
                    
                    // グリッド位置を設定（1mグリッド）
                    gridPosData.SetGridPosition(
                        Mathf.FloorToInt(worldPosition.x),
                        Mathf.FloorToInt(worldPosition.z)
                    );
                    
                    // サイズが1x1より大きい場合は複数グリッドにまたがる
                    if (data.gridWidth > 1 || data.gridLength > 1)
                    {
                        gridPosData.SetGridSize(data.gridWidth, data.gridLength);
                    }
                    
                    // ステージブロックに配置物を登録
                    blockData.RegisterPlacedObject(obj);
                }
            }
            else
            {
                Debug.LogWarning($"プールが見つかりません: {data.prefabKey}");
            }
        }
    }
    
    /// <summary>
    /// プレイヤーの現在位置からグリッド座標を取得
    /// </summary>
    public Vector2Int GetPlayerGridPosition()
    {
        if (playerReference == null) return Vector2Int.zero;
        
        // プレイヤーは原点に固定されているため、
        // 実際のグリッド位置はステージの移動距離から計算
        int gridX = Mathf.FloorToInt(playerReference.transform.position.x);
        int gridZ = Mathf.FloorToInt(currentStagePosition);
        
        return new Vector2Int(gridX, gridZ);
    }
    
    /// <summary>
    /// 指定グリッドにあるオブジェクトを取得
    /// </summary>
    public List<GameObject> GetObjectsAtGrid(Vector2Int gridPosition)
    {
        List<GameObject> result = new List<GameObject>();
        
        // すべてのアクティブなステージブロックから検索
        foreach (GameObject block in stageBlocks)
        {
            StageBlockData blockData = block.GetComponent<StageBlockData>();
            if (blockData != null)
            {
                // ブロック内の配置オブジェクトを取得
                List<GameObject> placedObjects = blockData.GetPlacedObjects();
                
                foreach (GameObject obj in placedObjects)
                {
                    if (!obj.activeInHierarchy) continue;
                    
                    GridPositionData gridData = obj.GetComponent<GridPositionData>();
                    if (gridData != null && gridData.IsInGrid(gridPosition))
                    {
                        result.Add(obj);
                    }
                }
            }
        }
        
        return result;
    }
    #endregion

    #region Object Pool Methods
    /// <summary>
    /// オブジェクトプールを初期化する
    /// </summary>
    private void InitializeObjectPools()
    {
        // アイテムのプールを初期化
        foreach (var config in itemPoolConfigs)
        {
            CreateObjectPool(config);
        }
        
        // 障害物のプールを初期化
        foreach (var config in obstaclePoolConfigs)
        {
            CreateObjectPool(config);
        }
        
        // 敵のプールを初期化
        foreach (var config in enemyPoolConfigs)
        {
            CreateObjectPool(config);
        }
    }

    /// <summary>
    /// オブジェクトプールを作成する
    /// </summary>
    private void CreateObjectPool(PoolableObjectConfig config)
    {
        if (string.IsNullOrEmpty(config.addressableKey))
        {
            Debug.LogError("アドレッサブルキーが空です。");
            return;
        }
        
        StartCoroutine(LoadAndCreatePool(config));
    }

    /// <summary>
    /// オブジェクトをロードしてプールを作成する
    /// </summary>
    private IEnumerator LoadAndCreatePool(PoolableObjectConfig config)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(config.addressableKey);
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;
            ObjectPool pool = new ObjectPool(prefab, config.initialPoolSize, config.maxPoolSize, transform);
            objectPools[config.addressableKey] = pool;
            Debug.Log($"プールを作成しました: {config.addressableKey}, サイズ: {config.initialPoolSize}");
        }
        else
        {
            Debug.LogError($"プレハブのロードに失敗しました: {config.addressableKey}");
        }
    }
    #endregion
}

/// <summary>
/// プール可能なオブジェクトの設定
/// </summary>
[System.Serializable]
public class PoolableObjectConfig
{
    public string addressableKey;
    public int initialPoolSize = 10;
    public int maxPoolSize = 30;
}

/// <summary>
/// オブジェクトプールクラス
/// </summary>
public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> inactiveObjects;
    private List<GameObject> activeObjects;
    private int maxSize;
    private Transform parent;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ObjectPool(GameObject prefab, int initialSize, int maxSize, Transform parent)
    {
        this.prefab = prefab;
        this.maxSize = maxSize;
        this.parent = parent;
        
        inactiveObjects = new Queue<GameObject>();
        activeObjects = new List<GameObject>();
        
        // 初期オブジェクトを生成
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject();
            obj.SetActive(false);
            inactiveObjects.Enqueue(obj);
        }
    }

    /// <summary>
    /// プールからオブジェクトを取得する
    /// </summary>
    public GameObject GetObject()
    {
        GameObject obj;
        
        if (inactiveObjects.Count > 0)
        {
            // 非アクティブなオブジェクトを再利用
            obj = inactiveObjects.Dequeue();
        }
        else if (activeObjects.Count < maxSize)
        {
            // 新しいオブジェクトを生成
            obj = CreateNewObject();
        }
        else
        {
            // 最大数に達した場合はnullを返す
            Debug.LogWarning($"オブジェクトプールが最大数に達しました: {prefab.name}");
            return null;
        }
        
        activeObjects.Add(obj);
        return obj;
    }

    /// <summary>
    /// オブジェクトをプールに返却する
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (activeObjects.Contains(obj))
        {
            activeObjects.Remove(obj);
            obj.SetActive(false);
            inactiveObjects.Enqueue(obj);
        }
    }

    /// <summary>
    /// 新しいオブジェクトを生成する
    /// </summary>
    private GameObject CreateNewObject()
    {
        GameObject obj = GameObject.Instantiate(prefab, parent);
        obj.name = $"{prefab.name}_pooled";
        
        // IPoolableインターフェースがあれば初期化
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.Initialize(this);
        }
        
        return obj;
    }
}

/// <summary>
/// プール可能なオブジェクトのインターフェース
/// </summary>
public interface IPoolable
{
    void Initialize(ObjectPool pool);
    void ReturnToPool();
}

/// <summary>
/// ステージブロックのデータクラス
/// </summary>
public class StageBlockData : MonoBehaviour
{
    [Tooltip("ステージブロックの終点位置")]
    [SerializeField] private Transform endPoint;
    
    [Tooltip("配置データ")]
    [SerializeField] private PlacementData[] placementData;
    
    // このブロック上に配置されたオブジェクト
    private List<GameObject> placedObjects = new List<GameObject>();

    /// <summary>
    /// 終点位置を取得する
    /// </summary>
    public Vector3 GetEndPosition()
    {
        if (endPoint != null)
        {
            return endPoint.position;
        }
        
        // エンドポイントが設定されていない場合は、位置＋前方向×100mとする
        return transform.position + transform.forward * 100f;
    }

    /// <summary>
    /// 配置データを取得する
    /// </summary>
    public PlacementData[] GetPlacementData()
    {
        return placementData;
    }

    /// <summary>
    /// 配置したオブジェクトを登録する
    /// </summary>
    public void RegisterPlacedObject(GameObject obj)
    {
        if (!placedObjects.Contains(obj))
        {
            placedObjects.Add(obj);
        }
    }

    /// <summary>
    /// 配置したオブジェクトのリストを取得する
    /// </summary>
    public List<GameObject> GetPlacedObjects()
    {
        return placedObjects;
    }

    /// <summary>
    /// 配置したオブジェクトをプールに戻す
    /// </summary>
    public void ReturnObjectsToPool()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                IPoolable poolable = obj.GetComponent<IPoolable>();
                if (poolable != null)
                {
                    poolable.ReturnToPool();
                }
                else
                {
                    obj.SetActive(false);
                }
            }
        }
        
        placedObjects.Clear();
    }
}

/// <summary>
/// オブジェクトの配置データ
/// </summary>
[System.Serializable]
public class PlacementData
{
    public string prefabKey;        // Addressableのキー
    public Vector3 localPosition;   // ローカル座標
    public Quaternion localRotation; // ローカル回転
    public Vector3 scale = Vector3.one; // スケール
    public string objectType;       // オブジェクトタイプ（アイテム、障害物、敵など）
    public int gridWidth = 1;       // グリッド上の幅
    public int gridLength = 1;      // グリッド上の長さ
}

/// <summary>
/// グリッド位置情報を保持するコンポーネント
/// </summary>
public class GridPositionData : MonoBehaviour
{
    // グリッド上の位置（X軸：レーン、Z軸：前後）
    private Vector2Int gridPosition;
    
    // オブジェクトのグリッドサイズ
    private Vector2Int gridSize = new Vector2Int(1, 1);
    
    /// <summary>
    /// グリッド位置を設定
    /// </summary>
    public void SetGridPosition(int x, int z)
    {
        gridPosition = new Vector2Int(x, z);
    }
    
    /// <summary>
    /// グリッドサイズを設定
    /// </summary>
    public void SetGridSize(int width, int length)
    {
        gridSize = new Vector2Int(width, length);
    }
    
    /// <summary>
    /// 指定位置がこのオブジェクトのグリッド内かチェック
    /// </summary>
    public bool IsInGrid(Vector2Int checkPosition)
    {
        // グリッド範囲内をチェック
        return checkPosition.x >= gridPosition.x && 
               checkPosition.x < gridPosition.x + gridSize.x && 
               checkPosition.z >= gridPosition.z && 
               checkPosition.z < gridPosition.z + gridSize.z;
    }
    
    /// <summary>
    /// グリッド位置を取得
    /// </summary>
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
    
    /// <summary>
    /// グリッドサイズを取得
    /// </summary>
    public Vector2Int GetGridSize()
    {
        return gridSize;
    }
}
