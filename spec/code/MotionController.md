# MotionControllerクラス設計

# 概要
- プレイヤーのモーション制御をする

# 実装
- MonoBehaviourを継承する
- Animatorの参照を持つ
- 将来的にIKを見据えた設計

# 処理フロー
- このクラスは呼び出しのたびにAnimatorの状態を見てAnimatorの操作をしたり遷移可否を返す

# 内部変数
- _animator: アニメーターの参照

# 外部インタフェース
- bool IsPlayingAnimation(): アニメーションが再生中かどうか返す
- void ChangeAnimation(string state): アニメーションの切り替え

# 期待値
- なし

# エッジケース
- なし
