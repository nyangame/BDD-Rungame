using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体の進行や変数を管理するクラス
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Inspector Variables
    [Header("Game Settings")]
    [Tooltip("ゲームの初期スピード")]
    [SerializeField] private float initialGameSpeed = 5.0f;
    
    [Tooltip("ゲームの最大スピード")]
    [SerializeField] private float maxGameSpeed = 20.0f;
    
    [Tooltip("スピード上昇率（1秒あたり）")]
    [SerializeField] private float speedIncreaseRate = 0.1f;
    
    [Tooltip("ゲームオーバー時の減速率")]
    [SerializeField] private float gameOverSlowdownRate = 0.95f;
    
    [Header("Score Settings")]
    [Tooltip("1メートルあたりのスコア")]
    [SerializeField] private int scorePerMeter = 10;
    
    [Tooltip("アイテム取得時の基本スコア")]
    [SerializeField] private int baseItemScore = 100;
    
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject gameplayUI;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject gameOverUI;
    #endregion

    #region Public Properties
    // 現在のゲームスピード
    public float GameSpeed { get; private set; }
    
    // 現在のスコア
    public int Score { get; private set; }
    
    // 走行距離（メートル）
    public float Distance { get; private set; }
    
    // ゲームの状態
    public GameState CurrentGameState { get; private set; }
    
    // プレイヤーの参照
    public Player PlayerReference { get; private set; }
    #endregion

    #region Private Variables
    // 前回の距離計算時のプレイヤー位置
    private Vector3 lastPlayerPosition;
    
    // スコア更新用のタイマー
    private float scoreUpdateTimer = 0f;
    #endregion

    #region Events
    // ゲーム状態変更イベント
    public event Action<GameState> OnGameStateChanged;
    
    // スコア変更イベント
    public event Action<int> OnScoreChanged;
    
    // ゲームスピード変更イベント
    public event Action<float> OnGameSpeedChanged;
    
    // アイテム取得イベント
    public event Action<ItemType> OnItemCollected;
    
    // ミス（障害物や敵との衝突）イベント
    public event Action OnPlayerMissed;
    
    // ゲームオーバーイベント
    public event Action OnGameOver;
    #endregion

    #region Unity Methods
    private void Start()
    {
        // 初期UIの設定
        SetupUI();
        
        // プレイヤーの参照を取得
        if (PlayerReference == null)
        {
            PlayerReference = FindObjectOfType<Player>();
        }
    }

    private void Update()
    {
        if (CurrentGameState != GameState.Playing) return;
        
        // ゲームスピードの更新
        UpdateGameSpeed();
        
        // 距離とスコアの更新
        UpdateDistanceAndScore();
        
        // ポーズ入力の検出
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
    }
    #endregion

    #region Game State Methods
    /// <summary>
    /// ゲームを初期化する
    /// </summary>
    private void InitializeGame()
    {
        CurrentGameState = GameState.MainMenu;
        GameSpeed = initialGameSpeed;
        Score = 0;
        Distance = 0f;
        
        // デバッグ用：直接ゲームを開始
        #if UNITY_EDITOR
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            StartGame();
        }
        #endif
    }

    /// <summary>
    /// ゲームを開始する
    /// </summary>
    public void StartGame()
    {
        if (CurrentGameState == GameState.MainMenu || CurrentGameState == GameState.GameOver)
        {
            // ゲーム変数のリセット
            GameSpeed = initialGameSpeed;
            Score = 0;
            Distance = 0f;
            
            // プレイヤーの初期化
            if (PlayerReference != null)
            {
                PlayerReference.Initialize();
                lastPlayerPosition = PlayerReference.transform.position;
            }
            
            // ゲーム状態の更新
            SetGameState(GameState.Playing);
            
            // UIの更新
            UpdateUI();
            
            Debug.Log("ゲームを開始しました");
        }
    }

    /// <summary>
    /// ゲームを一時停止する
    /// </summary>
    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            Time.timeScale = 0f;
            
            // UIの更新
            UpdateUI();
            
            Debug.Log("ゲームを一時停止しました");
        }
    }

    /// <summary>
    /// ゲームを再開する
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            Time.timeScale = 1f;
            
            // UIの更新
            UpdateUI();
            
            Debug.Log("ゲームを再開しました");
        }
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    public void GameOver()
    {
        if (CurrentGameState == GameState.Playing)
        {
            SetGameState(GameState.GameOver);
            
            // ゲームオーバー時の演出
            StartCoroutine(GameOverSequence());
            
            Debug.Log("ゲームオーバー");
            
            // イベント発火
            OnGameOver?.Invoke();
        }
    }

    /// <summary>
    /// メインメニューに戻る
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        SetGameState(GameState.MainMenu);
    }

    /// <summary>
    /// ゲーム状態を設定する
    /// </summary>
    private void SetGameState(GameState newState)
    {
        if (CurrentGameState != newState)
        {
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// ゲームオーバー時の演出シーケンス
    /// </summary>
    private IEnumerator GameOverSequence()
    {
        // スローモーション効果
        Time.timeScale = 0.5f;
        
        // ゲームスピードを徐々に下げる
        while (GameSpeed > 0.1f)
        {
            GameSpeed *= gameOverSlowdownRate;
            OnGameSpeedChanged?.Invoke(GameSpeed);
            yield return null;
        }
        
        // スピードをゼロにする
        GameSpeed = 0f;
        OnGameSpeedChanged?.Invoke(GameSpeed);
        
        // 通常の時間スケールに戻す
        Time.timeScale = 1f;
        
        // UIの更新
        UpdateUI();
    }
    #endregion

    #region Gameplay Methods
    /// <summary>
    /// ゲームスピードを更新する
    /// </summary>
    private void UpdateGameSpeed()
    {
        if (GameSpeed < maxGameSpeed)
        {
            GameSpeed += speedIncreaseRate * Time.deltaTime;
            GameSpeed = Mathf.Min(GameSpeed, maxGameSpeed);
            OnGameSpeedChanged?.Invoke(GameSpeed);
        }
    }

    /// <summary>
    /// 距離とスコアを更新する
    /// </summary>
    private void UpdateDistanceAndScore()
    {
        if (PlayerReference == null) return;
        
        // プレイヤーの前方移動距離を計算
        Vector3 movement = PlayerReference.transform.position - lastPlayerPosition;
        float forwardDistance = Vector3.Dot(movement, Vector3.forward);
        
        // 進んだ距離を加算
        if (forwardDistance > 0)
        {
            Distance += forwardDistance;
            
            // スコア更新（一定間隔で）
            scoreUpdateTimer += Time.deltaTime;
            if (scoreUpdateTimer >= 0.1f) // 0.1秒ごとに更新
            {
                int newScorePoints = Mathf.FloorToInt(forwardDistance * scorePerMeter);
                AddScore(newScorePoints);
                scoreUpdateTimer = 0f;
            }
        }
        
        // 現在位置を保存
        lastPlayerPosition = PlayerReference.transform.position;
    }

    /// <summary>
    /// スコアを追加する
    /// </summary>
    public void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>
    /// アイテム取得時の処理
    /// </summary>
    public void CollectItem(ItemType itemType, int bonusPoints = 0)
    {
        // アイテムタイプに応じたスコア加算
        int itemScore = baseItemScore;
        
        switch (itemType)
        {
            case ItemType.Coin:
                itemScore = baseItemScore;
                break;
            case ItemType.PowerUp:
                itemScore = baseItemScore * 2;
                break;
            case ItemType.SpecialItem:
                itemScore = baseItemScore * 5;
                break;
        }
        
        // ボーナスポイントを加算
        itemScore += bonusPoints;
        
        // スコア追加
        AddScore(itemScore);
        
        // イベント発火
        OnItemCollected?.Invoke(itemType);
    }

    /// <summary>
    /// プレイヤーのミス（障害物や敵との衝突）時の処理
    /// </summary>
    public void PlayerMissed()
    {
        // イベント発火
        OnPlayerMissed?.Invoke();
        
        // 現在はミスでゲームオーバーにする
        // 後でライフシステムなどを実装するかもしれない
        GameOver();
    }
    #endregion

    #region UI Methods
    /// <summary>
    /// UIのセットアップ
    /// </summary>
    private void SetupUI()
    {
        // 各UIの参照が設定されているか確認
        if (mainMenuUI == null || gameplayUI == null || pauseMenuUI == null || gameOverUI == null)
        {
            Debug.LogWarning("一部のUI参照が設定されていません");
        }
        
        // 現在の状態に応じたUIを表示
        UpdateUI();
    }

    /// <summary>
    /// UIを更新する
    /// </summary>
    private void UpdateUI()
    {
        // 全てのUIを非表示
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        
        // 現在の状態に応じたUIを表示
        switch (CurrentGameState)
        {
            case GameState.MainMenu:
                if (mainMenuUI != null) mainMenuUI.SetActive(true);
                break;
            case GameState.Playing:
                if (gameplayUI != null) gameplayUI.SetActive(true);
                break;
            case GameState.Paused:
                if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
                break;
            case GameState.GameOver:
                if (gameOverUI != null) gameOverUI.SetActive(true);
                break;
        }
    }
    #endregion
}

/// <summary>
/// ゲームの状態を表す列挙型
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// アイテムの種類を表す列挙型
/// </summary>
public enum ItemType
{
    Coin,
    PowerUp,
    SpecialItem,
    FlavorItem
}
