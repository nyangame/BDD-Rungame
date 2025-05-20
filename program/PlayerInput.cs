using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力を管理するクラス
/// InputSystemを使用して入力を処理します
/// </summary>
public class PlayerInput : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    
    // InputActionリファレンス
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _slideAction;
    private InputAction _attackAction;
    private InputAction _pauseAction;
    
    // 入力値
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _slidePressed;
    private bool _attackPressed;
    private bool _pausePressed;
    
    // Actionsマップ
    private InputActionMap _gameplayActionMap;
    
    private void Awake()
    {
        // InputActionマップを取得
        _gameplayActionMap = inputActions.FindActionMap("Gameplay");
        
        // 各アクションを取得
        _moveAction = _gameplayActionMap.FindAction("Move");
        _jumpAction = _gameplayActionMap.FindAction("Jump");
        _slideAction = _gameplayActionMap.FindAction("Slide");
        _attackAction = _gameplayActionMap.FindAction("Attack");
        _pauseAction = _gameplayActionMap.FindAction("Pause");
        
        // イベントの登録
        _moveAction.performed += OnMove;
        _moveAction.canceled += OnMove;
        
        _jumpAction.performed += OnJump;
        _jumpAction.canceled += OnJump;
        
        _slideAction.performed += OnSlide;
        _slideAction.canceled += OnSlide;
        
        _attackAction.performed += OnAttack;
        _attackAction.canceled += OnAttack;
        
        _pauseAction.performed += OnPause;
    }
    
    private void OnEnable()
    {
        // アクションの有効化
        _gameplayActionMap.Enable();
    }
    
    private void OnDisable()
    {
        // アクションの無効化
        _gameplayActionMap.Disable();
    }
    
    private void OnDestroy()
    {
        // イベントの登録解除
        _moveAction.performed -= OnMove;
        _moveAction.canceled -= OnMove;
        
        _jumpAction.performed -= OnJump;
        _jumpAction.canceled -= OnJump;
        
        _slideAction.performed -= OnSlide;
        _slideAction.canceled -= OnSlide;
        
        _attackAction.performed -= OnAttack;
        _attackAction.canceled -= OnAttack;
        
        _pauseAction.performed -= OnPause;
    }
    
    /// <summary>
    /// 移動入力イベントハンドラ
    /// </summary>
    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// ジャンプ入力イベントハンドラ
    /// </summary>
    private void OnJump(InputAction.CallbackContext context)
    {
        _jumpPressed = context.ReadValueAsButton();
    }
    
    /// <summary>
    /// スライド入力イベントハンドラ
    /// </summary>
    private void OnSlide(InputAction.CallbackContext context)
    {
        _slidePressed = context.ReadValueAsButton();
    }
    
    /// <summary>
    /// 攻撃入力イベントハンドラ
    /// </summary>
    private void OnAttack(InputAction.CallbackContext context)
    {
        _attackPressed = context.ReadValueAsButton();
    }
    
    /// <summary>
    /// ポーズ入力イベントハンドラ
    /// </summary>
    private void OnPause(InputAction.CallbackContext context)
    {
        _pausePressed = context.ReadValueAsButton();
        
        if (_pausePressed)
        {
            // ポーズ処理 (GameManagerに通知など)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
    }
    
    /// <summary>
    /// ジャンプボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>ジャンプボタンが押されたらtrue</returns>
    public bool IsJumpPressed()
    {
        return _jumpPressed;
    }
    
    /// <summary>
    /// スライドボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>スライドボタンが押されたらtrue</returns>
    public bool IsSlidePressed()
    {
        return _slidePressed;
    }
    
    /// <summary>
    /// 攻撃ボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>攻撃ボタンが押されたらtrue</returns>
    public bool IsAttackPressed()
    {
        return _attackPressed;
    }
    
    /// <summary>
    /// レーン変更方向を取得
    /// </summary>
    /// <returns>-1:左, 0:変更なし, 1:右</returns>
    public int GetLaneChangeDirection()
    {
        // 横方向の入力を取得
        float horizontalInput = _moveInput.x;
        
        // デッドゾーン処理
        if (Mathf.Abs(horizontalInput) < 0.5f)
        {
            return 0;
        }
        
        // -1か1を返す
        return (int)Mathf.Sign(horizontalInput);
    }
    
    /// <summary>
    /// 生の移動入力値を取得
    /// </summary>
    /// <returns>移動入力ベクトル</returns>
    public Vector2 GetMoveInput()
    {
        return _moveInput;
    }
    
    /// <summary>
    /// ポーズボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>ポーズボタンが押されたらtrue</returns>
    public bool IsPausePressed()
    {
        return _pausePressed;
    }
    
    /// <summary>
    /// 入力状態をリセット
    /// </summary>
    public void ResetInputs()
    {
        _moveInput = Vector2.zero;
        _jumpPressed = false;
        _slidePressed = false;
        _attackPressed = false;
        _pausePressed = false;
    }
}
