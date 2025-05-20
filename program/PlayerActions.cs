using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤーのアクションを管理するクラス
/// </summary>
public class PlayerActions : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float laneChangeDuration = 0.2f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpDuration = 0.5f;
    
    [Header("Slide Settings")]
    [SerializeField] private float slideDuration = 0.6f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private Transform attackPoint;

    // 状態変数
    private bool _isChangingLane = false;
    private bool _isJumping = false;
    private bool _isSliding = false;
    private float _lastAttackTime = -999f;
    
    // 参照
    private Player _player;
    private IPlayerAction _currentAction = null;

    // ステートマシン用のアクション
    private LaneChangeAction _laneChangeAction;
    private JumpAction _jumpAction;
    private SlideAction _slideAction;
    private AttackAction _attackAction;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(Player player)
    {
        _player = player;

        // 各アクションのインスタンス化
        _laneChangeAction = new LaneChangeAction(this, player);
        _jumpAction = new JumpAction(this, player);
        _slideAction = new SlideAction(this, player);
        _attackAction = new AttackAction(this, player);
    }

    /// <summary>
    /// レーン変更アクション
    /// </summary>
    /// <param name="direction">変更方向 (-1:左, 1:右)</param>
    public void ChangeLane(int direction)
    {
        // アクション実行中は無視
        if (_isChangingLane) return;

        // 現在のレーンから移動先を計算
        int targetLane = _player.CurrentLane + direction;
        
        // レーン範囲チェック
        if (targetLane < 0 || targetLane > 2) return;

        // レーン変更実行
        _isChangingLane = true;
        StartCoroutine(LaneChangeCoroutine(targetLane));
        
        // ステートマシンでのアクション実行
        SetCurrentAction(_laneChangeAction);
    }

    /// <summary>
    /// ジャンプアクション
    /// </summary>
    public void Jump()
    {
        // スライド中またはジャンプ中は無視
        if (_isJumping || _isSliding) return;

        // ジャンプ実行
        _isJumping = true;
        StartCoroutine(JumpCoroutine());
        
        // ステートマシンでのアクション実行
        SetCurrentAction(_jumpAction);
    }

    /// <summary>
    /// スライドアクション
    /// </summary>
    public void Slide()
    {
        // スライド中またはジャンプ中は無視
        if (_isSliding || _isJumping) return;

        // スライド実行
        _isSliding = true;
        StartCoroutine(SlideCoroutine());
        
        // ステートマシンでのアクション実行
        SetCurrentAction(_slideAction);
    }

    /// <summary>
    /// 攻撃アクション
    /// </summary>
    public void Attack()
    {
        // クールダウン中は無視
        if (Time.time - _lastAttackTime < attackCooldown) return;

        // 攻撃実行
        _lastAttackTime = Time.time;
        StartCoroutine(AttackCoroutine());
        
        // ステートマシンでのアクション実行
        SetCurrentAction(_attackAction);
    }

    /// <summary>
    /// レーン変更コルーチン
    /// </summary>
    private IEnumerator LaneChangeCoroutine(int targetLane)
    {
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3((targetLane - 1) * laneWidth, 0, 0);

        // レーン移動のアニメーション
        while (Time.time < startTime + laneChangeDuration)
        {
            float t = (Time.time - startTime) / laneChangeDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 位置を確定
        transform.position = targetPos;
        _player.SetLane(targetLane);
        _isChangingLane = false;
    }

    /// <summary>
    /// ジャンプコルーチン
    /// </summary>
    private IEnumerator JumpCoroutine()
    {
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        
        // ジャンプのアニメーション
        while (Time.time < startTime + jumpDuration)
        {
            float t = (Time.time - startTime) / jumpDuration;
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = new Vector3(startPos.x, height, startPos.z);
            yield return null;
        }

        // 位置を確定
        transform.position = new Vector3(startPos.x, 0, startPos.z);
        _isJumping = false;
    }

    /// <summary>
    /// スライドコルーチン
    /// </summary>
    private IEnumerator SlideCoroutine()
    {
        // スライド開始
        // ここでキャラクターのコライダーサイズを変更したり、アニメーションを再生したりする

        // スライド継続
        yield return new WaitForSeconds(slideDuration);

        // スライド終了
        // ここでキャラクターのコライダーサイズを元に戻したり、アニメーションを停止したりする
        _isSliding = false;
    }

    /// <summary>
    /// 攻撃コルーチン
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        // 攻撃エフェクト生成
        if (attackEffectPrefab != null && attackPoint != null)
        {
            GameObject attackEffect = Instantiate(attackEffectPrefab, attackPoint.position, attackPoint.rotation);
            Destroy(attackEffect, 0.5f);
        }

        // 攻撃判定
        // ここで敵との当たり判定を行う
        // 仕様では当たり判定はグリッドベースとのことなので、実装は別クラスで行う

        yield return null;
    }

    /// <summary>
    /// 現在のアクションを設定
    /// </summary>
    private void SetCurrentAction(IPlayerAction action)
    {
        if (_currentAction != null)
        {
            _currentAction.Exit();
        }
        
        _currentAction = action;
        _currentAction.Enter();
    }

    /// <summary>
    /// アクションが実行中かどうかを判定
    /// </summary>
    public bool IsActionInProgress()
    {
        return _isChangingLane || _isJumping || _isSliding;
    }
}

/// <summary>
/// プレイヤーアクションのインターフェース
/// </summary>
public interface IPlayerAction
{
    void Enter();
    void Update();
    void Exit();
}

/// <summary>
/// レーン変更アクション
/// </summary>
public class LaneChangeAction : IPlayerAction
{
    private PlayerActions _actions;
    private Player _player;

    public LaneChangeAction(PlayerActions actions, Player player)
    {
        _actions = actions;
        _player = player;
    }

    public void Enter()
    {
        // レーン変更開始処理
        // 例: アニメーション再生
    }

    public void Update()
    {
        // レーン変更中の更新処理
    }

    public void Exit()
    {
        // レーン変更終了処理
    }
}

/// <summary>
/// ジャンプアクション
/// </summary>
public class JumpAction : IPlayerAction
{
    private PlayerActions _actions;
    private Player _player;

    public JumpAction(PlayerActions actions, Player player)
    {
        _actions = actions;
        _player = player;
    }

    public void Enter()
    {
        // ジャンプ開始処理
        // 例: アニメーション再生
    }

    public void Update()
    {
        // ジャンプ中の更新処理
    }

    public void Exit()
    {
        // ジャンプ終了処理
    }
}

/// <summary>
/// スライドアクション
/// </summary>
public class SlideAction : IPlayerAction
{
    private PlayerActions _actions;
    private Player _player;

    public SlideAction(PlayerActions actions, Player player)
    {
        _actions = actions;
        _player = player;
    }

    public void Enter()
    {
        // スライド開始処理
        // 例: アニメーション再生、コライダーサイズ変更
    }

    public void Update()
    {
        // スライド中の更新処理
    }

    public void Exit()
    {
        // スライド終了処理
        // 例: アニメーション停止、コライダーサイズ復元
    }
}

/// <summary>
/// 攻撃アクション
/// </summary>
public class AttackAction : IPlayerAction
{
    private PlayerActions _actions;
    private Player _player;

    public AttackAction(PlayerActions actions, Player player)
    {
        _actions = actions;
        _player = player;
    }

    public void Enter()
    {
        // 攻撃開始処理
        // 例: アニメーション再生
    }

    public void Update()
    {
        // 攻撃中の更新処理
    }

    public void Exit()
    {
        // 攻撃終了処理
    }
}
