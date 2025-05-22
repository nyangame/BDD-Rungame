using UnityEngine;
using Cysharp.Threading.Tasks;

namespace RunGame
{
    /// <summary>
    /// ジャンプ状態クラス
    /// BDD仕様: spec/code/PlayerAction/JumpAction.md
    /// </summary>
    public class JumpAction : MonoBehaviour, IPlayerAction
    {
        [Header("Jump Parameters")]
        [SerializeField] private float jumpTime = 1.0f;
        [SerializeField] private float jumpEnableStart = 0.1f;
        [SerializeField] private float jumpEnableEnd = 0.8f;

        private float currentJumpTime = 0f;
        private bool isJumping = false;

        #region IPlayerAction Implementation

        public ActionTagType ActionTag => ActionTagType.BlockingAction;

        public bool IsExit()
        {
            // ジャンプ時間が終了したら終了
            return currentJumpTime >= jumpTime;
        }

        public void Enter()
        {
            Debug.Log("Entered Jump State");
            currentJumpTime = 0f;
            isJumping = true;
            
            // アニメーション開始などの処理
            StartJumpAnimation();
        }

        public void Update()
        {
            if (!isJumping) return;

            currentJumpTime += Time.deltaTime;

            // ジャンプ終了判定
            if (currentJumpTime >= jumpTime)
            {
                isJumping = false;
                Debug.Log("Jump Action Completed");
            }
        }

        public void Input(InputType inputType)
        {
            // ジャンプ中は他の入力を受け付けない
            // BlockingActionのため
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在ジャンプ中かどうか判定
        /// BDD仕様: jumpEnableStart～jumpEnableEndの間のみジャンプ中とみなす
        /// </summary>
        /// <returns>ジャンプ中ならtrue</returns>
        public bool IsInJumpState()
        {
            if (!isJumping) return false;
            
            float normalizedTime = currentJumpTime / jumpTime;
            return normalizedTime >= jumpEnableStart && normalizedTime <= jumpEnableEnd;
        }

        /// <summary>
        /// 空中にある配置物を取得可能かどうか
        /// BDD仕様: ジャンプ中にのみ空中にある配置物は取れる
        /// </summary>
        /// <returns>空中配置物取得可能ならtrue</returns>
        public bool CanCollectAirborneItems()
        {
            return IsInJumpState();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ジャンプアニメーション開始
        /// </summary>
        private void StartJumpAnimation()
        {
            // MotionControllerへのアニメーション指示
            var motionController = GetComponentInParent<MotionController>();
            if (motionController != null)
            {
                motionController.ChangeAnimation("Jump");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 初期化
            currentJumpTime = 0f;
            isJumping = false;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // jumpEnableStartとjumpEnableEndの値検証
            if (jumpEnableStart < 0f) jumpEnableStart = 0f;
            if (jumpEnableEnd > 1f) jumpEnableEnd = 1f;
            if (jumpEnableStart >= jumpEnableEnd) jumpEnableStart = jumpEnableEnd - 0.1f;
        }

        #endregion
    }
}