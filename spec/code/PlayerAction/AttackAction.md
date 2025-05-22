# 攻撃状態クラス設計

# 概要
- プレイヤーの攻撃状態の行動

# 実装
- MonoBehaviourを継承する
- IPlayerActionを継承する

# 処理フロー
1. Awakeでコンポーネントの初期化
2. Updateで処理を待つ、攻撃クールタイムを処理する
3. 攻撃終了したらIsExitでtrueが返せるようにする

# 攻撃時のフロー
1. Actionがコールされる
2. モーションを再生する、attackCoolTimeを設定し、攻撃状態にする
3. 攻撃開始時にHitDetectorに処理を渡す

# 内部変数
- attackCoolTime: 次に攻撃できるまでの時間

# 外部変数
- attackTime: 攻撃にかかる時間
- attackInterval: 攻撃が次に有効になるまでの時間

# 期待値
- 攻撃イベントを検知したらActionがコールされる
- attackCoolTimeが0.0以下でない場合は攻撃できない(処理を無視する)
- 攻撃発生地点のフレームで判断する
