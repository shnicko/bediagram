// Small browser helpers for the Blazor designer: keyboard shortcuts, wheel zoom/pan,
// canvas size, focusing inputs and downloading exports.
let dotnet = null;
let canvas = null;
// True while any open tab has unsaved changes: closing or reloading the browser tab then asks first.
let unsaved = false;

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
        dotnet.invokeMethodAsync('OnKey', 's', true, e.shiftKey, false);
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
    // Tab shortcuts use Alt, because the browser keeps Ctrl+Tab, Ctrl+W, Ctrl+T and Ctrl+N for itself.
    const tabKeys = ['PageUp', 'PageDown', 'w', 'n', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    if (e.altKey && !ctrl) {
        if (tabKeys.includes(key.length === 1 ? key.toLowerCase() : key)) {
            e.preventDefault();
            dotnet.invokeMethodAsync('OnKey', key, false, e.shiftKey, true);
        }
        return;
    }
    const handled = ['Delete', 'Backspace', 'Escape', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter'];
    const letters = ['j', 'b', 'c', 't', 'g', 'l', 'f', 'a', 'z', 'y', 'd', 'v', 'p', 'x', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '=', '-', '0'];
    if (handled.includes(key) || letters.includes(key.toLowerCase())) {
        if (ctrl || handled.includes(key)) e.preventDefault();
        dotnet.invokeMethodAsync('OnKey', key, ctrl, e.shiftKey, e.altKey);
    }
}

function onBeforeUnload(e) {
    if (!unsaved) return;
    e.preventDefault();
    e.returnValue = '';
}

// Called by the Designer after a render when "any tab has unsaved changes" has changed.
export function setUnsaved(value) {
    unsaved = value;
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

// Floating windows (the row editor): dragged by an element marked data-window-handle, moved here so a drag doesn't
// redraw the page on every move; where it was dropped goes back to .NET (OnWindowMoved) so a redraw keeps it there.
let windowDrag = null;

function onWindowDown(e) {
    if (e.button !== 0 || !e.target.closest) return;
    const handle = e.target.closest('[data-window-handle]');
    if (!handle || e.target.closest('button, input, select, textarea')) return;
    const win = handle.closest('.floating-window');
    if (!win) return;
    const r = win.getBoundingClientRect();
    windowDrag = { win: win, dx: e.clientX - r.left, dy: e.clientY - r.top };
    e.preventDefault();
}

function onWindowMove(e) {
    if (!windowDrag) return;
    const w = windowDrag.win;
    // Keep the title on screen.
    const left = Math.min(Math.max(0, e.clientX - windowDrag.dx), window.innerWidth - 80);
    const top = Math.min(Math.max(0, e.clientY - windowDrag.dy), window.innerHeight - 40);
    w.style.left = left + 'px';
    w.style.top = top + 'px';
}

function onWindowUp() {
    if (!windowDrag) return;
    const r = windowDrag.win.getBoundingClientRect();
    windowDrag = null;
    if (dotnet) dotnet.invokeMethodAsync('OnWindowMoved', r.left, r.top);
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
    window.addEventListener('beforeunload', onBeforeUnload);
    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('contextmenu', e => e.preventDefault());
    // Firefox only starts an HTML5 drag when data is set; the dragged type is tracked in .NET.
    document.addEventListener('dragstart', onDragStart);
    window.addEventListener('resize', sendRect);
    document.addEventListener('pointerdown', onWindowDown);
    document.addEventListener('pointermove', onWindowMove);
    document.addEventListener('pointerup', onWindowUp);
    new ResizeObserver(sendRect).observe(canvas);
    sendRect();
}

export function dispose() {
    document.removeEventListener('keydown', onKey);
    window.removeEventListener('beforeunload', onBeforeUnload);
    unsaved = false;
    document.removeEventListener('dragstart', onDragStart);
    window.removeEventListener('resize', sendRect);
    document.removeEventListener('pointerdown', onWindowDown);
    document.removeEventListener('pointermove', onWindowMove);
    document.removeEventListener('pointerup', onWindowUp);
    if (canvas) canvas.removeEventListener('wheel', onWheel);
    dotnet = null;
}

// Called after the render that adds the input (Designer.FocusAfterRender), so it's already in the page.
export function focus(id) {
    const el = document.getElementById(id);
    if (el) { el.focus(); if (el.select) el.select(); }
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
