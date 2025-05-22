# ゲーム中に期待される振る舞い

## Feature: プレーヤー入力

#### Scenario: 移動入力の処理
  Given いつでも  
  When 左右方向キーが押される  
  Then PlayerがMoveイベントを処理する  

#### Scenario: 入力の先行受付
  Given プレイヤーが[アクション]中である  
  When 行動終了のN秒以内にキー入力があった  
  Then 入力をキューに積み、行動終了時にイベントを発火させる  
  
#### Scenario: ジャンプ入力の処理
  Given プレイヤーが[アイドル状態]  
  When △ボタンが押される  
  Then PlayerActionクラスがステートをJumpActionステートに遷移させる  
  And PlayerActionがJumpActionステートの処理を開始する  

#### Scenario: スライディング入力の処理
  Given プレイヤーが[アイドル状態]  
  When ×ボタンが押される  
  Then PlayerActionクラスがステートをSlidingActionステートに遷移させる  
  And PlayerActionがSlidingActionステートの処理を開始する  

#### Scenario: 攻撃入力の処理
  Given プレイヤーが[アイドル状態]  
  When ◯ボタンが押される  
  Then PlayerActionクラスがステートをAttackActionステートに遷移させる  
  And PlayerActionがAttackActionステートの処理を開始する  

#### Scenario: 入力の無効化
  Given ゲームが[ポーズ状態]である  
  When なんらかのキー入力があった  
  Then 入力を処理しない  



## Feature: ゲーム中の接触判定

#### Scenario: 毎フレームの接触判定の起動
  Given 常に  
  When PlayerのUpdateで  
  Then HitDetectorがStageBehaviourから現在の[セル]の情報を取得  
  And 前回のセルと差分があれば判定処理を行う  


## Feature: ゲームの勝利条件と敗北条件
  Given 常に  
  When PlayerのUpdateで  
  Then 5000m走破したらゲームクリア  

  Given 接触判定時  
  When HitDetectorDeathObjectTagとの接触を検知した  
  Then ゲームオーバー  


