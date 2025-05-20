using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの統括管理を行うクラス
/// MotionControllerにプレイヤーのモーション管理を委譲する
/// </summary>
public class Player : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement Settings")]
    [Tooltip("左右の移動速度")]
    [SerializeField] private float laneChangeSpeed = 5f;
    
    [Tooltip("ジャンプの力")]
    [SerializeField] private float jumpForce = 10f;
    
    [Tooltip("スライディング時の高さ")]
    [SerializeField] private float slidingHeight = 0.5f;
    
    [Tooltip("スライディングの持続時間")]
    [SerializeField] private float slidingDuration = 1.0f;
    
    [Tooltip("レーン間の距離")]
    [SerializeField] private float laneDistance = 3f;
    
    [Header("Detection Settings")]
    [Tooltip("地面検出用のレイキャスト距離")]
    [SerializeField] private float groundDetectionRayDistance = 0.2f;
    
    [Tooltip("前方障害物検出用のレイキャスト距離")]
    [SerializeField] private float forwardDetectionRayDistance = 2f;
    
    [Header("References")]
    [Tooltip("プレイヤーのモーションコントローラー")]
    [SerializeField] private MotionController motionController;
    
    [Tooltip("衝突判定用のコライダー")]
    [SerializeField] private Collider mainCollider;
    
    [Tooltip("スライディング時のコライダー")]
    [SerializeField] private Collider slidingCollider;
    #endregion

    #region Private Variables
    // 現在のレーン位置（0=左, 1=中央, 2=右）
    private int currentLane = 1;
    
    // 目標のX位置
    private float targetPositionX = 0f;
    
    // ジャンプ中かどうか
    private bool isJumping = false;
    
    // スライディング中かどうか
    private bool isSliding = false;
    
    // 地面に接地しているかどうか
    private bool isGrounded = true;
    
    // スキル使用可能かどうか
    private bool canUseSkill = false;
    
    // リジッドボディの参照
    private Rigidbody rb;
    
    // スライディングのタイマー
    private float slidingTimer = 0f;
    
    // 初期の高さ
    private float originalHeight;
    
    // ゲームオーバーかどうか
    private bool isGameOver = false;
    #endregion

    #region Public Properties
    // プレイヤーの現在の状態
    public PlayerState CurrentState { get; private set; }
    
    // 残りのスキルポイント
    public int SkillPoints { get; private set; }
    #endregion

    #region Events
    // 状態変更イベント
    public event Action<PlayerState> OnStateChanged;
    
    // ジャンプイベント
    public event Action OnJump;
    
    // スライディングイベント
    public event Action OnSlide;
    
    // レーン変更イベント
    public event Action<int> OnLaneChange;
    
    // スキル使用イベント
    public event Action OnSkillUsed;
    
    // アイテム取得イベント
    public event Action<ItemType> OnItemCollected;
    
    // ダメージ受けたイベント
    public event Action OnDamageTaken;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // モーションコントローラーがなければ自動で取得
        if (motionController == null)
        {
            motionController = GetComponent<MotionController>();
        }
        
        // コライダーの設定
        if (mainCollider == null)
        {
            mainCollider = GetComponent<Collider>();
        }
        
        // 初期高さの記録
        originalHeight = transform.localScale.y;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (isGameOver) return;
        
        // 入力処理
        HandleInput();
        
        // 移動処理
        HandleMovement();
        
        // ステート更新
        UpdatePlayerState();
        
        // 地面の確認
        CheckGround();
        
        // スライディング制御
        if (isSliding)
        {
            slidingTimer -= Time.deltaTime;
            if (slidingTimer <= 0f)
            {
                StopSliding();
            }
        }
    }

    private void FixedUpdate()
    {
        // 前進処理
        if (!isGameOver && GameManager.Instance != null)
        {
            // ゲームスピードに合わせて前進
            float forwardSpeed = GameManager.Instance.GameSpeed;
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, forwardSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 衝突対象の判定
        if (other.CompareTag("Item"))
        {
            // アイテム取得処理
            CollectItem(other.gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            // 障害物衝突処理
            HitObstacle();
        }
        else if (other.CompareTag("Enemy"))
        {
            // 敵衝突処理
            HitEnemy(other.gameObject);
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// プレイヤーを初期化する
    /// </summary>
    public void Initialize()
    {
        // 変数の初期化
        currentLane = 1;
        targetPositionX = 0f;
        isJumping = false;
        isSliding = false;
        isGrounded = true;
        canUseSkill = false;
        isGameOver = false;
        SkillPoints = 0;
        
        // 位置と回転の初期化
        transform.position = new Vector3(0f, 0f, 0f);
        transform.rotation = Quaternion.identity;
        
        // 物理パラメータの初期化
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // コライダーの初期化
        mainCollider.enabled = true;
        if (slidingCollider != null)
        {
            slidingCollider.enabled = false;
        }
        
        // スケールをリセット
        transform.localScale = new Vector3(transform.localScale.x, originalHeight, transform.localScale.z);
        
        // 初期状態の設定
        SetPlayerState(PlayerState.Running);
        
        // モーションコントローラーの初期化
        if (motionController != null)
        {
            motionController.Initialize();
            motionController.PlayAnimation("Run");
        }
    }
    #endregion

    #region Input Handling
    /// <summary>
    /// 入力を処理する
    /// </summary>
    private void HandleInput()
    {
        // 左右移動
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            ChangeLane(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            ChangeLane(1);
        }
        
        // ジャンプ
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && isGrounded && !isJumping)
        {
            Jump();
        }
        
        // スライディング
        if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.LeftControl)) && isGrounded && !isSliding)
        {
            Slide();
        }
        
        // スキル使用
        if (Input.GetKeyDown(KeyCode.E) && canUseSkill)
        {
            UseSkill();
        }
    }
    #endregion

    #region Movement Control
    /// <summary>
    /// 移動処理を行う
    /// </summary>
    private void HandleMovement()
    {
        // レーン位置に合わせて左右に移動
        targetPositionX = (currentLane - 1) * laneDistance;
        
        // 現在のX位置
        float currentPositionX = transform.position.x;
        
        // 目標位置までスムーズに移動
        float newPositionX = Mathf.Lerp(currentPositionX, targetPositionX, laneChangeSpeed * Time.deltaTime);
        
        // 位置を更新
        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
    }

    /// <summary>
    /// レーンを変更する
    /// </summary>
    private void ChangeLane(int direction)
    {
        int newLane = currentLane + direction;
        
        // レーンの範囲チェック（0=左, 1=中央, 2=右）
        if (newLane >= 0 && newLane <= 2)
        {
            currentLane = newLane;
            
            // イベント発火
            OnLaneChange?.Invoke(currentLane);
            
            // モーションコントローラーにレーン変更を通知
            if (motionController != null)
            {
                // 左に移動
                if (direction < 0)
                {
                    motionController.PlayAnimation("TurnLeft");
                }
                // 右に移動
                else
                {
                    motionController.PlayAnimation("TurnRight");
                }
            }
        }
    }

    /// <summary>
    /// ジャンプする
    /// </summary>
    private void Jump()
    {
        if (!isGrounded || isJumping) return;
        
        isJumping = true;
        isGrounded = false;
        
        // 上方向に力を加える
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // プレイヤーの状態を更新
        SetPlayerState(PlayerState.Jumping);
        
        // イベント発火
        OnJump?.Invoke();
        
        // モーションコントローラーにジャンプを通知
        if (motionController != null)
        {
            motionController.PlayAnimation("Jump");
        }
    }

    /// <summary>
    /// スライディングする
    /// </summary>
    private void Slide()
    {
        if (!isGrounded || isJumping || isSliding) return;
        
        isSliding = true;
        slidingTimer = slidingDuration;
        
        // スライディング用のコライダーに切り替え
        if (mainCollider != null && slidingCollider != null)
        {
            mainCollider.enabled = false;
            slidingCollider.enabled = true;
        }
        
        // キャラクターの高さを下げる
        transform.localScale = new Vector3(transform.localScale.x, slidingHeight, transform.localScale.z);
        
        // プレイヤーの状態を更新
        SetPlayerState(PlayerState.Sliding);
        
        // イベント発火
        OnSlide?.Invoke();
        
        // モーションコントローラーにスライディングを通知
        if (motionController != null)
        {
            motionController.PlayAnimation("Slide");
        }
    }

    /// <summary>
    /// スライディングを終了する
    /// </summary>
    private void StopSliding()
    {
        isSliding = false;
        
        // 通常のコライダーに戻す
        if (mainCollider != null && slidingCollider != null)
        {
            mainCollider.enabled = true;
            slidingCollider.enabled = false;
        }
        
        // キャラクターの高さを元に戻す
        transform.localScale = new Vector3(transform.localScale.x, originalHeight, transform.localScale.z);
        
        // プレイヤーの状態を更新
        SetPlayerState(PlayerState.Running);
        
        // モーションコントローラーに走りを通知
        if (motionController != null)
        {
            motionController.PlayAnimation("Run");
        }
    }

    /// <summary>
    /// スキルを使用する
    /// </summary>
    private void UseSkill()
    {
        if (!canUseSkill) return;
        
        // スキルポイントを消費
        SkillPoints--;
        
        // スキルポイントがなくなったら使用不可に
        if (SkillPoints <= 0)
        {
            canUseSkill = false;
        }
        
        // イベント発火
        OnSkillUsed?.Invoke();
        
        // モーションコントローラーにスキル使用を通知
        if (motionController != null)
        {
            motionController.PlayAnimation("UseSkill");
        }
        
        // スキル効果の実装（後で実装）
        Debug.Log("スキルを使用しました");
    }
    #endregion

    #region State Management
    /// <summary>
    /// プレイヤーの状態を設定する
    /// </summary>
    private void SetPlayerState(PlayerState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            
            // イベント発火
            OnStateChanged?.Invoke(CurrentState);
        }
    }

    /// <summary>
    /// プレイヤーの状態を更新する
    /// </summary>
    private void UpdatePlayerState()
    {
        // 地面に着地した場合
        if (isGrounded && CurrentState == PlayerState.Jumping)
        {
            isJumping = false;
            
            // スライディング中でなければ走り状態に
            if (!isSliding)
            {
                SetPlayerState(PlayerState.Running);
                
                // モーションコントローラーに走りを通知
                if (motionController != null)
                {
                    motionController.PlayAnimation("Run");
                }
            }
        }
    }

    /// <summary>
    /// 地面に接地しているかを確認する
    /// </summary>
    private void CheckGround()
    {
        // 下方向へのレイキャスト
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundDetectionRayDistance))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
        else
        {
            isGrounded = false;
        }
    }
    #endregion

    #region Collision Handling
    /// <summary>
    /// アイテムを取得する
    /// </summary>
    private void CollectItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        
        if (item != null)
        {
            // アイテムの効果を適用
            ItemType itemType = item.GetItemType();
            
            // アイテムの種類に応じた処理
            switch (itemType)
            {
                case ItemType.Coin:
                    // スコア加算
                    break;
                case ItemType.PowerUp:
                    // パワーアップ効果
                    AddSkillPoint();
                    break;
                case ItemType.SpecialItem:
                    // 特殊効果
                    break;
                case ItemType.FlavorItem:
                    // フレーバーアイテム（図鑑登録など）
                    break;
            }
            
            // GameManagerに通知
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectItem(itemType);
            }
            
            // イベント発火
            OnItemCollected?.Invoke(itemType);
            
            // アイテムを非表示にする
            item.Collect();
        }
    }

    /// <summary>
    /// 障害物に衝突した
    /// </summary>
    private void HitObstacle()
    {
        // ダメージ処理
        TakeDamage();
    }

    /// <summary>
    /// 敵に衝突した
    /// </summary>
    private void HitEnemy(GameObject enemyObject)
    {
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            // 攻撃中の場合は敵を倒す
            if (CurrentState == PlayerState.Attacking)
            {
                enemy.Defeat();
            }
            else
            {
                // ダメージ処理
                TakeDamage();
            }
        }
    }

    /// <summary>
    /// ダメージを受ける
    /// </summary>
    private void TakeDamage()
    {
        // イベント発火
        OnDamageTaken?.Invoke();
        
        // モーションコントローラーにダメージを通知
        if (motionController != null)
        {
            motionController.PlayAnimation("Damage");
        }
        
        // GameManagerに通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerMissed();
        }
        
        // ゲームオーバー状態に
        isGameOver = true;
    }

    /// <summary>
    /// 攻撃する
    /// </summary>
    public void Attack()
    {
        // プレイヤーの状態を更新
        SetPlayerState(PlayerState.Attacking);
        
        // モーションコントローラーに攻撃を通知
        if (motionController != null)
        {
            motionController.PlayAnimation("Attack");
        }
        
        // 攻撃後、少し待ってから走り状態に戻す
        StartCoroutine(ReturnToRunningState(0.5f));
    }

    /// <summary>
    /// 走り状態に戻るコルーチン
    /// </summary>
    private IEnumerator ReturnToRunningState(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 走り状態に戻す
        SetPlayerState(PlayerState.Running);
        
        // モーションコントローラーに走りを通知
        if (motionController != null && !isGameOver)
        {
            motionController.PlayAnimation("Run");
        }
    }
    #endregion

    #region Skill Management
    /// <summary>
    /// スキルポイントを追加する
    /// </summary>
    public void AddSkillPoint()
    {
        SkillPoints++;
        canUseSkill = true;
        
        Debug.Log("スキルポイントを獲得しました: " + SkillPoints);
    }
    #endregion
}

