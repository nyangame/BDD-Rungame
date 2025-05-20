using UnityEngine;

/// <summary>
/// プレイヤーキャラクターの制御を担当するクラス
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private float speedIncreaseRate = 0.1f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("References")]
    [SerializeField] private Transform playerModel;

    // 委譲クラスへの参照
    private PlayerInput _playerInput;
    private PlayerActions _playerActions;
    private MotionController _motionController;

    // プレイヤーの状態変数
    private float _currentSpeed;
    private int _currentLane = 1; // 0:左, 1:中央, 2:右
    private float _distance = 0f; // 走行距離
    private bool _isAlive = true;

    // プロパティ
    public float CurrentSpeed => _currentSpeed;
    public int CurrentLane => _currentLane;
    public float Distance => _distance;
    public bool IsAlive => _isAlive;
    public Vector3 Position => transform.position;

    private void Awake()
    {
        // 委譲クラスのインスタンス化
        _playerInput = GetComponent<PlayerInput>();
        _playerActions = GetComponent<PlayerActions>();
        _motionController = GetComponent<MotionController>();

        // 委譲クラスがアタッチされていない場合は自動的に追加
        if (_playerInput == null)
            _playerInput = gameObject.AddComponent<PlayerInput>();
        
        if (_playerActions == null)
            _playerActions = gameObject.AddComponent<PlayerActions>();
            
        if (_motionController == null)
            _motionController = gameObject.AddComponent<MotionController>();
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!_isAlive) return;

        // 速度の更新
        UpdateSpeed();

        // 距離の更新
        UpdateDistance();

        // 入力を処理
        ProcessInput();
    }

    /// <summary>
    /// プレイヤーの初期化
    /// </summary>
    public void Initialize()
    {
        transform.position = Vector3.zero;
        _currentSpeed = initialSpeed;
        _currentLane = 1;
        _distance = 0f;
        _isAlive = true;
        
        // 委譲クラスの初期化
        _playerActions.Initialize(this);
        _motionController.Initialize(playerModel);
    }

    /// <summary>
    /// プレイヤーの速度を更新
    /// </summary>
    private void UpdateSpeed()
    {
        _currentSpeed += speedIncreaseRate * Time.deltaTime;
        _currentSpeed = Mathf.Min(_currentSpeed, maxSpeed);
    }

    /// <summary>
    /// プレイヤーの走行距離を更新
    /// </summary>
    private void UpdateDistance()
    {
        _distance += _currentSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 入力に基づいてプレイヤーのアクションを実行
    /// </summary>
    private void ProcessInput()
    {
        // PlayerInputクラスから入力情報を取得
        bool jumpRequested = _playerInput.IsJumpPressed();
        bool slideRequested = _playerInput.IsSlidePressed();
        int laneChangeDirection = _playerInput.GetLaneChangeDirection();
        bool attackRequested = _playerInput.IsAttackPressed();

        // PlayerActionsクラスで対応するアクションを実行
        if (jumpRequested)
        {
            _playerActions.Jump();
        }
        
        if (slideRequested)
        {
            _playerActions.Slide();
        }
        
        if (laneChangeDirection != 0)
        {
            _playerActions.ChangeLane(laneChangeDirection);
        }
        
        if (attackRequested)
        {
            _playerActions.Attack();
        }
    }

    /// <summary>
    /// レーンを変更
    /// </summary>
    /// <param name="newLane">新しいレーン番号</param>
    public void SetLane(int newLane)
    {
        _currentLane = Mathf.Clamp(newLane, 0, 2);
        // MotionControllerにレーン変更を通知
        _motionController.OnLaneChanged(_currentLane);
    }

    /// <summary>
    /// 衝突判定 (敵や障害物との衝突時に呼び出される)
    /// </summary>
    public void OnCollision()
    {
        if (!_isAlive) return;
        
        Die();
    }

    /// <summary>
    /// プレイヤーの死亡処理
    /// </summary>
    private void Die()
    {
        _isAlive = false;
        _motionController.PlayDeathAnimation();
        
        // ゲームオーバー通知
        GameManager.Instance.OnPlayerDied();
    }
}
