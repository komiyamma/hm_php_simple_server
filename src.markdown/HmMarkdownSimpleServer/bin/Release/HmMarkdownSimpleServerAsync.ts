/// <reference path="types/hm_jsmode.d.ts" />
/*
 * HmMarkdownSimpleServer v1.1.0.1
 *
 * Copyright (c) 2023 Akitsugu Komiyama
 * under the MIT License
 */

// ブラウザペインのターゲット。個別枠。
let target_browser_pane: "_each" = "_each";

// 表示するべき一時ファイルのURL
let absolute_uri: string = getVar("$ABSOLUTE_URI") as string;

/*
// ポート番号
let port = getVar("#PORT") as number;
*/

if (typeof (timerHandle) === "undefined") {
    var timerHandle: number = 0; // 時間を跨いで共通利用するので、varで
}

// 基本、マクロを実行しなおす度にTickは一度クリア
function stopIntervalTick(timerHandle: number): void {
    if (timerHandle) {
        hidemaru.clearInterval(timerHandle);
    }
}

// Tick作成。
function createIntervalTick(func): number {
    return hidemaru.setInterval(func, 1000);
}

// Tick。
function tickMethod(): void {
    // (他の)マクロ実行中は安全のため横槍にならないように何もしない。
    if (hidemaru.isMacroExecuting()) {
        return;
    }
    // ファイルが更新されていたら、ブラウザをリロードする。
    // 実際には、ファイルが更新されると、先に「Markdown」⇒「html」化したTempファイルの内容が更新されるので、ブラウザにリロード命令を出しておくことで更新できる。
    if (isFileLastModifyUpdated()) {
        browserpanecommand({
            target: target_browser_pane,
            url: "javascript:location.reload();"
        });
    }

    /*
    // テキスト内容が変更になっている時だけ
    else if (isTotalTextChange()) {
        browserpanecommand(
            {
                target: "_each",
                url: `javascript:updateFetch(${port})`,
                show: 1
            }
        );
    }
    */

    // 何か変化が起きている？ linenoは変化した？ または、全体の行数が変化した？
    let [isDiff, posY, allLineCount] = getChangeYPos();
    // Zero Division Error回避
    if (allLineCount < 0) {
        allLineCount = 1;
    }

    // 何か変化が起きていて、かつ、linenoが1以上で、かつ、全体の行数が1以上であれば、
    if (isDiff && posY > 0 && allLineCount > 0) {

        // 最初の行まであと3行程度なのであれば、最初にいる扱いにする。
        if (posY <= 3) {
            posY = 0;
        }

        // 最後の行まであと3行程度なのであれば、最後の行にいる扱いにする。
        if (allLineCount - posY < 3) {
            posY = allLineCount;
        }

        // perYが0以上1以下になるように正規化する。
        let perY = posY / allLineCount;
        try {
            // perYが0以下なら、ブラウザは先頭へ
            if (perY <= 0) {
                browserpanecommand({
                    target: target_browser_pane,
                    url: "javascript:window.scrollTo(0, 0);"
                });
            }

            // perYが1以上なら、ブラウザは末尾へ
            if (perY >= 1) {
                browserpanecommand({
                    target: target_browser_pane,
                    url: "javascript:window.scrollTo(0, (document.body.scrollHeight)*(2));" // 微妙に末尾にならなかったりするので、2倍している。
                });
            }
        }
        catch (e) {
            // エラーならアウトプット枠に
            let outdll = hidemaru.loadDll("HmOutputPane.dll");
            outdll.dllFuncW.OutputW(hidemaru.getCurrentWindowHandle(), `${e}\r\n`);
        }
    }
}

/*
let lastUpdateCount: number = 0;
let preTotalText: string = "";
function isTotalTextChange() {
    // updateCountで判定することで、テキスト内容の更新頻度を下げる。
    // getTotalTextを分割したりコネコネするのは、行数が多くなってくるとやや負荷になりやすいので
    // テキスト更新してないなら、前回の結果を返す。
    let updateCount = hidemaru.getUpdateCount();
    // 前回から何も変化していないなら、前回の結果を返す。
    if (lastUpdateCount == updateCount) {
        return false;
    }
    lastUpdateCount = updateCount;

    let totalText: string | undefined = hidemaru.getTotalText();
    if (preTotalText == totalText) {
        return false;
    }
    preTotalText = totalText;

    return true;
}
*/


