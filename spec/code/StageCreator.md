# StageCreatorクラス設計

- MonoBehaviourを継承し、インスペクタから値の調整ができるようにする
- Hierarchyに配置され、ゲームの最初から存在するオブジェクト

- StartのタイミングでステージのPrefabデータを読み込む(PrefabはAddressables管理)
- Updateのタイミングで、プレイヤーの距離が一定以内の場合にマップを作る
- 読み込んだマップデータにあわせて配置物を設定する
