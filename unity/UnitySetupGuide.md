# Unity セットアップガイド - Rungame

## 1. 前提条件

### 必要なパッケージ
```
Window → Package Manager で以下をインストール:
- Input System (Unity公式)
- Addressable Asset System (Unity公式)
- R3 (https://github.com/Cysharp/R3)
- UniTask (https://github.com/Cysharp/UniTask)
- SubclassSelector (https://github.com/mackysoft/Unity-SerializeReferenceExtensions)
```

### プロジェクト設定
```
Edit → Project Settings → Player → Configuration
- Scripting Backend: IL2CPP (推奨)
- Api Compatibility Level: .NET Standard 2.1

Edit → Project Settings → XR Plug-in Management → Input System Package
- Input System Package (new): チェック
- Both: チェック解除 (警告が出る場合)
```

## 2. シーン構成

### InGame シーン階層構造
```
InGame (Scene)
├── GameManager
│   ├── GameManager.cs
│   └── StageCreator.cs
├── Player
│   ├── Player.cs
│   ├── PlayerActionController.cs
│   ├── MotionController.cs
│   ├── HitDetector.cs
│   └── PlayerModel (子オブジェクト)
│       └── Animator
├── StageParent (Empty GameObject)
│   └── (動的生成されるステージブロック)
├── BackgroundParent (Empty GameObject)
│   └── (背景オブジェクト)
├── UI
│   ├── Canvas
│   ├── ScoreText
│   └── GameOverPanel
└── Lighting
    ├── Directional Light
    └── Skybox
```

## 3. ステップバイステップ構築手順

### Step 1: GameManager の作成

1. **空のGameObjectを作成**
   ```
   右クリック → Create Empty → 名前を "GameManager" に変更
   Transform: (0, 0, 0)
   ```

2. **GameManager.cs をアタッチ**
   ```
   GameManager オブジェクトを選択
   Add Component → Scripts → GameManager
   ```

3. **StageCreator.cs をアタッチ**
   ```
   同じ GameManager オブジェクトに
   Add Component → Scripts → StageCreator
   ```

### Step 2: Player の作成

1. **Playerオブジェクトの作成**
   ```
   右クリック → 3D Object → Capsule → 名前を "Player" に変更
   Transform Position: (0, 0, 0)
   Transform Scale: (0.5, 1, 0.5)
   ```

2. **コンポーネントのアタッチ**
   ```
   Player オブジェクトを選択して以下を追加:
   - Player.cs
   - PlayerActionController.cs
   - MotionController.cs
   - HitDetector.cs
   ```

3. **PlayerModelの作成**
   ```
   Player の子オブジェクトとして:
   右クリック → Create Empty → 名前を "PlayerModel" に変更
   
   PlayerModel の子として:
   右クリック → 3D Object → Cube (実際のキャラクターモデル)
   ```

4. **Animatorの設定**
   ```
   PlayerModel を選択
   Add Component → Animator
   Controller: 新しいAnimator Controllerを作成してアサイン
   ```

### Step 3: ステージ構造の作成

1. **StageParent の作成**
   ```
   右クリック → Create Empty → 名前を "StageParent" に変更
   Transform: (0, 0, 0)
   ```

2. **BackgroundParent の作成**
   ```
   右クリック → Create Empty → 名前を "BackgroundParent" に変更
   Transform: (0, 0, 0)
   ```

3. **StageBehaviour の作成**
   ```
   GameManager オブジェクトを選択
   Add Component → Scripts → StageBehaviour
   ```

### Step 4: Input System の設定

1. **Input Actions Asset の作成**
   ```
   Project ウィンドウで右クリック
   Create → Input Actions → 名前を "PlayerInputActions" に変更
   ```

2. **Input Actions の設定**
   ```
   PlayerInputActions をダブルクリックして編集
   
   Action Maps: Player
   ├── Move (Value, Vector2)
   │   └── Binding: WASD, Arrow Keys
   ├── Jump (Button)
   │   └── Binding: Space, Gamepad South
   ├── Slide (Button)
   │   └── Binding: S, Gamepad East
   ├── Attack (Button)
   │   └── Binding: Mouse Left, Gamepad West
   └── Pause (Button)
       └── Binding: Escape
   ```

3. **Player Input Component の追加**
   ```
   Player オブジェクトを選択
   Add Component → Player Input
   Actions: PlayerInputActions をアサイン
   ```

### Step 5: UI システムの作成

1. **Canvas の作成**
   ```
   右クリック → UI → Canvas
   Canvas Scaler: Scale With Screen Size
   Reference Resolution: 1920x1080
   ```

2. **スコア表示の作成**
   ```
   Canvas の子として:
   右クリック → UI → Text - TextMeshPro → 名前を "ScoreText" に変更
   Anchor: Top Left
   Position: (100, -50, 0)
   Text: "Score: 0"
   ```

## 4. 参照のアサイン手順

### GameManager の設定

1. **GameManager.cs の設定**
   ```
   GameManager オブジェクトを選択
   Inspector で以下をアサイン:
   
   ├── Stage Behaviour: StageBehaviour component
   ├── Stage Creator: StageCreator component
   └── Player: Player GameObject
   ```

### Player の設定

1. **Player.cs の設定**
   ```
   Player オブジェクトを選択
   Inspector で以下をアサイン:
   
   ├── Player Model: PlayerModel GameObject
   ├── Current Lane: 1
   ├── Lane Width: 2.0
   ├── Lane Transition Speed: 5.0
   ├── Dead Zone: 0.1
   ├── Buffer Time: 0.2
   └── Max Queue Size: 3
   ```

