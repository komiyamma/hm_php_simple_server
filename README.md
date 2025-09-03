# HmPHPSimpleServer

![HmPHPSimpleServer latest release](https://img.shields.io/github/v/release/komiyamma/hm_php_simple_server)
[![MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](LICENSE)
![Hidemaru 9.22](https://img.shields.io/badge/Hidemaru-v9.22-6479ff.svg)
![.NET 4.72](https://img.shields.io/badge/.NET-4.72-6479ff.svg)
![PHP 7.x](https://img.shields.io/badge/PHP-7.x-6479ff.svg)
![PHP 8.x](https://img.shields.io/badge/PHP-8.x-6479ff.svg)

秀丸エディタのブラウザ枠機能を利用して、PHPやHTMLファイルのプレビューをリアルタイムで行うための簡易HTTPサーバーです。

[作者による紹介ページ](https://秀丸マクロ.net/?page=nobu_tool_hm_php_simple_server)

## 主な機能

- **簡易Webサーバー**: PHPのビルトインサーバー機能を利用して、ローカルWebサーバーを起動します。
- **ライブリロード**: ファイルを保存すると、表示中のブラウザ枠が自動的にリロードされます。
- **フォルダ監視**: CSS/JavaScript/画像ファイルなど、指定した拡張子のファイルが変更された場合も、自動でリロードします。
- **動的ドキュメントルート**: 秀丸エディタで開いているファイルの場所を基準に、ドキュメントルートが自動で設定されます。
- **ポート自動選択**: 起動時に利用可能なポートを自動で検索するため、ポートの競合を心配する必要がありません。
- **簡単な導入**: 秀丸エディタのマクロフォルダにファイルを配置するだけで、すぐに利用を開始できます。

## 動作環境

- **秀丸エディタ**: v9.22 以降
- **.NET Framework**: 4.7.2 以降
- **PHP**: 7.x / 8.x (別途インストールが必要です)

## インストール方法

1. **PHPの準備**:
   - PCにPHPがインストールされていない場合は、[PHP for Windows](https://windows.php.net/download) から「**Zip**」版をダウンロードし、任意のフォルダに展開してください。
   - 例: `C:\usr\php`

2. **マクロのダウンロード**:
   - [リリースページ](https://github.com/komiyamma/hm_php_simple_server/releases)から最新版の`HmPHPSimpleServer.zip`をダウンロードします。

3. **ファイルの配置**:
   - ダウンロードしたzipファイルを展開します。
   - 中に含まれている以下の2つのファイルを、秀丸エディタのマクロ用フォルダ（`hidemaru.exe`がある場所の`mac`フォルダなど）にコピーします。
     - `HmPHPSimpleServer.mac`
     - `HmPHPSimpleServer.dll`

## 設定方法

設定は、マクロファイル `HmPHPSimpleServer.mac` を直接編集して行います。

```hidescript
// =================================================
// ● PHP実行ファイルへのフルパス
// =================================================
// PCにインストールしたphp.exeのフルパスを指定してください。
$PHP_FULLPATH = @"C:\usr\php\php.exe";

// =================================================
// ● ファイル監視による自動リロードの有効化
// =================================================
// 1: 有効, 0: 無効
#NOTIFY_FILE_WATCHER = 1;

// =================================================
// ● 自動リロードの対象となるファイルの拡張子（正規表現）
// =================================================
// この正規表現にマッチするファイルが変更されると、ブラウザがリロードされます。
$NOTIFY_FOLDER_CHANGE_FILTER = "\.php|\.html|\.htm|\.css|\.js|\.png|\.jpg|\.jpeg";

// =================================================
// ● PHPサーバーのドキュメントルート（上級者向け）
// =================================================
// 通常は空白のままで問題ありません。
// 空白の場合、現在開いているファイルのフォルダがドキュメントルートになります。
$PHP_DOCUMENTROOT = getarg(0);
```

特に、`$PHP_FULLPATH`はご自身の環境に合わせて必ず設定してください。

## 使い方

1. プレビューしたいPHPまたはHTMLファイルを秀丸エディタで開きます。
2. `[マクロ]`メニューから`HmPHPSimpleServer.mac`を実行します。
3. 秀丸エディタのブラウザ枠が開き、プレビューが表示されます。
4. ファイルを編集して保存すると、プレビューが自動的に更新されます。

## ライセンス

このプロジェクトは [MITライセンス](LICENSE.txt) の下で公開されています。