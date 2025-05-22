using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// アイテムの制御をするベースクラス
    /// BDD仕様: spec/code/Item.md
    /// </summary>
    public abstract class Item : MonoBehaviour, IPlacementObject
    {
        [Header("Item Settings")]
        [SerializeField] protected bool isCollected = false;

        #region IPlacementObject Implementation

        public virtual PlacementObjectType ObjType => PlacementObjectType.SafeObject;

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public virtual void Action()
        {
            if (isCollected) return;

            // アイテム取得処理
            OnItemCollected();
            isCollected = true;

            // 視覚効果
            PlayCollectionEffect();

            // オブジェクトの非活性化（プールに戻す）
            gameObject.SetActive(false);
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// アイテム取得時の処理（派生クラスで実装）
        /// BDD仕様: 派生先に倣う
        /// </summary>
        protected abstract void OnItemCollected();

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 取得エフェクトの再生
        /// </summary>
        protected virtual void PlayCollectionEffect()
        {
            // パーティクルエフェクトや音響効果を再生
            Debug.Log($"{gameObject.name} collected!");

            // TODO: エフェクトシステムとの連携
            // - パーティクルエフェクト
            // - サウンドエフェクト
            // - UI通知
        }

        /// <summary>
        /// アイテムのリセット（プールから再取得時）
        /// </summary>
        protected virtual void ResetItem()
        {
            isCollected = false;
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // 基本初期化
            ResetItem();
        }

        protected virtual void OnEnable()
        {
            // プールから取得された際の初期化
            ResetItem();
        }

        protected virtual void Update()
        {
            // アイテム固有の更新処理
            // 例: 回転アニメーション、浮遊効果など
            UpdateItemBehavior();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// アイテムの挙動更新（派生クラスでオーバーライド可能）
        /// </summary>
        protected virtual void UpdateItemBehavior()
        {
            // デフォルトの回転アニメーション
            transform.Rotate(0, 90f * Time.deltaTime, 0);
        }

        /// <summary>
        /// GameManagerの取得
        /// </summary>
        protected GameManager GetGameManager()
        {
            return FindObjectOfType<GameManager>();
        }

        #endregion

        #region Editor Support

        protected virtual void OnValidate()
        {
            // エディタでの検証処理
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // アイテムの取得範囲を可視化
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        #endregion
    }
}