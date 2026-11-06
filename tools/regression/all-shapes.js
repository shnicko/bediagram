// All shapes: opens the "shapes" test diagram (see make-shapes-diagram.py), saves the canvas SVG and runs every export
// on an open canvas and on A3.
// Usage: node all-shapes.js <app url> <app storage folder> <output folder>
const { chromium } = require('playwright');
const fs = require('fs');
const [url, dir, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1600, height: 1000 } });
  // Toolbar controls that moved into the menu bar (File → Open / Save / Export …): open the File menu first when the
  // control isn't on screen. Builds without a menu bar have them on the toolbar, so this does nothing there.
  const reveal = async (sel) => { if (await p.isVisible(sel)) return; if (await p.$('.menubar')) { await p.click('.menubar button.menu-head:text-is("File")'); await p.waitForTimeout(250); } };
  const errs = []; p.on('pageerror', e => errs.push(e.message)); p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  await p.goto(url); await p.waitForTimeout(2500);
  await reveal('button[title="Open a saved diagram"]'); await p.click('button[title="Open a saved diagram"]'); await p.waitForTimeout(400);
  await p.click('.files button.link:text-is("shapes")'); await p.waitForTimeout(800);
  await p.keyboard.press('f'); await p.waitForTimeout(400);
  const canvas = async (tag) => { fs.writeFileSync(`${out}/${tag}.canvas.svg`, await p.evaluate(() => document.querySelector('svg.surface').outerHTML)); await p.screenshot({ path: `${out}/${tag}.screen.png` }); };
  await canvas('fit');
  // select a few things so selection outlines / ports / edge handles render too
  await p.keyboard.press('Control+a'); await p.waitForTimeout(400); await canvas('all-selected');
  await p.keyboard.press('Escape'); await p.waitForTimeout(200);
  const exportAll = async (tag) => {
    await reveal('text=Export ▾'); if (await p.isVisible('text=Export ▾')) await p.click('text=Export ▾'); await p.waitForTimeout(300);
    const labels = await p.$$eval('.menu button', bs => bs.map(x => x.textContent));
    for (const l of labels) {
      if (!(await p.$('.menu'))) { await reveal('text=Export ▾'); if (await p.isVisible('text=Export ▾')) await p.click('text=Export ▾'); await p.waitForTimeout(300); }
      await p.click('.menu button:text-is("' + l + '")'); await p.waitForTimeout(1500);
    }
    for (const f of fs.readdirSync(dir)) if (f.startsWith('shapes.') && !f.endsWith('.diagram.json')) fs.copyFileSync(`${dir}/${f}`, `${out}/${tag}-${f}`);
  };
  await exportAll('canvas');
  await p.selectOption('.page-select', 'A3'); await p.waitForTimeout(600); await p.keyboard.press('f'); await p.waitForTimeout(400);
  await canvas('a3');
  await exportAll('a3');
  console.log('errors:', errs); await b.close();
})();
