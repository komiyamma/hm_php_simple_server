# hm_php_simple_daemon
秀丸エディタ - ブラウザ枠用 - 簡易デーモン

https://www.youtube.com/embed/kzg6j6lnc_Q

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

秀丸の１プロセスあたり、１つのサーバーが起動する。
(よってそれぞれの秀丸の個別ブラウザに対して別々のサーバーが機能する)

## 再度 HmPHPSimpleDaemon.mac のマクロを実行すると？
該当プロセスで起動中のPHPサーバーが一旦終了し、再度起動する。
この際、それまでとはPort番号が異なる。
(全プロセスでマクロが実行される度にPort番号が１つずつ上がる。60000を超えると50001に戻る)

## 該当プロセスを閉じると？
該当プロセスで起動していたPHPサーバーは終了する。


