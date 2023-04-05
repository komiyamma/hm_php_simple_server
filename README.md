# hm_php_simple_daemon
秀丸エディタ - ブラウザ枠用 - 簡易デーモン

## PHP配備 (すでにPCにPHP配置してある人はスキップ)
https://windows.php.net/download#php-8.2
のzip を適当に好きなのダウンロード。
zip ファイルを展開し適当な位置に配備

## HmPHPSimpleDaemon.mac のマクロ編集

```
$PHP_FULLPATH
```
の変数内容をダウンロードしたphpの「php.exe」までのフルパスを設定。

## HmPHPSimpleDaemon.mac のマクロの実行
適当に「.html」や「.php」などまぁブラウザで開けそうな適当な「ファイルを秀丸で開き」、  
「HmPHPSimpleDaemon.mac」を実行すると、「該当ファイルのフォルダを起点としてPHPサーバーが起動する」  
このため、個別ブラウザでhttpとして表示できる。  