/// <summary>
/// プレイヤーの状態を表す列挙型
/// </summary>
public enum PlayerState
{
    Running,
    Jumping,
    Sliding,
    Attacking,
    Damaged
}

/// <summary>
/// プレイヤーのモーション管理を行うクラス
/// </summary>
[RequireComponent(typeof(Animator))]
public class MotionController : MonoBehaviour
{
    #region Inspector Variables
    [Tooltip("アニメーター参照")]
    [SerializeField] private Animator animator;
    
    [Tooltip("アニメーション遷移のブレンド時間")]
    [SerializeField] private float transitionTime = 0.1f;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // アニメーターがなければ自動で取得
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    #endregion

    #region Animation Control
    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        if (animator != null)
        {
            // アニメーターのパラメータをリセット
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsSliding", false);
            animator.SetBool("IsAttacking", false);
            animator.SetFloat("Speed", 1.0f);
        }
    }

    /// <summary>
    /// アニメーションを再生する
    /// </summary>
    public void PlayAnimation(string animationName)
    {
        if (animator == null) return;
        
        switch (animationName)
        {
            case "Run":
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsSliding", false);
                animator.SetBool("IsAttacking", false);
                break;
            case "Jump":
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsSliding", false);
                break;
            case "Slide":
                animator.SetBool("IsSliding", true);
                animator.SetBool("IsJumping", false);
                break;
            case "Attack":
                animator.SetBool("IsAttacking", true);
                
                // 攻撃アニメーション終了時に自動でフラグをリセットするためのトリガー
                animator.SetTrigger("Attack");
                break;
            case "TurnLeft":
                animator.SetTrigger("TurnLeft");
                break;
            case "TurnRight":
                animator.SetTrigger("TurnRight");
                break;
            case "Damage":
                animator.SetTrigger("Damage");
                break;
            case "UseSkill":
                animator.SetTrigger("UseSkill");
                break;
        }
    }

    /// <summary>
    /// アニメーションスピードを設定する
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }
    }
    #endregion
}

