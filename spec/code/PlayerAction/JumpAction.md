# ジャンプ状態クラス設計

# 概要
- プレイヤーのジャンプ状態の行動

# 実装
- IPlayerActionを継承する

# 処理フロー
1. Awakeでコンポーネントの初期化
2. Updateで処理を待つ
3. ジャンプ終了したらIsExitでtrueが返せるようにする

# 外部変数
- jumpTime: ジャンプにかかる時間
- jumpEnableStart: ジャンプが有効な時間のはじまり
- jumpEnableEnd: ジャンプが有効な時間のおわり

# 期待値
- jumpTimeの時間を正として判断する
- jumpEnableStart～jumpEnableEndの間のみジャンプ中とみなす
- 上記でジャンプ中に該当しない場合、jumpActionにいても障害物に当たることはあり得る
- ジャンプ中にのみ空中にある配置物は取れる
