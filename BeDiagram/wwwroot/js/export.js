// PNG export: draws an exported SVG onto a canvas at the requested pixel size and returns the PNG bytes.
// Called from BrowserPngRasterizer (Services/Export/IPngRasterizer.cs); the bytes are streamed back to .NET.
export async function svgToPng(svg, width, height) {
    const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
    try {
        const img = new Image();
        await new Promise((resolve, reject) => {
            img.onload = resolve;
            img.onerror = () => reject(new Error('the browser could not draw the SVG'));
            img.src = url;
        });
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        if (!ctx) throw new Error('the browser could not create a canvas this size');
        ctx.drawImage(img, 0, 0, width, height);
        const blob = await new Promise((resolve, reject) =>
            canvas.toBlob(b => b ? resolve(b) : reject(new Error('the image is too large for the browser')), 'image/png'));
        return new Uint8Array(await blob.arrayBuffer());
    } finally {
        URL.revokeObjectURL(url);
    }
}

// Copy to clipboard: puts an exported file (a PNG) on the clipboard, for pasting into Word, PowerPoint, Teams…
// Called from BrowserClipboard (Services/Export/BrowserClipboard.cs) with the bytes as a stream.
// Browsers only allow this on https or localhost, and while the page has focus.
export async function copyToClipboard(streamRef, type) {
    if (!window.isSecureContext || !navigator.clipboard || typeof ClipboardItem === 'undefined') {
        throw new Error('this browser only allows copying pictures when BeDiagram is opened on localhost or https');
    }
    const blob = new Blob([await streamRef.arrayBuffer()], { type });
    await navigator.clipboard.write([new ClipboardItem({ [type]: blob })]);
}
