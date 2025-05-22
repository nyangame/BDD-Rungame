```
## 製品概要
product_infomation:
  title: Rungame
  platform: [pc]
  genre: [action, rungame]
  reference: ["temple run"]

## AI アシスト
ai_assist:
  format_style: markdown + toml + gherkin
  primary_targets:
    - ./spec/rule/general.md
    - ./spec/rule/coding.md
    - ./spec/ubi/ubiquitous.yaml
```

# BDD-Rungame

ビヘイビア駆動開発で、Unityのスクリプトを開発してみるテストです  

# フォルダ構成

### spec
仕様書が入ります。  

### unity
Unityプロジェクトです。ここにあるのは、すべてClaudeが書いたコードです。

### Data
AIに作らせたデータが入ります。
