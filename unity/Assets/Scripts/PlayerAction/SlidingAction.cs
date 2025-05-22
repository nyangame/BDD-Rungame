using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// スライディング状態クラス
    /// BDD仕様: spec/code/PlayerAction/SlidingAction.md
    /// </summary>
    public class SlidingAction : MonoBehaviour, IPlayerAction
    {
        [Header("Sliding Parameters")]
        [SerializeField] private float slidingTime = 1.0f;
        [SerializeField] private AnimationCurve slidingMoveCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

        private float currentSlidingTime = 0f;
        private bool isSliding = false;
        private float baseSpeed = 1f;

        #region IPlayerAction Implementation

        public ActionTagType ActionTag => ActionTagType.BlockingAction;

        public bool IsExit()
        {
            // スライディング時間が終了したら終了
            return currentSlidingTime >= slidingTime;
        }

        public void Enter()
        {
            Debug.Log("Entered Sliding State");
            currentSlidingTime = 0f;
            isSliding = true;
            
            // 基本速度を保存
            // TODO: StageBehaviourから現在の速度を取得
            baseSpeed = 1f;
            
            // アニメーション開始
            StartSlidingAnimation();
        }

        public void Update()
        {
            if (!isSliding) return;

            currentSlidingTime += Time.deltaTime;

            // 移動速度の加速処理
            ApplySpeedModification();

            // スライディング終了判定
            if (currentSlidingTime >= slidingTime)
            {
                isSliding = false;
                ResetSpeedModification();
                Debug.Log("Sliding Action Completed");
            }
        }

        public void Input(InputType inputType)
        {
            // スライディング中は他の入力を受け付けない
            // BlockingActionのため
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在スライディング中かどうか
        /// </summary>
        /// <returns>スライディング中ならtrue</returns>
        public bool IsSliding()
        {
            return isSliding;
        }

        /// <summary>
        /// 現在の速度倍率を取得
        /// BDD仕様: slidingMoveCurveに応じて加速される
        /// </summary>
        /// <returns>速度倍率</returns>
        public float GetCurrentSpeedMultiplier()
        {
            if (!isSliding) return 1f;
            
            float normalizedTime = currentSlidingTime / slidingTime;
            return slidingMoveCurve.Evaluate(normalizedTime);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// スライディングアニメーション開始
        /// </summary>
        private void StartSlidingAnimation()
        {
            var motionController = GetComponentInParent<MotionController>();
            if (motionController != null)
            {
                motionController.ChangeAnimation("Slide");
            }
        }

        /// <summary>
        /// 速度変更の適用
        /// BDD仕様: 移動速度がslidingMoveCurveに応じて加速される
        /// </summary>
        private void ApplySpeedModification()
        {
            float speedMultiplier = GetCurrentSpeedMultiplier();
            
            // TODO: StageBehaviourに速度変更を通知
            // StageBehaviourのスクロール速度に影響を与える
            Debug.Log($"Sliding Speed Multiplier: {speedMultiplier:F2}");
        }

        /// <summary>
        /// 速度変更のリセット
        /// </summary>
        private void ResetSpeedModification()
        {
            // TODO: StageBehaviourに速度リセットを通知
            Debug.Log("Reset Speed Modification");
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 初期化
            currentSlidingTime = 0f;
            isSliding = false;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // パラメータの検証
            if (slidingTime <= 0f) slidingTime = 1f;
            
            // AnimationCurveが設定されていない場合のデフォルト設定
            if (slidingMoveCurve.keys.Length == 0)
            {
                slidingMoveCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.5f);
            }
        }

        #endregion
    }
}