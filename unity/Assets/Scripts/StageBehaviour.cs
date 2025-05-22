using UnityEngine;
using R3;

namespace RunGame
{
    /// <summary>
    /// ステージを動かすクラス
    /// BDD仕様: spec/code/StageBehaviour.md
    /// </summary>
    public class StageBehaviour : MonoBehaviour
    {
        [Header("Scroll Settings")]
        [SerializeField] private float scrollSpeedMin = 1.0f;
        [SerializeField] private float scrollSpeedMax = 10.0f;
        [SerializeField] private int maxGear = 5;

        [Header("Stage Management")]
        [SerializeField] private Transform stageParent;
        [SerializeField] private Transform backgroundParent;

        // 内部変数
        private int gear = 0;
        private float totalDistance = 0f;
        private float currentScrollSpeed;
        private StageCreator stageCreator;

        // イベント通知
        private readonly Subject<float> onDistanceChanged = new Subject<float>();
        private readonly Subject<int> onGearChanged = new Subject<int>();

        #region Properties

        /// <summary>
        /// 現在のギア
        /// </summary>
        public int CurrentGear => gear;

        /// <summary>
        /// 現在のスクロール速度
        /// </summary>
        public float CurrentScrollSpeed => currentScrollSpeed;

        /// <summary>
        /// 総移動距離
        /// </summary>
        public float TotalDistance => totalDistance;

        /// <summary>
        /// 距離変更の通知
        /// </summary>
        public Observable<float> OnDistanceChanged => onDistanceChanged.AsObservable();

        /// <summary>
        /// ギア変更の通知
        /// </summary>
        public Observable<int> OnGearChanged => onGearChanged.AsObservable();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            // BDD仕様: Updateでスクロール処理を実行する
            ExecuteScrolling();
            
            // BDD仕様: 一定距離を走破したか判断し、ギアを上げる
            CheckGearUpCondition();
            
            // BDD仕様: 一定距離を走破したか判断し、StageCreatorに次のマップを用意させる
            CheckStageCreationCondition();
        }

        private void OnDestroy()
        {
            onDistanceChanged?.Dispose();
            onGearChanged?.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定位置の配置物情報を取得
        /// </summary>
        /// <param name="distance">距離</param>
        /// <param name="lane">レーン番号</param>
        /// <returns>配置物ID</returns>
        public int GetPlacementAt(int distance, int lane)
        {
            if (stageCreator != null)
            {
                return stageCreator.GetPlacementAt(distance, lane);
            }
            return 0;
        }

        /// <summary>
        /// 現在のプレイヤー位置での配置物情報を取得
        /// </summary>
        /// <param name="player">プレイヤー参照</param>
        /// <returns>各レーンの配置物ID配列</returns>
        public int[] GetCurrentPlacementInfo(Player player)
        {
            int playerDistance = Mathf.FloorToInt(totalDistance);
            int[] placements = new int[3]; // 3レーン分
            
            for (int lane = 0; lane < 3; lane++)
            {
                placements[lane] = GetPlacementAt(playerDistance, lane);
            }
            
            return placements;
        }

        /// <summary>
        /// 強制的にギアを変更
        /// </summary>
        /// <param name="newGear">新しいギア値</param>
        public void SetGear(int newGear)
        {
            int clampedGear = Mathf.Clamp(newGear, 0, maxGear);
            if (gear != clampedGear)
            {
                gear = clampedGear;
                UpdateScrollSpeed();
                onGearChanged.OnNext(gear);
                Debug.Log($"Gear changed to: {gear}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            stageCreator = FindObjectOfType<StageCreator>();
            
            if (stageParent == null)
            {
                GameObject stageObj = GameObject.Find("StageParent");
                if (stageObj == null)
                {
                    stageObj = new GameObject("StageParent");
                }
                stageParent = stageObj.transform;
            }
            
            if (backgroundParent == null)
            {
                GameObject bgObj = GameObject.Find("BackgroundParent");
                if (bgObj == null)
                {
                    bgObj = new GameObject("BackgroundParent");
                }
                backgroundParent = bgObj.transform;
            }
            
            // 初期スクロール速度の設定
            UpdateScrollSpeed();
            
            Debug.Log("StageBehaviour initialized");
        }

        /// <summary>
        /// スクロール処理の実行
        /// BDD仕様: Updateでスクロール処理を実行する
        /// </summary>
        private void ExecuteScrolling()
        {
            float deltaDistance = currentScrollSpeed * Time.deltaTime;
            totalDistance += deltaDistance;
            
            // ステージブロックの移動
            if (stageParent != null)
            {
                stageParent.position += Vector3.back * deltaDistance;
            }
            
            // 背景の移動（異なる速度でパララックス効果）
            if (backgroundParent != null)
            {
                float backgroundSpeed = currentScrollSpeed * 0.5f; // 半分の速度
                backgroundParent.position += Vector3.back * backgroundSpeed * Time.deltaTime;
            }
            
            // 距離変更の通知
            onDistanceChanged.OnNext(totalDistance);
        }

        /// <summary>
        /// ギアアップ条件のチェック
        /// BDD仕様: 1000mを超えると内部変数の「ギア」を一段階上げる
        /// </summary>
        private void CheckGearUpCondition()
        {
            int targetGear = Mathf.FloorToInt(totalDistance / 1000f);
            int newGear = Mathf.Min(targetGear, maxGear);
            
            if (newGear > gear)
            {
                SetGear(newGear);
            }
        }

        /// <summary>
        /// ステージ生成条件のチェック
        /// BDD仕様: StageCreatorに次のマップを用意させる
        /// </summary>
        private void CheckStageCreationCondition()
        {
            if (stageCreator != null)
            {
                // StageCreatorに現在の距離を通知
                // StageCreator側で必要に応じて新しいブロックを生成
                stageCreator.UpdatePlayerDistance(totalDistance);
            }
        }

        /// <summary>
        /// スクロール速度の更新
        /// BDD仕様: スクロール速度は徐々に上昇する
        /// </summary>
        private void UpdateScrollSpeed()
        {
            if (maxGear <= 0)
            {
                currentScrollSpeed = scrollSpeedMin;
                return;
            }
            
            float gearRatio = (float)gear / maxGear;
            currentScrollSpeed = Mathf.Lerp(scrollSpeedMin, scrollSpeedMax, gearRatio);
            
            Debug.Log($"Scroll speed updated: {currentScrollSpeed:F2} (Gear: {gear})");
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // パラメータの検証
            if (scrollSpeedMin <= 0f) scrollSpeedMin = 1f;
            if (scrollSpeedMax <= scrollSpeedMin) scrollSpeedMax = scrollSpeedMin + 1f;
            if (maxGear <= 0) maxGear = 1;
            
            // ギアの制限
            gear = Mathf.Clamp(gear, 0, maxGear);
        }

        private void OnDrawGizmosSelected()
        {
            // 現在の進行状況を可視化
            Gizmos.color = Color.green;
            Vector3 distancePos = new Vector3(0, 2, -totalDistance);
            Gizmos.DrawWireSphere(distancePos, 1f);
            
            // ギア表示
            for (int i = 0; i <= maxGear; i++)
            {
                Vector3 gearPos = new Vector3(i * 2 - maxGear, 3, 0);
                Gizmos.color = (i == gear) ? Color.red : Color.gray;
                Gizmos.DrawWireCube(gearPos, Vector3.one * 0.5f);
            }
        }

        #endregion
    }
}