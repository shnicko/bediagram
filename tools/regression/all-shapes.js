// All shapes: opens the "shapes" test diagram (see make-shapes-diagram.py), saves the canvas SVG and runs every export
// on an open canvas and on A3.
// Usage: node all-shapes.js <app url> <app storage folder> <output folder>
const { chromium } = require('playwright');
const blazor = require('./blazor');
const fs = require('fs');
const [url, dir, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1600, height: 1000 } });
  const z = blazor.watch(p);   // waits follow the server's answers (see blazor.js)
  // Toolbar controls that moved into the menu bar (File → Open / Save / Export …): open the File menu first when the
  // control isn't on screen. Builds without a menu bar have them on the toolbar, so this does nothing there.
  const reveal = async (sel) => { if (await p.isVisible(sel)) return; if (await p.$('.menubar')) { await z.act(() => p.click('.menubar button.menu-head:text-is("File")')); } };
  // The export entries: the toolbar's Export ▾ menu, or File in the menu bar. Opens it unless a menu is already open.
  const openExport = async () => {
    if (await p.$('.menu')) return;
    if (await p.isVisible('text=Export ▾')) await z.act(() => p.click('text=Export ▾'));
    else await z.act(() => p.click('.menubar button.menu-head:text-is("File")'));
  };
  const isExport = t => /SVG|PNG|PDF|Mermaid/.test(t);
  const errs = []; p.on('pageerror', e => errs.push(e.message)); p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  await p.goto(url); await z.ready();
  await reveal('button[title="Open a saved diagram"]'); await z.act(() => p.click('button[title="Open a saved diagram"]'));
  await z.act(() => p.click('.files button.link:text-is("shapes")'));
  await z.act(() => p.keyboard.press('f'));
  const canvas = async (tag) => { fs.writeFileSync(`${out}/${tag}.canvas.svg`, await p.evaluate(() => document.querySelector('svg.surface').outerHTML)); await p.screenshot({ path: `${out}/${tag}.screen.png` }); };
  await canvas('fit');
  // select a few things so selection outlines / ports / edge handles render too
  await z.act(() => p.keyboard.press('Control+a')); await canvas('all-selected');
  await z.act(() => p.keyboard.press('Escape'));
  const exportAll = async (tag) => {
    await openExport();
    const labels = (await p.$$eval('.menu button', bs => bs.map(x => x.textContent))).filter(isExport);
    for (const l of labels) {
      if (!(await p.$('.menu'))) { await openExport(); }
      await z.act(() => p.click('.menu button:text-is("' + l + '")'));
    }
    for (const f of fs.readdirSync(dir)) if (f.startsWith('shapes.') && !f.endsWith('.diagram.json')) fs.copyFileSync(`${dir}/${f}`, `${out}/${tag}-${f}`);
  };
  await exportAll('canvas');
  await reveal('.page-select'); await z.act(() => p.selectOption('.page-select', 'A3')); if (await p.$('.menu-backdrop')) { await z.act(() => p.click('.menu-backdrop', { position: { x: 5, y: 5 } })); } await z.act(() => p.keyboard.press('f'));
  await canvas('a3');
  await exportAll('a3');
  console.log(z.report() + '; errors:', errs); await b.close();
})();
