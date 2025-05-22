# クラスの関係図や役割を定義する

## Player
プレイヤーに関連する処理を統括する  
プレイヤーの入力系統を処理する  
以下を内部に持つ  
- PlayerAction

## PlayerActionController
プレイヤーが実行する[アクション]を処理するステートマシン  
IPlayerActionStateを継承したアクションを生成/所持し切り替えて運用する  

## StageCreator
ステージを生成する  

## StageDirector
ステージを移動させる  
ステージの情報を持つ  

## HitDetector
接触判定を統括する  

## 
