# PlayerInputクラス設計

## コードの運用仕様
- Playerクラスから移譲される
- Playerと同じオブジェクトにアタッチされる

## 実装
- MonoBehaviourを継承し、インスペクタからInputSystemの設定ができるようにすること
- InputSystemを使用して操作系は管理する
- キーは「spec/projのキー入力.md」を参考にすること
