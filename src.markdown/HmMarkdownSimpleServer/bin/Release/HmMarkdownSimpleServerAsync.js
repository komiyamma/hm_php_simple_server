/// <reference path="hm_jsmode.d.ts" />
var target_browser_pane = getVar("$TARGET_RENDER_PANE");
var relative_uri = getVar("$RELATIVE_URI");
if (typeof (timerHandle) === "undefined") {
    var timerHandle = 0; // 時間を跨いで共通利用するので、varで
}
function tickMethod() {
    if (hidemaru.isMacroExecuting()) {
        return;
    }
    if (isFileLastModifyUpdated()) {
        console.log("ロケーションリロード");
        renderpanecommand({
            target: target_browser_pane,
            url: "javascript:location.reload();"
        });
        return;
    }
    let [isDiff, posY, allLineCount] = getChangeYPos();
    // Zero Division Error回避
    if (allLineCount < 0) {
        allLineCount = 1;
    }
    if (isDiff && posY > 0 && allLineCount > 0) {
        // 最初の行まであと3行程度なのであれば、最初にいる扱いにする。
        if (posY <= 3) {
            posY = 0;
        }
        // 最後の行まであと3行程度なのであれば、最後の行にいる扱いにする。
        if (allLineCount - posY < 3) {
            posY = allLineCount;
        }
        let perY = posY / allLineCount;
        if (perY >= 1) {
            perY = 1; // これ丁度だと最後の行が微妙な感じになりやすい。
        }
        else if (perY < 0) {
            perY = 0;
        }
        try {
            if (perY == 0) {
                renderpanecommand({
                    target: target_browser_pane,
                    url: "javascript:window.scrollTo(0, 0);"
                });
            }
            if (perY >= 1) {
                renderpanecommand({
                    target: target_browser_pane,
                    url: "javascript:window.scrollTo(0, (document.body.scrollHeight)*(2));"
                });
            }
        }
        catch (e) {
            let outdll = hidemaru.loadDll("HmOutputPane.dll");
            outdll.dllFuncW.OutputW(hidemaru.getCurrentWindowHandle(), `${e}\r\n`);
        }
    }
}
let lastPosY = 0;
let lastAllLineCount = 0;
function getChangeYPos() {
    let isDiff = false;
    let posY = getCurCursorYPos();
    let allLineCount = getAllLineCount();
    if (lastPosY != posY) {
        lastPosY = posY;
        isDiff = true;
    }
    if (lastAllLineCount != allLineCount) {
        lastAllLineCount = allLineCount;
        isDiff = true;
    }
    return [isDiff, posY, allLineCount];
}
function stopIntervalTick(timerHandle) {
    if (timerHandle) {
        hidemaru.clearInterval(timerHandle);
    }
}
function createIntervalTick(func) {
    return hidemaru.setInterval(func, 1000);
}
let preUpdateCount = 0;
let lastText = "";
function getAllLineCount() {
    let updateCount = hidemaru.getUpdateCount();
    if (updateCount != preUpdateCount) {
        preUpdateCount = updateCount;
        lastText = hidemaru.getTotalText();
    }
    lastText = lastText.replace(/\r/g, "");
    let lines = lastText.split("\n");
    if (lines) {
        let index = lines.length - 1;
        while (index >= 1) {
            if (lines[index] != "") {
                break;
            }
            index--;
        }
        return index + 1; // lineno相当に直す
    }
    else {
        return 1;
    }
}
function getCurCursorYPos() {
    let pos = hidemaru.getCursorPos("wcs");
    return pos[0];
}
function getCurCursorYPosFromMousePos() {
    let pos = hidemaru.getCursorPosFromMousePos("wcs");
    return pos[0];
}
let lastFileModified = 0;
let fso = hidemaru.createObject("Scripting.FileSystemObject");
function isFileLastModifyUpdated() {
    let diff = false;
    let filepath = hidemaru.getFileFullPath();
    if (filepath != "") {
        let f = fso.GetFile(filepath);
        let m = f.DateLastModified;
        if (m != lastFileModified) {
            diff = true;
            lastFileModified = m;
        }
    }
    return diff;
}
let paneArg = {
    target: target_browser_pane,
    url: relative_uri,
    show: 1
};
renderpanecommand(paneArg);
stopIntervalTick(timerHandle);
tickMethod();
timerHandle = createIntervalTick(tickMethod);
