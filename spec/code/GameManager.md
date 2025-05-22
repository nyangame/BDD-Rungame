# GameManagerクラス設計

# 概要
- ゲームの進行管理

# 実装
- スコアの管理
- ゲーム状態の状態管理

# 処理フロー
1. Awakeで各コンポーネントを生成し初期化する
2. Updateで、各コンポーネントのUpdateCallを呼び出す
3. 呼び出す順番を期待値に記す

# 内部変数
各種コンポーネントの参照

# 外部インタフェース
- Score: 現状のスコア

# 期待値
- 以下の順番でUpdateCallを実行する
  - StageBehaviour
  - StageCreator
  - Player
  
# エッジケース
- なし
