// Bend handle: drags the middle segment of an elbow connector, undoes and redoes, and saves the diagram.
// Usage: node bend.js <app url> <app storage folder> <output folder>
const { chromium } = require('playwright');
const fs = require('fs');
const [url, dir, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1400, height: 850 } });
  await p.goto(url); await p.waitForTimeout(2500);
  const centre = async (sel, i = 0) => p.evaluate(([s, i]) => { const r = document.querySelectorAll(s)[i].getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; }, [sel, i]);
  const drag = async (f, t) => { await p.mouse.move(...f); await p.mouse.down(); for (let k = 1; k <= 8; k++) await p.mouse.move(f[0] + (t[0] - f[0]) * k / 8, f[1] + (t[1] - f[1]) * k / 8); await p.mouse.up(); await p.waitForTimeout(250); };
  await p.mouse.click(700, 300); await p.keyboard.press('1'); await p.waitForTimeout(250); await p.keyboard.press('2'); await p.waitForTimeout(250);
  const c = await centre('.node > path:not(.node-shadow)', 1); await drag(c, [c[0] + 230, c[1] + 90]);
  const ec = await p.evaluate(() => { const pth = document.querySelectorAll('.edge-hit')[0]; const pt = pth.getPointAtLength(pth.getTotalLength() / 2); const m = pth.getScreenCTM(); return [pt.x * m.a + m.e, pt.y * m.d + m.f]; });
  await p.mouse.click(...ec); await p.waitForTimeout(300);
  console.log('bend handle:', !!(await p.$('.bend-handle')));
  const bh = await centre('.bend-handle'); await drag(bh, [bh[0] + 40, bh[1] + 40]);
  await p.keyboard.press('Control+z'); await p.waitForTimeout(250); await p.keyboard.press('Control+y'); await p.waitForTimeout(250);
  await p.keyboard.press('Control+s'); await p.waitForTimeout(400); await p.fill('#saveas-name', 'bend'); await p.keyboard.press('Enter'); await p.waitForTimeout(500);
  fs.copyFileSync(`${dir}/bend.diagram.json`, `${out}/bend.json`);
  await b.close();
})();
