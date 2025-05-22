using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// コイン（スコアアイテム）の実装
    /// BDD仕様: spec/code/Item/CoinItem.md
    /// </summary>
    public class CoinItem : Item
    {
        [Header("Coin Settings")]
        [SerializeField] private CoinType scoreType = CoinType.Normal;
        
        // コインタイプ別のスコア値
        private static readonly int[] CoinScoreValues = { 10, 50, 100, 500 };

        #region Enums

        /// <summary>
        /// コインの種類
        /// BDD仕様: scoreType: コインの種類を示す
        /// </summary>
        public enum CoinType
        {
            Normal = 0,     // 通常コイン: 10点
            Silver = 1,     // シルバーコイン: 50点
            Gold = 2,       // ゴールドコイン: 100点
            Diamond = 3     // ダイヤモンドコイン: 500点
        }

        #endregion

        #region Properties

        /// <summary>
        /// このコインのスコア値
        /// </summary>
        public int ScoreValue => GetScoreValue(scoreType);

        /// <summary>
        /// コインタイプ
        /// </summary>
        public CoinType CurrentType => scoreType;

        #endregion

        #region Override Methods

        protected override void OnItemCollected()
        {
            // BDD仕様: アイテムのタイプに応じてスコアが加算される
            int scoreToAdd = GetScoreValue(scoreType);
            
            var gameManager = GetGameManager();
            if (gameManager != null)
            {
                gameManager.AddScore(scoreToAdd);
                Debug.Log($"Coin collected! Score +{scoreToAdd} (Type: {scoreType})");
            }
            else
            {
                Debug.LogWarning("GameManager not found! Score not added.");
            }
        }

        protected override void PlayCollectionEffect()
        {
            base.PlayCollectionEffect();
            
            // コインタイプ別のエフェクト
            PlayCoinSpecificEffect();
        }

        protected override void UpdateItemBehavior()
        {
            // コイン特有のアニメーション
            // 浮遊効果 + 回転
            float floatHeight = Mathf.Sin(Time.time * 2f) * 0.1f;
            Vector3 basePosition = transform.position;
            basePosition.y += floatHeight * Time.deltaTime;
            transform.position = basePosition;
            
            // 回転アニメーション
            transform.Rotate(0, 180f * Time.deltaTime, 0);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// コインタイプの設定
        /// </summary>
        /// <param name="newType">新しいコインタイプ</param>
        public void SetCoinType(CoinType newType)
        {
            scoreType = newType;
            UpdateVisualAppearance();
        }

        /// <summary>
        /// 指定タイプのスコア値を取得
        /// BDD仕様: scoreTypeに応じたスコアが加算される
        /// </summary>
        /// <param name="type">コインタイプ</param>
        /// <returns>スコア値</returns>
        public int GetScoreValue(CoinType type)
        {
            int index = (int)type;
            if (index >= 0 && index < CoinScoreValues.Length)
            {
                return CoinScoreValues[index];
            }
            return CoinScoreValues[0]; // デフォルト値
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コインタイプ別のエフェクト再生
        /// </summary>
        private void PlayCoinSpecificEffect()
        {
            switch (scoreType)
            {
                case CoinType.Normal:
                    // 通常のコインエフェクト
                    Debug.Log("Normal coin effect");
                    break;
                    
                case CoinType.Silver:
                    // シルバーコインエフェクト
                    Debug.Log("Silver coin effect");
                    break;
                    
                case CoinType.Gold:
                    // ゴールドコインエフェクト
                    Debug.Log("Gold coin effect");
                    break;
                    
                case CoinType.Diamond:
                    // ダイヤモンドコインエフェクト
                    Debug.Log("Diamond coin effect");
                    break;
            }
            
            // TODO: 実際のパーティクルエフェクト、音響効果の実装
        }

        /// <summary>
        /// 見た目の更新
        /// </summary>
        private void UpdateVisualAppearance()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                // コインタイプ別の色設定
                Color coinColor = GetCoinColor(scoreType);
                renderer.material.color = coinColor;
            }
        }

        /// <summary>
        /// コインタイプ別の色を取得
        /// </summary>
        /// <param name="type">コインタイプ</param>
        /// <returns>色</returns>
        private Color GetCoinColor(CoinType type)
        {
            switch (type)
            {
                case CoinType.Normal:
                    return Color.yellow;
                case CoinType.Silver:
                    return new Color(0.8f, 0.8f, 0.9f); // シルバー
                case CoinType.Gold:
                    return new Color(1f, 0.8f, 0f); // ゴールド
                case CoinType.Diamond:
                    return new Color(0.7f, 0.9f, 1f); // ダイヤモンド
                default:
                    return Color.yellow;
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            UpdateVisualAppearance();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateVisualAppearance();
        }

        #endregion

        #region Editor Support

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // エディタでタイプが変更された時の見た目更新
            if (Application.isPlaying)
            {
                UpdateVisualAppearance();
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // コインタイプ別のギズモ色
            Gizmos.color = GetCoinColor(scoreType);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        [ContextMenu("Set Random Coin Type")]
        private void SetRandomCoinType()
        {
            var values = System.Enum.GetValues(typeof(CoinType));
            CoinType randomType = (CoinType)values.GetValue(Random.Range(0, values.Length));
            SetCoinType(randomType);
        }

        #endregion
    }
}