using UnityEngine;

/// <summary>
/// プレイヤーの入力を管理するクラス
/// </summary>
public class PlayerInput : MonoBehaviour
{
    [Header("Key Settings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.F;
    
    [Header("Touch Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float tapMaxMovement = 10f;

    // タッチ入力用の変数
    private Vector2 _touchStartPosition;
    private Vector2 _touchEndPosition;
    private bool _isTouching = false;
    private float _touchStartTime;
    private float _maxTapDuration = 0.3f;
    
    // スワイプ検出用のクールダウン
    private float _swipeCooldown = 0.2f;
    private float _lastSwipeTime = 0f;

    /// <summary>
    /// ジャンプボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>ジャンプボタンが押されていればtrue</returns>
    public bool IsJumpPressed()
    {
        // キーボード入力
        if (Input.GetKeyDown(jumpKey))
        {
            return true;
        }
        
        // スワイプ上判定
        if (DetectSwipeDirection() == SwipeDirection.Up)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// スライドボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>スライドボタンが押されていればtrue</returns>
    public bool IsSlidePressed()
    {
        // キーボード入力
        if (Input.GetKeyDown(slideKey))
        {
            return true;
        }
        
        // スワイプ下判定
        if (DetectSwipeDirection() == SwipeDirection.Down)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// レーン変更方向を取得
    /// </summary>
    /// <returns>-1:左, 0:変更なし, 1:右</returns>
    public int GetLaneChangeDirection()
    {
        // キーボード入力
        if (Input.GetKeyDown(leftKey))
        {
            return -1;
        }
        
        if (Input.GetKeyDown(rightKey))
        {
            return 1;
        }
        
        // スワイプ左右判定
        SwipeDirection swipeDir = DetectSwipeDirection();
        if (swipeDir == SwipeDirection.Left)
        {
            return -1;
        }
        else if (swipeDir == SwipeDirection.Right)
        {
            return 1;
        }
        
        return 0;
    }

    /// <summary>
    /// 攻撃ボタンが押されたかどうかを判定
    /// </summary>
    /// <returns>攻撃ボタンが押されていればtrue</returns>
    public bool IsAttackPressed()
    {
        // キーボード入力
        if (Input.GetKeyDown(attackKey))
        {
            return true;
        }
        
        // タップ判定
        if (DetectTap())
        {
            return true;
        }
        
        return false;
    }

    private void Update()
    {
        // タッチ入力の検出
        DetectTouchInput();
    }

    /// <summary>
    /// タッチ入力を検出する
    /// </summary>
    private void DetectTouchInput()
    {
        // モバイル端末でない場合はスキップ
        if (!Input.touchSupported && !Application.isEditor)
        {
            return;
        }

        // エディタでマウス入力をタッチとして扱う
        if (Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPosition = Input.mousePosition;
                _touchStartTime = Time.time;
                _isTouching = true;
            }
            else if (Input.GetMouseButtonUp(0) && _isTouching)
            {
                _touchEndPosition = Input.mousePosition;
                _isTouching = false;
            }
        }
        // 実機でのタッチ入力
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                _touchStartPosition = touch.position;
                _touchStartTime = Time.time;
                _isTouching = true;
            }
            else if (touch.phase == TouchPhase.Ended && _isTouching)
            {
                _touchEndPosition = touch.position;
                _isTouching = false;
            }
        }
    }

    /// <summary>
    /// スワイプ方向を検出する
    /// </summary>
    private SwipeDirection DetectSwipeDirection()
    {
        // タッチ操作が終了していない場合は何もしない
        if (_isTouching)
        {
            return SwipeDirection.None;
        }
        
        // クールダウン中は何もしない
        if (Time.time - _lastSwipeTime < _swipeCooldown)
        {
            return SwipeDirection.None;
        }

        // タッチ時間が長すぎる場合はスワイプと見なさない
        if (Time.time - _touchStartTime > _maxTapDuration)
        {
            return SwipeDirection.None;
        }

        // スワイプ方向の判定
        Vector2 swipeDelta = _touchEndPosition - _touchStartPosition;
        float swipeDistance = swipeDelta.magnitude;
        
        if (swipeDistance > swipeThreshold)
        {
            _lastSwipeTime = Time.time;
            
            // 水平方向と垂直方向のどちらが大きいかで方向を判定
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // 左右のスワイプ
                return swipeDelta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                // 上下のスワイプ
                return swipeDelta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }
        
        return SwipeDirection.None;
    }

    /// <summary>
    /// タップを検出する
    /// </summary>
    private bool DetectTap()
    {
        // タッチ操作が終了していない場合は何もしない
        if (_isTouching)
        {
            return false;
        }

        // タッチ時間が長すぎる場合はタップと見なさない
        if (Time.time - _touchStartTime > _maxTapDuration)
        {
            return false;
        }

        // タップ判定
        Vector2 tapDelta = _touchEndPosition - _touchStartPosition;
        float tapDistance = tapDelta.magnitude;
        
        if (tapDistance < tapMaxMovement)
        {
            // タップ動作をリセット
            _touchStartPosition = Vector2.zero;
            _touchEndPosition = Vector2.zero;
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// スワイプ方向を表す列挙型
    /// </summary>
    private enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
