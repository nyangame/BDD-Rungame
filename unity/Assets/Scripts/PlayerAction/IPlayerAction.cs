using UnityEngine;

namespace RunGame
{
    public enum ActionTagType
    {
        None,
        FreeMoveAction,   // 移動可能なアクション
        BlockingAction,   // 他のアクションをブロックするアクション
    }

    public enum InputType
    {
        None,
        MoveStick,  // アナログスティック移動
        MoveAxis,   // 軸入力移動
        OK,         // Attack - ◯ボタン
        NG,         // Jump - △ボタン
        Slide,      // スライド - ×ボタン
        Pause
    }

    /// <summary>
    /// プレイヤーアクションのインターフェース
    /// BDD仕様: spec/ref/IPlayerAction.cs
    /// </summary>
    public interface IPlayerAction
    {
        ActionTagType ActionTag { get; }
        
        /// <summary>
        /// アクションを終了するかどうか
        /// </summary>
        bool IsExit();
        
        /// <summary>
        /// アクション開始時の処理
        /// </summary>
        void Enter();
        
        /// <summary>
        /// アクションの更新処理
        /// </summary>
        void Update();
        
        /// <summary>
        /// 入力処理
        /// </summary>
        /// <param name="inputType">入力タイプ</param>
        void Input(InputType inputType);
    }
}