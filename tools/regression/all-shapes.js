// All shapes: opens the "shapes" test diagram (see make-shapes-diagram.py), saves the canvas SVG and runs every export
// on an open canvas and on A3.
// Usage: node all-shapes.js <app url> <app storage folder> <output folder>
const { chromium } = require('playwright');
const fs = require('fs');
const [url, dir, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1600, height: 1000 } });
  const errs = []; p.on('pageerror', e => errs.push(e.message)); p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  await p.goto(url); await p.waitForTimeout(2500);
  await p.click('button[title="Open a saved diagram"]'); await p.waitForTimeout(400);
  await p.click('.files button.link:text-is("shapes")'); await p.waitForTimeout(800);
  await p.keyboard.press('f'); await p.waitForTimeout(400);
  const canvas = async (tag) => { fs.writeFileSync(`${out}/${tag}.canvas.svg`, await p.evaluate(() => document.querySelector('svg.surface').outerHTML)); await p.screenshot({ path: `${out}/${tag}.screen.png` }); };
  await canvas('fit');
  // select a few things so selection outlines / ports / edge handles render too
  await p.keyboard.press('Control+a'); await p.waitForTimeout(400); await canvas('all-selected');
  await p.keyboard.press('Escape'); await p.waitForTimeout(200);
  const exportAll = async (tag) => {
    await p.click('text=Export ▾'); await p.waitForTimeout(300);
    const labels = await p.$$eval('.menu button', bs => bs.map(x => x.textContent));
    for (const l of labels) {
      if (!(await p.$('.menu'))) { await p.click('text=Export ▾'); await p.waitForTimeout(300); }
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
