using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// ステージを動的に生成するクラス
/// </summary>
public class StageCreator : MonoBehaviour
{
    #region Inspector Variables
    [Header("Stage Generation Settings")]
    [Tooltip("プレイヤーからの距離がこの値より大きくなると新しいステージブロックを生成します")]
    [SerializeField] private float generationDistance = 150f;
    
    [Tooltip("ステージブロックの長さ（メートル）")]
    [SerializeField] private float stageBlockLength = 100f;
    
    [Tooltip("生成済みステージブロックの最大数")]
    [SerializeField] private int maxStageBlocks = 5;
    
    [Tooltip("ステージブロックのアドレッサブルキー")]
    [SerializeField] private List<string> stageBlockKeys = new List<string>();
    
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
    
    // 最後に生成したステージブロックの終点位置
    private Vector3 lastBlockEndPosition = Vector3.zero;
    
    // プレイヤーの参照
    private Transform playerTransform;
    
    // ロード済みかどうか
    private bool isLoaded = false;
    #endregion

    #region Unity Methods
    /// <summary>
    /// 開始時の処理
    /// </summary>
    private void Start()
    {
        // プレイヤーの参照を取得
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (playerTransform == null)
        {
            Debug.LogError("プレイヤーが見つかりません。Playerタグが正しく設定されているか確認してください。");
            return;
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
        if (!isLoaded || playerTransform == null) return;
        
        // プレイヤーと最後のステージブロックの終点の距離を計算
        float distanceToLastBlock = Vector3.Distance(
            new Vector3(playerTransform.position.x, 0, playerTransform.position.z),
            new Vector3(lastBlockEndPosition.x, 0, lastBlockEndPosition.z)
        );
        
        // 一定距離以下になったら新しいステージブロックを生成
        if (distanceToLastBlock < generationDistance)
        {
            GenerateNextStageBlock();
            
            // 最大数を超えたら古いステージブロックを削除
            if (stageBlocks.Count > maxStageBlocks)
            {
                RemoveOldestStageBlock();
            }
        }
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
        
        // プレハブからステージブロックをインスタンス化
        GameObject stageBlock = Instantiate(prefab, lastBlockEndPosition, Quaternion.identity);
        stageBlocks.Add(stageBlock);
        
        // ステージブロックのデータを取得
        StageBlockData blockData = stageBlock.GetComponent<StageBlockData>();
        if (blockData == null)
        {
            Debug.LogWarning($"ステージブロック '{randomKey}' にStageBlockDataコンポーネントがありません。");
            
            // デフォルトの終点位置を計算
            lastBlockEndPosition += Vector3.forward * stageBlockLength;
        }
        else
        {
            // ステージブロックに配置物を設定
            SetupStageBlockObjects(blockData);
            
            // 次の終点位置を計算
            lastBlockEndPosition = blockData.GetEndPosition();
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
}
