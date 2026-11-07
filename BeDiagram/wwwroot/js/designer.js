// Small browser helpers for the Blazor designer: keyboard shortcuts, wheel zoom/pan,
// canvas size, focusing inputs and downloading exports.
let dotnet = null;
let canvas = null;

function isTyping(e) {
    const t = e.target;
    return t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable);
}

function onKey(e) {
    if (!dotnet) return;
    const ctrl = e.ctrlKey || e.metaKey;
    const key = e.key;
    // Ctrl+S works everywhere, other shortcuts only when not typing in a field.
    if (ctrl && key.toLowerCase() === 's') {
        e.preventDefault();
        dotnet.invokeMethodAsync('OnKey', 's', true, e.shiftKey);
        return;
    }
    if (isTyping(e)) {
        if (key === 'Escape') e.target.blur();
        // Enter finishes an inline label edit (Shift+Enter = new line); text boxes keep Enter for new lines.
        const t = e.target;
        if (key === 'Enter' && !e.shiftKey && t.classList.contains('inline-edit') && !t.classList.contains('multiline')) e.preventDefault();
        // Connector labels are two lines at most: no new line once there are two.
        if (key === 'Enter' && t.classList.contains('two-line') && t.value.includes('\n')) e.preventDefault();
        return;
    }
    const handled = ['Delete', 'Backspace', 'Escape', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter'];
    const letters = ['j', 'b', 'c', 't', 'g', 'l', 'f', 'a', 'z', 'y', 'd', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '=', '-', '0'];
    if (handled.includes(key) || letters.includes(key.toLowerCase())) {
        if (ctrl || handled.includes(key)) e.preventDefault();
        dotnet.invokeMethodAsync('OnKey', key, ctrl, e.shiftKey);
    }
}

function onWheel(e) {
    if (!dotnet) return;
    e.preventDefault();
    const r = canvas.getBoundingClientRect();
    dotnet.invokeMethodAsync('OnWheel', e.deltaX, e.deltaY, e.ctrlKey || e.metaKey, e.shiftKey,
        e.clientX - r.left, e.clientY - r.top);
}

function onDragStart(e) {
    const item = e.target.closest && e.target.closest('[data-palette]');
    if (item) {
        e.dataTransfer.setData('text/plain', item.dataset.palette);
        e.dataTransfer.effectAllowed = 'copy';
    }
}

function sendRect() {
    if (!dotnet || !canvas) return;
    const r = canvas.getBoundingClientRect();
    dotnet.invokeMethodAsync('OnCanvasRect', r.left, r.top, r.width, r.height);
}

export function init(ref, el) {
    dotnet = ref;
    canvas = el;
    document.addEventListener('keydown', onKey);
    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('contextmenu', e => e.preventDefault());
    // Firefox only starts an HTML5 drag when data is set; the dragged type is tracked in .NET.
    document.addEventListener('dragstart', onDragStart);
    window.addEventListener('resize', sendRect);
    new ResizeObserver(sendRect).observe(canvas);
    sendRect();
}

export function dispose() {
    document.removeEventListener('keydown', onKey);
    document.removeEventListener('dragstart', onDragStart);
    window.removeEventListener('resize', sendRect);
    if (canvas) canvas.removeEventListener('wheel', onWheel);
    dotnet = null;
}

export function focus(id) {
    setTimeout(() => {
        const el = document.getElementById(id);
        if (el) { el.focus(); if (el.select) el.select(); }
    }, 30);
}

export function download(fileName, mime, text) {
    const blob = new Blob([text], { type: mime });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}
