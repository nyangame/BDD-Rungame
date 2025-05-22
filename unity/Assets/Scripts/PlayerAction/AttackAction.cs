using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// 攻撃状態クラス
    /// BDD仕様: spec/code/PlayerAction/AttackAction.md
    /// </summary>
    public class AttackAction : MonoBehaviour, IPlayerAction
    {
        [Header("Attack Parameters")]
        [SerializeField] private float attackTime = 0.5f;
        [SerializeField] private float attackInterval = 1.0f;

        private float currentAttackTime = 0f;
        private float attackCoolTime = 0f;
        private bool isAttacking = false;

        #region IPlayerAction Implementation

        public ActionTagType ActionTag => ActionTagType.BlockingAction;

        public bool IsExit()
        {
            // 攻撃時間が終了したら終了
            return currentAttackTime >= attackTime;
        }

        public void Enter()
        {
            // BDD仕様: attackCoolTimeが0.0以下でない場合は攻撃できない
            if (attackCoolTime > 0f)
            {
                Debug.Log("Attack is on cooldown. Ignoring attack input.");
                return;
            }

            Debug.Log("Entered Attack State");
            currentAttackTime = 0f;
            isAttacking = true;
            
            // 攻撃開始処理
            StartAttack();
        }

        public void Update()
        {
            // 攻撃クールタイムの処理
            if (attackCoolTime > 0f)
            {
                attackCoolTime -= Time.deltaTime;
                if (attackCoolTime < 0f) attackCoolTime = 0f;
            }

            if (!isAttacking) return;

            currentAttackTime += Time.deltaTime;

            // 攻撃終了判定
            if (currentAttackTime >= attackTime)
            {
                isAttacking = false;
                // 攻撃インターバル開始
                attackCoolTime = attackInterval;
                Debug.Log("Attack Action Completed");
            }
        }

        public void Input(InputType inputType)
        {
            // 攻撃中は他の入力を受け付けない
            // BlockingActionのため
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在攻撃中かどうか
        /// </summary>
        /// <returns>攻撃中ならtrue</returns>
        public bool IsAttacking()
        {
            return isAttacking;
        }

        /// <summary>
        /// 攻撃が可能かどうか
        /// BDD仕様: attackCoolTimeが0.0以下でない場合は攻撃できない
        /// </summary>
        /// <returns>攻撃可能ならtrue</returns>
        public bool CanAttack()
        {
            return attackCoolTime <= 0f;
        }

        /// <summary>
        /// 現在のクールタイム残り時間
        /// </summary>
        /// <returns>クールタイム残り時間</returns>
        public float GetCooldownRemaining()
        {
            return Mathf.Max(0f, attackCoolTime);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 攻撃開始処理
        /// BDD仕様: 攻撃時のフロー
        /// </summary>
        private void StartAttack()
        {
            // 1. モーションを再生する
            StartAttackAnimation();
            
            // 2. attackCoolTimeを設定し、攻撃状態にする
            // (これはEnterで既に実行済み)
            
            // 3. 攻撃開始時にHitDetectorに処理を渡す
            NotifyHitDetector();
        }

        /// <summary>
        /// 攻撃アニメーション開始
        /// </summary>
        private void StartAttackAnimation()
        {
            var motionController = GetComponentInParent<MotionController>();
            if (motionController != null)
            {
                motionController.ChangeAnimation("Attack");
            }
        }

        /// <summary>
        /// HitDetectorに攻撃処理を通知
        /// BDD仕様: 攻撃発生地点のフレームで判断する
        /// </summary>
        private void NotifyHitDetector()
        {
            // TODO: HitDetectorに攻撃判定を通知
            // 現在のプレイヤー位置での敵・障害物との接触判定を実行
            var hitDetector = FindObjectOfType<HitDetector>();
            if (hitDetector != null)
            {
                // HitDetectorに攻撃イベントを通知
                Debug.Log("Notified HitDetector of attack");
                // hitDetector.OnPlayerAttack(); // TODO: HitDetector実装時に追加
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 初期化
            currentAttackTime = 0f;
            attackCoolTime = 0f;
            isAttacking = false;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // パラメータの検証
            if (attackTime <= 0f) attackTime = 0.5f;
            if (attackInterval <= 0f) attackInterval = 1f;
        }

        #endregion
    }
}