// linenoが変化したか、全体の行数が変化したかを判定する。
let lastPosY: number = 0;
let lastAllLineCount: number = 0;
function getChangeYPos(): [boolean, number, number] {
    let isDiff = false;

    // linenoが変わってるなら、isDiffをtrueにする。
    let posY = getCurCursorYPos();
    if (lastPosY != posY) {
        lastPosY = posY;
        isDiff = true;
    }

    // 行全体が変わってるなら、isDiffをtrueにする。
    let allLineCount = getAllLineCount();
    if (lastAllLineCount != allLineCount) {
        lastAllLineCount = allLineCount;
        isDiff = true;
    }
    return [isDiff, posY, allLineCount];
}

// テキスト全体の行数を取得する。
// 実際には末尾の空行を除いた行数を取得する。
let preUpdateCount: number = 0;
let lastIndex: number = 0;
function getAllLineCount(): number {

    // updateCountで判定することで、テキスト内容の更新頻度を下げる。
    // getTotalTextを分割したりコネコネするのは、行数が多くなってくるとやや負荷になりやすいので
    // テキスト更新してないなら、前回の結果を返す。
    let updateCount: number = hidemaru.getUpdateCount();

    // 前回から何も変化していないなら、前回の結果を返す。
    if (updateCount == preUpdateCount) {
        return lastIndex + 1; // lineno相当に直す
    }
    else {
        preUpdateCount = updateCount;

        // テキスト全体から
        let lastText : string | undefined = hidemaru.getTotalText();

        // 失敗することがあるらしい...
        if (lastText == undefined) {
            return 1;
        }

        // \r は判定を歪めやすいので先に除去
        lastText = lastText.replace(/\r/g, "");

        // 改行で分割
        let lines = lastText.split("\n");
        let index = lines.length - 1; // 最後の行の中身から探索する
        while (index >= 1) {
            // 空ではない行を見つけたら、有効な行である。
            if (lines[index] != "") {
                break;
            }
            index--;
        }

        // 前回の有効な行として格納
        lastIndex = index;
        return index + 1; // lineno相当に直す
    }
}

// lineno相当
function getCurCursorYPos(): number {
    let pos = hidemaru.getCursorPos("wcs");
    return pos[0];
}

// ファイルが更新されたかどうかを判定する。
let lastFileModified: number = 0;
let fso: any = null;
function isFileLastModifyUpdated(): boolean {
    if (fso == null) {
        fso = hidemaru.createObject("Scripting.FileSystemObject");
    }
    let diff: boolean = false;

    // 無題になってたらこれやらない。
    let filepath = hidemaru.getFileFullPath();
    if (filepath != "") {
        try {
            // 編集しているファイルではなく、Tempファイルの方のファイルが更新されてるかが重要。
            let f = fso.GetFile(absolute_uri);
            let m = f.DateLastModified;
            if (m != lastFileModified) {
                diff = true;
                lastFileModified = m;
            }
        } catch (e) {
            // エラーならアウトプット枠に
            let outdll = hidemaru.loadDll("HmOutputPane.dll");
            outdll.dllFuncW.OutputW(hidemaru.getCurrentWindowHandle(), `${e}\r\n`);
        }
    }
    return diff;
}




// 初期化
function initVariable(): void {
/*
    lastUpdateCount = 0;
    preTotalText = "";
*/
    lastPosY = 0;
    lastAllLineCount = 0;

    preUpdateCount = 0;
    lastIndex = 0;

    lastFileModified = 0;
    fso = null;
}


// 表示
browserpanecommand({
    target: target_browser_pane,
    url: absolute_uri,
    show: 1
});

// 初期化
initVariable();

// 前回のが残っているかもしれないので、止める
stopIntervalTick(timerHandle);

 // １回走らせる
tickMethod();

// Tick実行
timerHandle = createIntervalTick(tickMethod);

