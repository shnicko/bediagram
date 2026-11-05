// Gestures + exports: adds shapes, then drags, multi-selects, connects, marquee-selects, groups, resizes, moves connector
// ends, pans and undoes, saving the diagram after each step; then runs every export on an open canvas and on A4.
// Usage: node gestures.js <app url> <app storage folder> <output folder>
const { chromium } = require('playwright');
const fs = require('fs');
const [url, dir, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1400, height: 850 } });
  const errs = [];
  p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  p.on('pageerror', e => errs.push(e.message));
  await p.goto(url); await p.waitForTimeout(2500);
  const W = 250;
  const centre = async (sel, i = 0) => p.evaluate(([s, i]) => { const r = document.querySelectorAll(s)[i].getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; }, [sel, i]);
  const drag = async (from, to, opts = {}) => {
    await p.mouse.move(from[0], from[1]); await p.mouse.down(opts);
    for (let k = 1; k <= 8; k++) await p.mouse.move(from[0] + (to[0] - from[0]) * k / 8, from[1] + (to[1] - from[1]) * k / 8);
    if (opts.shot) await p.screenshot({ path: opts.shot });
    if (opts.escape) { await p.keyboard.press('Escape'); await p.waitForTimeout(150); }
    await p.mouse.up(opts); await p.waitForTimeout(W);
  };
  let step = 0, saved = false;
  const snap = async (label) => {
    await p.keyboard.press('Control+s'); await p.waitForTimeout(400);
    if (!saved) { await p.fill('#saveas-name', 'gest'); await p.keyboard.press('Enter'); await p.waitForTimeout(500); saved = true; }
    fs.copyFileSync(`${dir}/gest.diagram.json`, `${out}/${String(++step).padStart(2, '0')}-${label}.json`);
  };
  await p.mouse.click(700, 300);
  for (const k of ['1', '2', '3']) { await p.keyboard.press(k); await p.waitForTimeout(W); }
  await snap('added');
  // 1 Move one shape (Move gesture)
  await drag(await centre('.node path', 1), (c => [c[0] + 160, c[1] + 33])(await centre('.node path', 1)));
  await snap('move');
  // 2 Click without drag (no undo step, no change)
  await drag(await centre('.node path', 0), await centre('.node path', 0));
  // 3 Shift multi-select + move
  await p.keyboard.down('Shift'); await p.mouse.click(...await centre('.node path', 2)); await p.keyboard.up('Shift');
  await drag(await centre('.node path', 2), (c => [c[0] - 90, c[1] + 47])(await centre('.node path', 2)));
  await snap('multimove');
  // 4 Connect: port of node 0 onto node 2
  await p.mouse.click(...await centre('.node path', 0)); await p.waitForTimeout(W);
  await drag(await centre('.node .port', 1), await centre('.node path', 2), { shot: `${out}/connect-preview.png` });
  await snap('connect');
  // 5 Connect to empty canvas -> new joined step
  await drag(await centre('.node .port', 2), (c => [c[0] + 20, c[1] + 160])(await centre('.node .port', 2)));
  await snap('connect-empty');
  // 6 Marquee select everything then nudge via move of one
  await drag([230, 80], [1080, 800], { shot: `${out}/marquee.png` });
  await snap('marquee');
  // 7 Group (Ctrl+G) then resize via handle
  await p.keyboard.press('Control+g'); await p.waitForTimeout(W);
  await drag(await centre('.resize', 0), (c => [c[0] + 77, c[1] + 64])(await centre('.resize', 0)));
  await snap('resize');
  // 8 Click group body (selects group) then drag group by title? click empty canvas first
  await p.mouse.click(1050, 120); await p.waitForTimeout(W);
  // 9 Select an edge and drag its end handle, then its bend
  const ec = await p.evaluate(() => { const pth = document.querySelectorAll('.edge-hit')[0]; const pt = pth.getPointAtLength(pth.getTotalLength() / 2); const m = pth.getScreenCTM(); return [pt.x * m.a + m.e, pt.y * m.d + m.f]; });
  await p.mouse.click(ec[0], ec[1]); await p.waitForTimeout(W);
  const h = await centre('.edge-handle', 1);
  await drag(h, [h[0] + 70, h[1] - 40], { shot: `${out}/edge-end.png` });
  await snap('edge-end');
  if (await p.$('.bend-handle')) { const bh = await centre('.bend-handle'); await drag(bh, [bh[0] + 43, bh[1] + 43]); }
  await snap('bend');
  // 10 Right-button pan, then move with Escape mid-drag
  await drag([1000, 700], [900, 650], { button: 'right' });
  await drag(await centre('.node path', 0), (c => [c[0] + 50, c[1]])(await centre('.node path', 0)), { escape: true });
  await snap('pan-escape');
  // 11 Undo x3, redo x1
  for (let i = 0; i < 3; i++) { await p.keyboard.press('Control+z'); await p.waitForTimeout(W); }
  await p.keyboard.press('Control+y'); await p.waitForTimeout(W);
  await snap('undo');
  await p.screenshot({ path: `${out}/final.png` });
  // Every export, open canvas then A4 page (with a text box and a comment added)
  await p.keyboard.press('t'); await p.waitForTimeout(W); await p.keyboard.press('Escape'); await p.keyboard.press('c'); await p.waitForTimeout(W); await p.keyboard.press('Escape');
  const exportAll = async (tag) => {
    await p.click('text=Export ▾'); await p.waitForTimeout(300);
    const labels = await p.$$eval('.menu button', bs => bs.map(b => b.textContent));
    for (const l of labels) {
      if (!(await p.$('.menu'))) { await p.click('text=Export ▾'); await p.waitForTimeout(300); }
      await p.click('.menu button:text-is("' + l + '")'); await p.waitForTimeout(1200);
    }
    for (const f of fs.readdirSync(dir)) if (f.startsWith('gest.') && !f.endsWith('.diagram.json')) fs.copyFileSync(`${dir}/${f}`, `${out}/${tag}-${f}`);
  };
  await exportAll('canvas');
  await p.selectOption('.page-select', 'A4'); await p.waitForTimeout(600);
  await exportAll('a4');
  console.log('errors:', errs);
  await b.close();
})();
