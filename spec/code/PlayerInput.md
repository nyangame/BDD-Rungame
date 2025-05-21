# PlayerInputクラス設計

# 概要
- InputSystemを活用したプレイヤー入力管理クラス。Playerクラスから移譲され、同じオブジェクトにアタッチされる。

# コードの運用仕様
- Playerクラスから移譲される
- Playerと同じオブジェクトにアタッチされる

# 実装
- MonoBehaviourを継承し、インスペクタからInputSystemの設定ができるようにすること
- InputSystemを使用して操作系は管理する
- キーは「spec/projのキー入力.md」を参考にすること

# 処理フロー
1. Awakeでコンポーネントの初期化とコールバック登録
2. インプットシステムからの入力をクラス内変数に格納
3. 入力変化時に対応するイベントを発火
4. Playerクラスがイベントを購読して動作を実行

# 主要なインターフェース
## パブリックプロパティ
- Vector2 MovementValue: 現在の移動入力値
- bool IsJumpTriggered: ジャンプボタンが押されたか
- bool IsSlideTriggered: スライドボタンが押されたか
- bool IsAttackTriggered: 攻撃ボタンが押されたか

## パブリックメソッド
- void EnableInput(): 入力を有効化
- void DisableInput(): 入力を無効化

## イベント
- OnMove(Vector2 direction): 移動方向が変化した時
- OnJump(): ジャンプボタンが押された時
- OnSlide(): スライドボタンが押された時
- OnAttack(): 攻撃ボタンが押された時
- OnPause(): ポーズボタンが押された時

# 期待値
- 入力から動作までの遅延: 1フレーム以内
- アナログスティックのデッドゾーン: 0.1
- 同時入力があった場合：優先順位は「ジャンプ > スライド > 攻撃」

# エッジケース
- ゲームポーズ中は全ての入力イベント無効
- 特殊アクション中（被ダメージ、死亡）は入力を無視
