# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

Unity 6（6000.3.10f1）で作られた2Dサイドスクロールアクションゲーム。アートはSunnyLand Artworkアセットを使用。レンダラーはURP、入力はNew Input System（`com.unity.inputsystem`）で処理している。

## 起動・ビルド

Unity Hubからエディタバージョン **6000.3.10f1** を選んでプロジェクトを開く。ビルドはエディタ内の **File → Build Settings** から行う。ビルドスクリプトやCI設定はない。

## アーキテクチャ

### シーン遷移

シーンは2つ：`StartScene` → `MainScene`。ステージの進行状況は `PlayerPrefs` で保存される。
- `TryStage` — これからプレイするステージのインデックス（0始まり）
- `ClearStage` — クリア済みの最大ステージインデックス。これを基にステージ選択のロックを制御する

`StartManager` が `StageButtons` プレハブを動的に生成し、`StageButtonsCtrl` がアンロック条件（`stageNumber <= ClearStage + 1`）を判定する。

### ステージ読み込み

`MainManager`（`MainScene` に存在）が `PlayerPrefs` から `TryStage` を読み取り、`stages[]` 配列の対応するプレハブ（Stage1〜Stage6）をInstantiateする。タイルマップ・スポーナー・敵はすべてそのプレハブの中に含まれている。

### プレイヤー（`PlayerCrtl`）

- 移動：`rb.AddForceX` で `rb.linearVelocityX` に対してスムージングをかけて加速
- 地面・坂道判定：コライダー半径の半分だけ前後にオフセットした2本の下向きレイキャストで坂道を検出。片方だけヒットしている場合は坂道とみなし、静止中はY位置をフリーズしてずり落ちを防止
- 踏みつけ：`OnCollisionEnter2D` で `enemy.y < player.y - colliderRadius` なら踏みつけ（敵を破壊してバウンス）、それ以外は `GameOver()`
- ゴール：`Gem` レイヤーへの `OnTriggerEnter2D` で `MainManager.GameClear()` を呼ぶ
- 落下死：`y < -11` で `GameOver()`

### 敵の共通パターン

3種類の敵はすべて同じ **Spawner** 構造を持つ。`Start()` で `GameObject.Find("Player")` によりプレイヤーを取得し、`Update()` で30ユニット以内に近づいたらプレハブをSpawn、一定距離（種類によって30〜50ユニット）離れたらDespawnする。

| 敵 | 動作 |
|----|------|
| `BearCtrl` | 地上パトロール。崖・壁を2本のレイキャストで検出し、端や壁に当たると方向転換 |
| `FrogCtrl` | プレイヤーが `moveDistance` 以内に入るまで待機し、タイマーでプレイヤーのX座標に向かってジャンプ |
| `EagleCtrl` | 重力無効。サイン波オフセットでプレイヤーの上空をホバリングし、`attackInterval` 秒ごとに楕円弧の軌道でダイブ攻撃 |

### WaveManager（ステージ5）

`WaveManager_Stage5.cs`（クラス名 `WaveManager`）がコルーチンベースのタイムド波管理を行う。`SpawnData` ごとに `spawnTime`・`duration`・出現位置を指定する。全敵の出現処理が完了してから22秒後に `gemPrefab` が出現し、ステージクリアが可能になる。

### Physics API の注意

このプロジェクトはUnity 6のRigidbody2D APIを使用している：`rb.linearVelocity`、`rb.linearVelocityX`、`rb.linearVelocityY`。`rb.velocity` はUnity 6で非推奨となりコンパイルエラーになるため**使用しないこと**。

### レイヤー

- `Ground` — 地面・壁のレイキャスト判定に使用
- `Enemy` — プレイヤーの衝突ロジックをトリガー
- `Gem` — `GameClear()` をトリガー

### 新しいステージを追加する手順

1. 既存のステージプレハブを複製してリネームする（例：`Stage7`）
2. プレハブ内に敵スポーナー（`BearSpawner` 等）のGameObjectを配置し、対応するプレハブ参照をセット
3. 新しいプレハブを `MainManager.stages[]` にインスペクターから追加する
4. `StartScene` の `StartManager` の `stageCount` をインクリメントする
