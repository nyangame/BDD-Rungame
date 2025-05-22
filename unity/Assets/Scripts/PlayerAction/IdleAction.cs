using UnityEngine;

namespace RunGame
{
    /// <summary>
    /// アイドル状態クラス
    /// BDD仕様: spec/code/PlayerAction/IdleAction.md
    /// </summary>
    public class IdleAction : MonoBehaviour, IPlayerAction
    {
        #region IPlayerAction Implementation

        public ActionTagType ActionTag => ActionTagType.FreeMoveAction;

        public bool IsExit()
        {
            // アイドル状態は常に他のアクションに遷移可能
            return false;
        }

        public void Enter()
        {
            // アイドル状態開始時の処理（特になし）
            Debug.Log("Entered Idle State");
        }

        public void Update()
        {
            // アイドル状態では何もしない
            // BDD仕様: なにもしない
        }

        public void Input(InputType inputType)
        {
            // アイドル状態では入力を受け付けない
            // 入力処理はPlayerActionControllerで管理される
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // コンポーネントの初期化（特になし）
        }

        #endregion
    }
}