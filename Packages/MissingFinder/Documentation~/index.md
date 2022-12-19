# Missing Finder

## 概要

アセット中の参照切れ(Missing)を検出します

## インストール

### Package Manager からのインストール

* Package Manager → Scoped Registries に以下を登録
    * URL: https://package.openupm.com
    * Scope: jp.co.bexide
* Package Manager → My Registries から以下を選択して Install
    * BxUni Missing Finder

## 使い方

### 起動
 
UnityEditorメニュー → BeXide → Missing Reference Finder
 

![](images/mf01.png)

### 設定項目

#### 対象フォルダ

チェクしたいフォルダを指定します。
右端のボタンを押してリストから選択するか、またはプロジェクトウィンドウからフォルダをドラッグ・アンド・ドロップします。

#### 対象アセットタイプ

チェックしたいアセットのタイプを選びます。
デフォルトでは全てのタイプが対象となっていますが、対象を絞りたい場合はここから指定することができます。

### チェック実行

「チェック」ボタンを押すと実行されます。実行中に中断したい場合は「キャンセル」ボタンを押します。

## 結果の見方

検査が終わると検査結果が表示されます。

![](images/mf02.png)

| 欄        | 内容                    |
|----------|-----------------------|
| Asset    | 問題のあるアセットへの参照         |
| SubAsset | アセット中の、問題のあるオブジェクトの名前 |
| Property | 問題のあるプロパティ            |

## 修正機能

検査結果の下にある「参照切れを削除」ボタンを押すと、検出された参照切れを全てクリアします。
この操作により元より設定されていた参照は永久に失われ、アンドゥできませんのでご注意ください。

## お問い合わせ

* 不具合のご報告は GitHub の Issues へ
* その他お問い合わせは mailto:tech-info@bexide.co.jp へ

