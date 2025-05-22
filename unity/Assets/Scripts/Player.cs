using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using System;
using System.Collections.Generic;

namespace RunGame
{
    /// <summary>
    /// プレイヤーの挙動を統括する
    /// BDD仕様: spec/code/Player.md
    /// </summary>
    [RequireComponent(typeof(PlayerActionController))]
    [RequireComponent(typeof(MotionController))]
    public class Player : MonoBehaviour
    {
        [Header("Model Reference")]
        [SerializeField] private Transform playerModel;

        [Header("Input Settings")]
        [SerializeField] private float deadZone = 0.1f;

        [Header("Lane Settings")]
        [SerializeField] private int currentLane = 1; // 0=左, 1=中央, 2=右
        [SerializeField] private float laneWidth = 2.0f;
        [SerializeField] private float laneTransitionSpeed = 5.0f;

        [Header("Input Buffer")]
        [SerializeField] private float bufferTime = 0.2f;
        [SerializeField] private int maxQueueSize = 3;

        // コンポーネント参照
        private PlayerActionController actionController;
        private MotionController motionController;
        private HitDetector hitDetector;

        // 入力関連
        private Queue<InputType> inputQueue = new Queue<InputType>();
        private float lastActionEndTime = 0f;

        // 状態管理
        private bool isPaused = false;

        #region Properties

        /// <summary>
        /// 現在のレーン番号
        /// </summary>
        public int CurrentLane => currentLane;

        /// <summary>
        /// プレイヤーの現在位置
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// 現在のアクション状態
        /// </summary>
        public IPlayerAction CurrentAction => actionController?.PlayerActionState;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            RegisterInputCallbacks();
        }

        private void Update()
        {
            ProcessInputQueue();
            UpdateLanePosition();
            PerformHitDetection();
        }

        private void OnDestroy()
        {
            UnregisterInputCallbacks();
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// 移動入力処理
        /// BDD仕様: 左右方向キーが押されるとPlayerがMoveイベントを処理する
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            if (isPaused) return;

            if (context.performed)
            {
                Vector2 input = context.ReadValue<Vector2>();
                
                // デッドゾーン処理
                if (Mathf.Abs(input.x) > deadZone)
                {
                    if (input.x > 0) // 右移動
                    {
                        MoveLane(1);
                    }
                    else if (input.x < 0) // 左移動
                    {
                        MoveLane(-1);
                    }
                }
            }
        }

