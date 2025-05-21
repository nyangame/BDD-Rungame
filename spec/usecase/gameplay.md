# ゲーム中に期待される振る舞い

## Feature: プレーヤー入力

#### Scenario: 移動入力の処理
  Given いつでも
  When 左右方向キーが押される
  Then MovementInputのx値が1.0になる
  And OnMoveInputイベントが発火する

#### Scenario: 入力の先行受付
  Given プレイヤーがスライディング中である
  When 行動終了のN秒以内にキー入力があった
  Then 入力をキューに積み、行動終了時にイベントを発火させる
  
#### Scenario: ジャンプ入力の処理
  Given プレイヤーが地上にいる
  When △ボタンが押される
  Then IsJumpPressedがtrueになる
  And OnJumpPerformedイベントが発火する
  
#### Scenario: 入力の無効化
  Given ゲームがポーズ状態である
  When DisableAllInputが呼ばれる
  Then すべての入力イベントが発火しなくなる



## Feature: ゲーム中の接触判定



## Feature: ゲームの勝利条件と敗北条件