2. **PlayerActionController.cs の設定**
   ```
   Player オブジェクトを選択
   Inspector で PlayerActionController を見つけて:
   
   Actions (List):
   ├── [0] IdleAction
   ├── [1] JumpAction
   ├── [2] SlidingAction
   └── [3] AttackAction
   
   各Actionの設定:
   - SubclassSelector で適切なクラスを選択
   - パラメータを設定
   ```

3. **MotionController.cs の設定**
   ```
   Player オブジェクトを選択
   Inspector で以下をアサイン:
   
   └── Animator: PlayerModel の Animator component
   ```

4. **Player Input の設定**
   ```
   Player オブジェクトを選択
   Player Input component で:
   
   ├── Actions: PlayerInputActions
   ├── Default Map: Player
   └── Behavior: Send Messages
   
   Events:
   ├── Move → Player.OnMove
   ├── Jump → Player.OnJump
   ├── Slide → Player.OnSlide
   ├── Attack → Player.OnAttack
   └── Pause → Player.OnPause
   ```

### StageBehaviour の設定

1. **StageBehaviour.cs の設定**
   ```
   GameManager オブジェクトを選択
   Inspector で StageBehaviour を見つけて:
   
   ├── Scroll Speed Min: 1.0
   ├── Scroll Speed Max: 10.0
   ├── Max Gear: 5
   ├── Stage Parent: StageParent GameObject
   └── Background Parent: BackgroundParent GameObject
   ```

### StageCreator の設定

1. **StageCreator.cs の設定**
   ```
   GameManager オブジェクトを選択
   Inspector で StageCreator を見つけて:
   
   ├── Generate Ahead Distance: 300
   ├── Max Active Blocks: 5
   ├── Stage Data Label: "StageData"
   └── Stage Block Prefabs: (Addressable Assets)
   ```

## 5. Addressable Assets の設定

### StageData の作成

1. **StageData Asset の作成**
   ```
   Project ウィンドウで右クリック
   Create → RunGame → Stage Data
   名前を "TestStage01" に変更
   ```

2. **StageData の設定**
   ```
   TestStage01 を選択
   Inspector で:
   
   ├── Map Model: 適当なMeshをアサイン
   ├── Block Size: 100
   ├── Lane Num: 3
   └── Context Menu → Generate Sample Data (右クリック)
   ```

3. **Addressable 設定**
   ```
   TestStage01 を選択
   Inspector で Addressable にチェック
   Address: "TestStage01"
   Label: "StageData" を追加
   ```

### 配置物Prefab の作成

1. **CoinItem Prefab**
   ```
   3D Object → Sphere → 名前を "CoinItem" に変更
   Scale: (0.5, 0.5, 0.5)
   Add Component → CoinItem.cs
   Material: 黄色のマテリアル
   
   Project にドラッグしてPrefab化
   Addressable 設定: Address "CoinItem", Label "PlacementObjects"
   ```

2. **Obstacle Prefab**
   ```
   3D Object → Cube → 名前を "BasicObstacle" に変更
   Scale: (1, 1, 1)
   Add Component → Obstacle.cs
   Material: 赤色のマテリアル
   
   Project にドラッグしてPrefab化
   Addressable 設定: Address "BasicObstacle", Label "PlacementObjects"
   ```

3. **Enemy Prefab**
   ```
   3D Object → Capsule → 名前を "BasicEnemy" に変更
   Scale: (0.8, 1, 0.8)
   Add Component → BasicEnemy.cs
   Material: 紫色のマテリアル
   
   Project にドラッグしてPrefab化
   Addressable 設定: Address "BasicEnemy", Label "PlacementObjects"
   ```

## 6. 動作確認

### テストシーケンス

1. **基本動作確認**
   ```
   Play ボタンを押して:
   ✓ プレイヤーが中央レーンに配置される
   ✓ A/D キーでレーン移動できる
   ✓ スペースキーでジャンプできる
   ✓ ステージがスクロールする
   ```

2. **ステージ生成確認**
   ```
   Hierarchy で StageParent を確認:
   ✓ StageBlock が動的に生成される
   ✓ 古いブロックが削除される
   ```

3. **当たり判定確認**
   ```
   Scene ビューで HitDetector のギズモを確認:
   ✓ プレイヤーの現在セルが青色で表示される
   ✓ 配置物との当たり判定が動作する
   ```

## 7. トラブルシューティング

### よくある問題

1. **Input System が動作しない**
   ```
   解決方法:
   - Player Input の Behavior を "Send Messages" に設定
   - Input Actions Asset を Generate C# Class でコード生成
   - Project Settings で Input System Package を有効化
   ```

2. **参照が見つからない**
   ```
   解決方法:
   - FindObjectOfType で動的に取得される設計
   - シーン内に該当オブジェクトが存在するかチェック
   - Console でエラーメッセージを確認
   ```

3. **Addressable Assets が読み込めない**
   ```
   解決方法:
   - Window → Asset Management → Addressables → Groups
   - Build → New Build → Default Build Script
   - Label とAddress が正しく設定されているかチェック
   ```

4. **アニメーション関連**
   ```
   解決方法:
   - Animator Controller に適切なステートを作成
   - MotionController.ChangeAnimation の引数名がステート名と一致しているかチェック
   ```

## 8. 最適化のヒント

### パフォーマンス向上
- オブジェクトプールの事前ウォームアップ
- LOD システムの導入
- テクスチャアトラスの使用
- バッチング対応のマテリアル設計

### メモリ管理
- 不要になったAddressable Assetの解放
- プールサイズの適切な調整
- ガベージコレクション頻度の監視

このガイドに従ってセットアップすれば、完全に動作するランゲームが完成します！