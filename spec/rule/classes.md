# クラスの関係図や役割を定義する

## GameManager
ゲームの進行や変数を管理する

## Player
プレイヤーに関連する処理を統括する  
プレイヤーの入力系統を処理する  
以下を内部に持つ  
- PlayerActionController
- MotionController

## PlayerActionController
プレイヤーが実行する[アクション]を処理するステートマシン  
IPlayerActionStateを継承したアクションを生成/所持し切り替えて運用する  

## MotionController
プレイヤーのモーションを管理する

## StageCreator
ステージを生成する  
StageBehaviourからコールされる  

## StageBehaviour
ステージを移動させる  
ステージをどのくらい進んだかや、配置物の情報を持つ  

## HitDetector
接触判定を統括する  

## IPlacementObject
配置物のインタフェース
配置物のタグ情報と、Actionメソッドを持つ

## Enemy
敵の処理のベースクラス
それぞれの個性的な敵は派生クラスで表現する

## Item
取得可能なアイテム
個性的なアイテムはActionの差で表現する

## Obstacle
障害物。当たると死ぬオブジェクトである


# プロジェクト全体の詳細設計
- 個々の詳細な設計は、code以下にクラス名と同じmdを記載し仕様化する
  - 個々の仕様が無いものは、このページの情報をもとに生成する事
