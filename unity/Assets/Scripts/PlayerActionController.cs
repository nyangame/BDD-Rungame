using UnityEngine;
using System.Collections.Generic;

namespace RunGame
{
    /// <summary>
    /// プレイヤーの行動制御をするステートマシン
    /// BDD仕様: spec/code/PlayerActionController.md
    /// </summary>
    public class PlayerActionController : MonoBehaviour
    {
        [Header("Player Actions")]
        [SerializeReference, SubclassSelector]
        private List<IPlayerAction> actions = new List<IPlayerAction>();

        private IPlayerAction currentPlayerActionState;
        private Dictionary<System.Type, IPlayerAction> actionMap = new Dictionary<System.Type, IPlayerAction>();

        #region Properties

        /// <summary>
        /// 現在のステートを返す
        /// </summary>
        public IPlayerAction PlayerActionState => currentPlayerActionState;

        /// <summary>
        /// 現在のアクションタイプ
        /// </summary>
        public ActionTagType CurrentActionTag => currentPlayerActionState?.ActionTag ?? ActionTagType.None;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeStates();
        }

        private void Update()
        {
            UpdateCurrentState();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// 指定したアクションタイプに遷移
        /// </summary>
        /// <typeparam name="T">アクションタイプ</typeparam>
        /// <returns>遷移に成功したらtrue</returns>
        public bool TransitionTo<T>() where T : class, IPlayerAction
        {
            if (actionMap.TryGetValue(typeof(T), out IPlayerAction targetAction))
            {
                return TransitionToAction(targetAction);
            }
            
            Debug.LogWarning($"Action of type {typeof(T).Name} not found!");
            return false;
        }

        /// <summary>
        /// アイドル状態に遷移
        /// </summary>
        /// <returns>遷移に成功したらtrue</returns>
        public bool TransitionToIdle()
        {
            return TransitionTo<IdleAction>();
        }

        /// <summary>
        /// 入力処理
        /// </summary>
        /// <param name="inputType">入力タイプ</param>
        public void ProcessInput(InputType inputType)
        {
            currentPlayerActionState?.Input(inputType);
        }

        /// <summary>
        /// 現在のアクションが指定したタイプかどうか
        /// </summary>
        /// <typeparam name="T">チェックするアクションタイプ</typeparam>
        /// <returns>指定タイプならtrue</returns>
        public bool IsCurrentAction<T>() where T : class, IPlayerAction
        {
            return currentPlayerActionState is T;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ステートの初期化とステートの登録
        /// BDD仕様: Awakeでコンポーネントの初期化とステートの登録をする
        /// </summary>
        private void InitializeStates()
        {
            // アクションマップの構築
            actionMap.Clear();
            
            foreach (var action in actions)
            {
                if (action != null)
                {
                    actionMap[action.GetType()] = action;
                }
            }

            // デフォルトでアイドル状態に設定
            if (actionMap.TryGetValue(typeof(IdleAction), out IPlayerAction idleAction))
            {
                currentPlayerActionState = idleAction;
                currentPlayerActionState.Enter();
            }
            else
            {
                Debug.LogError("IdleAction not found! Please add IdleAction to the actions list.");
            }

            Debug.Log($"PlayerActionController initialized with {actionMap.Count} actions");
        }

        /// <summary>
        /// 現在のステートの更新処理
        /// BDD仕様: Updateでステートの変化を取得、処理すべき項目があれば処理する
        /// </summary>
        private void UpdateCurrentState()
        {
            if (currentPlayerActionState == null) return;

            // 現在のステートの更新
            currentPlayerActionState.Update();

            // 終了条件チェック
            // BDD仕様: IPlayerAction.IsExitが立っているステートからは離脱し、Idleに戻る
            if (currentPlayerActionState.IsExit())
            {
                TransitionToIdle();
            }
        }

        /// <summary>
        /// 指定したアクションに遷移
        /// </summary>
        /// <param name="targetAction">遷移先アクション</param>
        /// <returns>遷移に成功したらtrue</returns>
        private bool TransitionToAction(IPlayerAction targetAction)
        {
            if (targetAction == null) return false;
            if (currentPlayerActionState == targetAction) return true;

            // 現在のアクションがBlockingActionの場合、遷移を拒否
            if (currentPlayerActionState?.ActionTag == ActionTagType.BlockingAction && 
                !currentPlayerActionState.IsExit())
            {
                Debug.Log($"Cannot transition to {targetAction.GetType().Name}: Current action is blocking");
                return false;
            }

            // ステート遷移実行
            Debug.Log($"State transition: {currentPlayerActionState?.GetType().Name} -> {targetAction.GetType().Name}");
            
            currentPlayerActionState = targetAction;
            currentPlayerActionState.Enter();
            
            return true;
        }

        #endregion

        #region Editor Support

        private void OnValidate()
        {
            // エディタでのアクションリスト検証
            if (actions == null)
            {
                actions = new List<IPlayerAction>();
            }

            // 重複チェック（エディタでの設定ミス防止）
            var types = new HashSet<System.Type>();
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                if (actions[i] == null) continue;
                
                var type = actions[i].GetType();
                if (types.Contains(type))
                {
                    Debug.LogWarning($"Duplicate action type found: {type.Name}. Removing duplicate.");
                    actions.RemoveAt(i);
                }
                else
                {
                    types.Add(type);
                }
            }
        }

        #endregion
    }
}