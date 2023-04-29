debuginfo(2);
if (timerHandle === 'undefined') {
    var timerHandle = 0; // 時間を跨いで共通利用するので、varで
}
function updateMethod() {
    if (hidemaru.isMacroExecuting()) {
        return;
    }
    let [isDiff, posY, allLineCount] = getChangeYPos();
    if (allLineCount < 0) {
        allLineCount = 1;
    }
    if (isDiff && posY > 0 && allLineCount > 0) {
        if (posY < 5) { // 最初の行まであと10行程度なのであれば、最初にいる扱いにする。
            posY = 0;
        }
        if (allLineCount - posY < 5) {
            posY = allLineCount; // 最後の行まであと10行程度なのであれば、最後の行にいる扱いにする。
        }
        let perY = posY / allLineCount;
        if (perY >= 1) {
            perY = 1.1; // これ丁度だと最後の行が微妙な感じになりやすい。
        }
        else if (perY < 0) {
            perY = 0;
        }
        // console.log("perY:"+perY);
        try {
            if (perY == 0) {
                browserpanecommand({
                    target:"_each",
                    url:"javascript:window.scrollTo(0, 0);"
                });
            }
            if (perY >= 1) {
                browserpanecommand({
                    target:"_each",
                    url:"javascript:window.scrollTo(0, (document.body.scrollHeight)*(2));"
                });
            }
        }
        catch (e) {
            console.log(e);
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
function stopIntervalTick() {
    if (timerHandle) {
        hidemaru.clearInterval(timerHandle);
    }
}
function createIntervalTick(func) {
    timerHandle = hidemaru.setInterval(func, 1000);
    return timerHandle;
}
function getAllLineCount() {
    let text = hidemaru.getTotalText();
    let cnt = text.match(/\n/g);
    if (cnt) {
        return cnt.length;
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

stopIntervalTick();
updateMethod();
createIntervalTick(updateMethod);
