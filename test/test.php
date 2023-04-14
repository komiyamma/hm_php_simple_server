<!DOCTYPE HTML>
<html lang="ja">
<head>
    <title>秀丸マクロ.net</title>
    <meta http-equiv="content-type" content="text/html; charset=utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
    <meta http-equiv="content-style-type" content="text/css">
    <link rel="apple-touch-icon" href="./apple-touch-icon.png">
    <link rel="stylesheet" type="text/css" href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/4.6.2/css/bootstrap.min.css">
    <script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.4/jquery.min.js"></script>
    <script type="text/javascript" src="https://ajax.googleapis.com/ajax/libs/jquery/3.6.2/jquery.min.js"></script>
</head>

<body class="sitebody">

<div class="jumbotron">
  <h1 class="display-4">Hello, world!</h1>
  <p class="lead">これはシンプルなヒーローユニットで、注目のコンテンツや情報に特別な注意を促すためのシンプルなジャンボトロンスタイルのコンポーネントです。</p>
  <hr class="my-4">
  <p>タイポグラフィとスペーシングにユーティリティクラスを使用し、大きなコンテナの中でコンテンツの間隔を空けることができます。</p>
  <a class="btn btn-primary btn-lg" href="?text=あいうえお" role="button">Learn more</a>
</div>

<?php
echo $aaa = filter_input(INPUT_GET, 'text');
?>

<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/4.6.2/js/bootstrap.bundle.min.js"></script>
</body>

</html>