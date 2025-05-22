using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// プレイヤーのモーション制御
    /// BDD仕様: spec/code/MotionController.md
    /// </summary>
    public class MotionController : MonoBehaviour
    {
        [Header("Animator Settings")]
        [SerializeField] private Animator _animator;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// アニメーションが再生中かどうか返す
        /// </summary>
        /// <returns>アニメーション再生中ならtrue</returns>
        public bool IsPlayingAnimation()
        {
            if (_animator == null) return false;
            
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.normalizedTime < 1.0f;
        }

        /// <summary>
        /// アニメーションの切り替え
        /// </summary>
        /// <param name="stateName">アニメーション状態名</param>
        public void ChangeAnimation(string stateName)
        {
            if (_animator == null)
            {
                Debug.LogWarning("Animator is not assigned.");
                return;
            }

            _animator.Play(stateName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            // Animatorが設定されていない場合、子オブジェクトから取得を試みる
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogWarning($"Animator not found on {gameObject.name}. Please assign Animator component.");
            }
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            if (_animator == null && Application.isPlaying == false)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        #endregion
    }
}