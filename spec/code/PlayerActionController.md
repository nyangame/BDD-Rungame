# PlayerActionControllerクラス設計

# 概要
- プレイヤーの行動制御をするステートマシン

# 実装
- MonoBehaviourを継承し、インスペクタから行動の設定ができるようにする

# 処理フロー
1. Awakeでコンポーネントの初期化とステートの登録をする
2. Updateでステートの変化を取得、処理すべき項目があれば処理する

# 内部変数
- currentPlayerActionState: 現在のステート

# 外部変数
- Actions: IPlayerActionを継承するクラスのリスト。SerializeReferenceとSubclassSelectorを使用すること。

# 外部インタフェース
- PlayerActionState: 現在のステートを返す

# 期待値
- IPlayerAction.IsExitが立っているステートからは離脱し、Idleに戻る

# エッジケース
- なし
