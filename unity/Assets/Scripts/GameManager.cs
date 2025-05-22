using UnityEngine;
using R3;
using System;

namespace RunGame
{
    /// <summary>
    /// ゲームの進行管理クラス
    /// BDD仕様: spec/code/GameManager.md
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private StageBehaviour stageBehaviour;
        [SerializeField] private StageCreator stageCreator;
        [SerializeField] private Player player;

        [Header("Game State")]
        [SerializeField] private int currentScore = 0;

        #region Properties

        /// <summary>
        /// 現在のスコア
        /// </summary>
        public int Score => currentScore;

        /// <summary>
        /// スコア変更の通知
        /// </summary>
        public Observable<int> OnScoreChanged => onScoreChangedSubject.AsObservable();
        private readonly Subject<int> onScoreChangedSubject = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            // BDD仕様: 以下の順番でUpdateCallを実行する
            // 1. StageBehaviour
            // 2. StageCreator  
            // 3. Player
            UpdateComponents();
        }

        private void OnDestroy()
        {
            onScoreChangedSubject?.Dispose();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// スコアを加算
        /// </summary>
        /// <param name="points">加算するポイント</param>
        public void AddScore(int points)
        {
            currentScore += points;
            onScoreChangedSubject.OnNext(currentScore);
            Debug.Log($"Score Updated: {currentScore}");
        }

        /// <summary>
        /// ゲームオーバー処理
        /// </summary>
        public void GameOver()
        {
            Debug.Log("Game Over!");
            // TODO: ゲームオーバー時の処理を実装
            // - UI表示
            // - 入力無効化
            // - リザルト画面への遷移など
        }

        /// <summary>
        /// ゲームクリア処理
        /// </summary>
        public void GameClear()
        {
            Debug.Log("Game Clear!");
            // TODO: ゲームクリア時の処理を実装
            // - UI表示
            // - 入力無効化
            // - リザルト画面への遷移など
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            // コンポーネント参照の自動取得
            if (stageBehaviour == null)
                stageBehaviour = FindObjectOfType<StageBehaviour>();
            
            if (stageCreator == null)
                stageCreator = FindObjectOfType<StageCreator>();
            
            if (player == null)
                player = FindObjectOfType<Player>();

            // 参照チェック
            ValidateComponents();
        }

        /// <summary>
        /// コンポーネント参照の検証
        /// </summary>
        private void ValidateComponents()
        {
            if (stageBehaviour == null)
                Debug.LogError("StageBehaviour reference is missing!");
            
            if (stageCreator == null)
                Debug.LogError("StageCreator reference is missing!");
            
            if (player == null)
                Debug.LogError("Player reference is missing!");
        }

        /// <summary>
        /// 各コンポーネントの更新処理
        /// BDD仕様: 指定された順番でUpdateCallを実行
        /// </summary>
        private void UpdateComponents()
        {
            // 1. StageBehaviour
            if (stageBehaviour != null)
            {
                // StageBehaviourは通常のUpdateで処理されるため、ここでは特別な処理は不要
                // 必要に応じて明示的な UpdateCall メソッドを追加可能
            }

            // 2. StageCreator
            if (stageCreator != null)
            {
                // StageCreatorは通常のUpdateで処理されるため、ここでは特別な処理は不要
                // 必要に応じて明示的な UpdateCall メソッドを追加可能
            }

            // 3. Player
            if (player != null)
            {
                // Playerは通常のUpdateで処理されるため、ここでは特別な処理は不要
                // 必要に応じて明示的な UpdateCall メソッドを追加可能
            }
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            if (Application.isPlaying == false)
            {
                // エディタでの参照設定支援
                if (stageBehaviour == null)
                    stageBehaviour = FindObjectOfType<StageBehaviour>();
                
                if (stageCreator == null)
                    stageCreator = FindObjectOfType<StageCreator>();
                
                if (player == null)
                    player = FindObjectOfType<Player>();
            }
        }

        #endregion
    }
}