        /// <summary>
        /// ジャンプ入力処理
        /// BDD仕様: △ボタンが押されるとJumpActionステートに遷移
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ProcessActionInput(InputType.NG); // NG = Jump
            }
        }

        /// <summary>
        /// スライディング入力処理
        /// BDD仕様: ×ボタンが押されるとSlidingActionステートに遷移
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnSlide(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ProcessActionInput(InputType.Slide);
            }
        }

        /// <summary>
        /// 攻撃入力処理
        /// BDD仕様: ◯ボタンが押されるとAttackActionステートに遷移
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                ProcessActionInput(InputType.OK); // OK = Attack
            }
        }

        /// <summary>
        /// ポーズ入力処理
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnPause(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                TogglePause();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ポーズ状態の切り替え
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;
            Debug.Log($"Game Paused: {isPaused}");
        }

        /// <summary>
        /// 現在アイドル状態かどうか
        /// </summary>
        /// <returns>アイドル状態ならtrue</returns>
        public bool IsIdle()
        {
            return actionController.IsCurrentAction<IdleAction>();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントの初期化とコールバック登録
        /// BDD仕様: Awakeでコンポーネントの初期化とコールバック登録
        /// </summary>
        /// <summary>
        /// コンポーネントの初期化とコールバック登録
        /// BDD仕様: Awakeでコンポーネントの初期化とコールバック登録
        /// </summary>
        private void InitializeComponents()
        {
            actionController = GetComponent<PlayerActionController>();
            motionController = GetComponent<MotionController>();
            hitDetector = FindObjectOfType<HitDetector>();

            // モデル参照の設定
            if (playerModel == null)
            {
                playerModel = transform;
            }

            // 初期レーン位置の設定
            SetLanePosition(currentLane);

            Debug.Log("Player components initialized");
        }

        /// <summary>
        /// 入力コールバックの登録
        /// </summary>
        private void RegisterInputCallbacks()
        {
            // InputSystemのコールバックは基本的にInspectorで設定するため、
            // ここでは特別な処理は不要
            // 必要に応じてプログラマティックな登録を行う
        }

        /// <summary>
        /// 入力コールバックの登録解除
        /// </summary>
        private void UnregisterInputCallbacks()
        {
            // InputSystemのコールバック解除
            // 必要に応じて実装
        }

        /// <summary>
        /// アクション入力の処理
        /// BDD仕様: 入力を処理、PlayerActionStateの状態を見てステートを遷移させる
        /// </summary>
        /// <param name="inputType">入力タイプ</param>
        private void ProcessActionInput(InputType inputType)
        {
            if (isPaused) return;

            // BDD仕様: PlayerActionがIdleでない時は入力を無視
            if (!IsIdle())
            {
                // 先行受付の処理
                // BDD仕様: 行動終了のN秒以内にキー入力があったら入力をキューに積む
                if (Time.time - lastActionEndTime <= bufferTime)
                {
                    EnqueueInput(inputType);
                }
                return;
            }

            // 即座にアクション実行
            ExecuteAction(inputType);
        }

        /// <summary>
        /// アクションの実行
        /// BDD仕様: 同時入力があった場合の優先順位「ジャンプ > スライド > 攻撃」
        /// </summary>
        /// <param name="inputType">入力タイプ</param>
        private void ExecuteAction(InputType inputType)
        {
            bool success = false;

            switch (inputType)
            {
                case InputType.NG: // Jump - 最高優先度
                    success = actionController.TransitionTo<JumpAction>();
                    break;
                case InputType.Slide: // Slide - 中優先度
                    success = actionController.TransitionTo<SlidingAction>();
                    break;
                case InputType.OK: // Attack - 最低優先度
                    success = actionController.TransitionTo<AttackAction>();
                    break;
            }

            if (success)
            {
                Debug.Log($"Action executed: {inputType}");
            }
        }

        /// <summary>
        /// 入力をキューに追加
        /// BDD仕様: 入力をキューに積み、行動終了時にイベントを発火させる
        /// </summary>
        /// <param name="inputType">入力タイプ</param>
        private void EnqueueInput(InputType inputType)
        {
            if (inputQueue.Count >= maxQueueSize)
            {
                // キューが満杯の場合、古い入力を削除
                inputQueue.Dequeue();
            }

            inputQueue.Enqueue(inputType);
            Debug.Log($"Input queued: {inputType} (Queue size: {inputQueue.Count})");
        }

        /// <summary>
        /// 入力キューの処理
        /// </summary>
        private void ProcessInputQueue()
        {
            if (inputQueue.Count == 0) return;
            if (!IsIdle()) return;

            // キューから入力を取り出して実行
            InputType queuedInput = inputQueue.Dequeue();
            ExecuteAction(queuedInput);
            
            lastActionEndTime = Time.time;
        }

        /// <summary>
        /// レーン移動処理
        /// </summary>
        /// <param name="direction">移動方向 (-1:左, 1:右)</param>
        private void MoveLane(int direction)
        {
            int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
            
            if (targetLane != currentLane)
            {
                currentLane = targetLane;
                Debug.Log($"Moving to lane: {currentLane}");
            }
        }

        /// <summary>
        /// レーン位置の更新
        /// </summary>
        private void UpdateLanePosition()
        {
            Vector3 targetPosition = GetLanePosition(currentLane);
            Vector3 currentPosition = transform.position;
            
            // Y座標は常に0を維持
            targetPosition.y = 0f;
            currentPosition.y = 0f;
            
            // スムーズにレーン移動
            if (Vector3.Distance(currentPosition, targetPosition) > 0.01f)
            {
                Vector3 newPosition = Vector3.MoveTowards(
                    currentPosition, 
                    targetPosition, 
                    laneTransitionSpeed * Time.deltaTime
                );
                newPosition.y = 0f; // Y座標を確実に0に固定
                transform.position = newPosition;
            }
        }

        /// <summary>
        /// 指定レーンの座標を取得
        /// </summary>
        /// <param name="lane">レーン番号</param>
        /// <returns>レーン座標</returns>
        private Vector3 GetLanePosition(int lane)
        {
            float x = (lane - 1) * laneWidth; // 中央を0として計算
            return new Vector3(x, 0f, 0f);
        }

        /// <summary>
        /// レーン位置を直接設定
        /// </summary>
        /// <param name="lane">レーン番号</param>
        private void SetLanePosition(int lane)
        {
            Vector3 position = GetLanePosition(lane);
            position.y = 0f;
            transform.position = position;
        }

        /// <summary>
        /// 判定処理を行う
        /// BDD仕様: 判定処理を行う
        /// </summary>
        private void PerformHitDetection()
        {
            if (hitDetector != null)
            {
                hitDetector.HitCheck();
            }
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // パラメータの検証
            if (deadZone < 0f) deadZone = 0f;
            if (deadZone > 1f) deadZone = 1f;
            
            if (laneWidth <= 0f) laneWidth = 2f;
            if (laneTransitionSpeed <= 0f) laneTransitionSpeed = 5f;
            
            if (bufferTime < 0f) bufferTime = 0f;
            if (maxQueueSize <= 0) maxQueueSize = 1;
            
            // レーン番号の制限
            currentLane = Mathf.Clamp(currentLane, 0, 2);
        }

        private void OnDrawGizmosSelected()
        {
            // レーン表示用のギズモ
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 3; i++)
            {
                Vector3 lanePos = GetLanePosition(i);
                Gizmos.DrawWireCube(lanePos, new Vector3(0.5f, 2f, 10f));
            }
            
            // 現在のレーンをハイライト
            if (currentLane >= 0 && currentLane <= 2)
            {
                Gizmos.color = Color.red;
                Vector3 currentLanePos = GetLanePosition(currentLane);
                Gizmos.DrawWireCube(currentLanePos, new Vector3(0.7f, 2.2f, 10f));
            }
        }

        #endregion
    }
}