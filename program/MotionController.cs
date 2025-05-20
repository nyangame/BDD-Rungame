using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MotionControllerクラス
/// プレイヤーのモーション管理を担当します
/// </summary>
[RequireComponent(typeof(Animator))]
public class MotionController : MonoBehaviour
{
    #region Inspector Variables
    [Tooltip("アニメーター参照")]
    [SerializeField] private Animator animator;
    
    [Tooltip("アニメーション遷移のブレンド時間")]
    [SerializeField] private float transitionTime = 0.1f;
    
    [Tooltip("モーションごとのサウンドエフェクト")]
    [SerializeField] private AudioClip[] motionSoundEffects;
    
    [Tooltip("サウンド再生用のAudioSource")]
    [SerializeField] private AudioSource audioSource;
    #endregion

    #region Private Variables
    // モーション名とサウンドのマッピング
    private Dictionary<string, AudioClip> motionSounds = new Dictionary<string, AudioClip>();
    
    // 現在のアニメーション状態
    private string currentAnimationState = "Run";
    
    // プレイヤーへの参照
    private Player playerReference;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // アニメーターがなければ自動で取得
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // AudioSourceがなければ自動で取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // プレイヤーの参照を取得
        playerReference = GetComponent<Player>();
        
        // モーションサウンドの設定
        InitializeMotionSounds();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// モーションサウンドを初期化する
    /// </summary>
    private void InitializeMotionSounds()
    {
        // モーションサウンドをマッピング
        for (int i = 0; i < motionSoundEffects.Length; i++)
        {
            if (motionSoundEffects[i] != null)
            {
                // ファイル名からモーション名を抽出（例: "Jump_Sound" -> "Jump"）
                string motionName = motionSoundEffects[i].name.Split('_')[0];
                
                // 重複チェック
                if (!motionSounds.ContainsKey(motionName))
                {
                    motionSounds.Add(motionName, motionSoundEffects[i]);
                }
            }
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        if (animator != null)
        {
            // アニメーターのパラメータをリセット
            ResetAnimatorParameters();
            
            // 初期アニメーション状態
            currentAnimationState = "Run";
        }
    }

    /// <summary>
    /// アニメーターのパラメータをリセットする
    /// </summary>
    private void ResetAnimatorParameters()
    {
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsSliding", false);
        animator.SetBool("IsAttacking", false);
        animator.SetFloat("Speed", 1.0f);
        animator.SetInteger("Lane", 1); // 中央レーン
    }
    #endregion

    #region Animation Control
    /// <summary>
    /// アニメーションを再生する
    /// </summary>
    public void PlayAnimation(string animationName)
    {
        if (animator == null) return;
        
        // 現在と同じアニメーションの場合は何もしない（トリガー系を除く）
        if (currentAnimationState == animationName &&
            animationName != "Attack" &&
            animationName != "TurnLeft" &&
            animationName != "TurnRight" &&
            animationName != "Damage" &&
            animationName != "UseSkill")
        {
            return;
        }
        
        // アニメーション状態の更新
        currentAnimationState = animationName;
        
        // アニメーターパラメータの設定
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
                
            case "Victory":
                animator.SetTrigger("Victory");
                break;
                
            case "Defeat":
                animator.SetTrigger("Defeat");
                break;
        }
        
        // サウンド再生
        PlayMotionSound(animationName);
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

    /// <summary>
    /// レーンパラメータを設定する
    /// </summary>
    public void SetLaneParameter(int lane)
    {
        if (animator != null)
        {
            animator.SetInteger("Lane", lane);
        }
    }
    #endregion

    #region Sound Control
    /// <summary>
    /// モーションに対応したサウンドを再生する
    /// </summary>
    private void PlayMotionSound(string motionName)
    {
        if (audioSource == null) return;
        
        // モーション名に対応するサウンドを検索
        AudioClip soundClip = null;
        if (motionSounds.TryGetValue(motionName, out soundClip) && soundClip != null)
        {
            // サウンド再生
            audioSource.PlayOneShot(soundClip);
        }
    }
    #endregion

    #region Animation Events
    /// <summary>
    /// アニメーションイベントから呼び出されるメソッド：フットステップ
    /// </summary>
    public void OnFootstep()
    {
        // フットステップサウンドの再生
        PlayMotionSound("Footstep");
    }

    /// <summary>
    /// アニメーションイベントから呼び出されるメソッド：攻撃終了
    /// </summary>
    public void OnAttackEnd()
    {
        // 攻撃フラグをリセット
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }
        
        // プレイヤーの状態を更新
        if (playerReference != null)
        {
            // プレイヤークラスに終了通知のメソッドがあれば呼び出す
            // この例では直接実装されていないため、コメントアウト
            // playerReference.OnAttackAnimationEnd();
        }
    }

    /// <summary>
    /// アニメーションイベントから呼び出されるメソッド：ジャンプ着地
    /// </summary>
    public void OnLanding()
    {
        // 着地サウンドの再生
        PlayMotionSound("Landing");
    }

    /// <summary>
    /// アニメーションイベントから呼び出されるメソッド：スライディング終了
    /// </summary>
    public void OnSlideEnd()
    {
        // スライディングフラグをリセット
        if (animator != null)
        {
            animator.SetBool("IsSliding", false);
        }
    }

    /// <summary>
    /// アニメーションイベントから呼び出されるメソッド：スキル使用終了
    /// </summary>
    public void OnSkillEnd()
    {
        // スキル使用後のリセット処理
        // プレイヤーの状態を更新
        if (playerReference != null)
        {
            // プレイヤークラスに終了通知のメソッドがあれば呼び出す
            // この例では直接実装されていないため、コメントアウト
            // playerReference.OnSkillAnimationEnd();
        }
    }
    #endregion
}
