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
        if (posY <= 3) { // 最初の行まであと3行程度なのであれば、最初にいる扱いにする。
            posY = 0;
        }
        if (allLineCount - posY < 3) {
            posY = allLineCount; // 最後の行まであと3行程度なのであれば、最後の行にいる扱いにする。
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

let preUpdateCount = 0;
let lastText = "";
function getAllLineCount() {
    let updateCount = hidemaru.updateCount();
    if (
    let text = hidemaru.getTotalText();
    if (previousTotalText != text) {
        previousTotalText = text;
    }
    text = text.replace(/\r/g, "");
    let lines = text.split("\n");
    if (lines) {
	    let index = lines.length-1;
	    while(index >= 1) {
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

stopIntervalTick();
updateMethod();
createIntervalTick(updateMethod);