/// <summary>
/// アイテムの基底クラス
/// </summary>
public class Item : MonoBehaviour, IPoolable
{
    #region Inspector Variables
    [Tooltip("アイテムの種類")]
    [SerializeField] private ItemType itemType;
    
    [Tooltip("回転速度")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("取得エフェクトのPrefab")]
    [SerializeField] private GameObject collectEffectPrefab;
    #endregion

    #region Private Variables
    // オブジェクトプールの参照
    private ObjectPool pool;
    #endregion

    #region Unity Methods
    private void Update()
    {
        // アイテムを回転させる
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    #endregion

    #region Item Methods
    /// <summary>
    /// アイテムの種類を取得する
    /// </summary>
    public ItemType GetItemType()
    {
        return itemType;
    }

    /// <summary>
    /// アイテムを取得した時の処理
    /// </summary>
    public void Collect()
    {
        // 取得エフェクトを生成
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // オブジェクトプールに戻す
        ReturnToPool();
    }
    #endregion

    #region IPoolable Implementation
    /// <summary>
    /// プールの初期化
    /// </summary>
    public void Initialize(ObjectPool objectPool)
    {
        pool = objectPool;
    }

    /// <summary>
    /// プールに戻す
    /// </summary>
    public void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnObject(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion
}

/// <summary>
/// 敵の基底クラス
/// </summary>
public class Enemy : MonoBehaviour, IPoolable
{
    #region Inspector Variables
    [Tooltip("敵の種類")]
    [SerializeField] protected EnemyType enemyType;
    
    [Tooltip("敵の速度")]
    [SerializeField] protected float moveSpeed = 2f;
    
    [Tooltip("敵の体力")]
    [SerializeField] protected int health = 1;
    
    [Tooltip("倒された時のエフェクトPrefab")]
    [SerializeField] protected GameObject defeatEffectPrefab;
    #endregion

    #region Private Variables
    // オブジェクトプールの参照
    protected ObjectPool pool;
    
    // アニメーターの参照
    protected Animator animator;
    
    // 倒されたかどうか
    protected bool isDefeated = false;
    #endregion

    #region Unity Methods
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        if (!isDefeated)
        {
            // 敵の動き
            Move();
        }
    }
    #endregion

    #region Enemy Methods
    /// <summary>
    /// 敵の移動
    /// </summary>
    protected virtual void Move()
    {
        // 基底クラスでは何もしない（派生クラスでオーバーライド）
    }

    /// <summary>
    /// 敵を倒す
    /// </summary>
    public virtual void Defeat()
    {
        if (isDefeated) return;
        
        isDefeated = true;
        
        // 倒されたアニメーション
        if (animator != null)
        {
            animator.SetTrigger("Defeat");
        }
        
        // 倒されたエフェクト
        if (defeatEffectPrefab != null)
        {
            Instantiate(defeatEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // しばらく待ってからプールに戻す
        StartCoroutine(ReturnToPoolDelayed(1.5f));
    }

    /// <summary>
    /// 遅延してプールに戻すコルーチン
    /// </summary>
    protected IEnumerator ReturnToPoolDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }
    #endregion

    #region IPoolable Implementation
    /// <summary>
    /// プールの初期化
    /// </summary>
    public virtual void Initialize(ObjectPool objectPool)
    {
        pool = objectPool;
        isDefeated = false;
        health = 1;
    }

    /// <summary>
    /// プールに戻す
    /// </summary>
    public virtual void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnObject(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion
}

/// <summary>
/// 敵の種類を表す列挙型
/// </summary>
public enum EnemyType
{
    Basic,
    Flying,
    Jumping,
    Charging
}
