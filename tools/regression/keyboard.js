// Keyboard + buttons: presses every shortcut (and clicks the toolbar / panel buttons that run commands), recording
// the editor state after each step.
// Usage: node keyboard.js <app url> <output .json>
const { chromium } = require('playwright');
const blazor = require('./blazor');
const fs = require('fs');
const [url, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1400, height: 850 } });
  const z = blazor.watch(p);   // waits follow the server's answers, not fixed pauses (see blazor.js)
  // Toolbar controls that moved into the menu bar (File → Open / Save / Export …): open the File menu first when the
  // control isn't on screen. Builds without a menu bar have them on the toolbar, so this does nothing there.
  const reveal = async (sel) => { if (await p.isVisible(sel)) return; if (await p.$('.menubar')) { await z.act(() => p.click('.menubar button.menu-head:text-is("File")')); } };
  const errs = [];
  p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  p.on('pageerror', e => errs.push(e.message));
  await p.goto(url); await z.ready();
  const log = [];
  const state = async (label) => {
    const s = await p.evaluate(() => {
      const ids = {}; let n = 0;
      const norm = t => t.replace(/\b[netg]_[0-9a-f]{8}\b/g, m => ids[m] || (ids[m] = '#' + (n++)));
      const svg = document.querySelector('svg.surface, svg[class*=surface]');
      const tb = [...document.querySelectorAll('.toolbar button')].map(x => `${x.textContent.trim()}|${x.title}|${x.disabled}`);
      return {
        svg: norm(svg ? svg.outerHTML : ''),
        zoom: document.querySelector('.zoom')?.textContent,
        selected: document.querySelectorAll('.selected').length,
        editing: !!document.querySelector('.inline-edit, #edit-box'),
        modal: document.querySelector('.modal h3, .modal h2')?.textContent || (document.querySelector('.modal') ? 'modal' : null),
        toast: document.querySelector('.toast')?.textContent || null,
        panel: norm(document.querySelector('aside.panel, .panel')?.innerText || ''),
        toolbar: tb,
      };
    });
    log.push({ label, ...s });
  };
  const key = async (k, label) => { await z.act(() => p.keyboard.press(k)); await state(label || k); };
  // A new shape opens its label for typing: wait until the label box has focus and record that, then Escape closes it
  // so the next key isn't typed into the label. (An Escape before the box has focus runs Cancel and clears the selection.)
  const add = async (k) => {
    let m = z.mark();
    await p.keyboard.press(k);
    await p.waitForFunction(() => document.activeElement?.id === 'edit-box', null, { timeout: 3000 }).catch(() => z.warn('a new label box never got focus'));
    await z.settled(m); await state(k);
    m = z.mark();
    await p.keyboard.press('Escape');
    await p.waitForSelector('#edit-box', { state: 'detached', timeout: 3000 }).catch(() => {});
    await z.settled(m);
  };
  const click = async (sel, label) => { await z.act(() => p.click(sel)); await state(label || sel); };
  const node = async i => p.evaluate(i => { const r = document.querySelectorAll('.node > path:not(.node-shadow)')[i].getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; }, i);

  await z.act(() => p.mouse.click(700, 300)); await state('start');
  for (const k of ['1', '2', '3', '4']) await add(k);
  await key('Control+a'); await key('j'); await key('Escape');
  await z.act(async () => p.mouse.click(...await node(1))); await state('click node 1');
  for (const k of ['ArrowUp', 'ArrowRight', 'Shift+ArrowDown', 'Shift+ArrowLeft', 'ArrowLeft', 'ArrowDown']) await key(k);
  await key('Control+ArrowUp'); await key('Control+j');
  await key('Control+d'); await key('Delete');
  await key('Control+z'); await key('Control+Shift+z'); await key('Control+y'); await key('Control+z'); await key('Control+z');
  await key('Control+Shift+a'); await key('Control+g'); await key('Escape'); await key('Escape');
  await z.act(async () => p.mouse.click(...await node(0)));
  await key('Enter'); await key('Escape'); await key('Escape');
  await key('Control+a'); await key('Control+g'); await key('Escape');
  await key('Control+Shift+g');
  await z.act(async () => p.mouse.click(...await node(0)));
  await key('Control+Shift+g', 'ungroup with a shape selected');
  await key('t'); await key('Escape'); await key('c'); await key('Escape'); await key('g'); await key('Escape'); await key('Escape');
  // select a connector, toggle style
  const ec = await p.evaluate(() => { const pth = document.querySelectorAll('.edge-hit')[0]; const pt = pth.getPointAtLength(pth.getTotalLength() / 2); const m = pth.getScreenCTM(); return [pt.x * m.a + m.e, pt.y * m.d + m.f]; });
  await z.act(() => p.mouse.click(...ec)); await state('edge selected');
  await key('l'); await key('Shift+L'); await key('l'); await key('Enter'); await key('Escape'); await key('Escape');
  await key('l', 'l with nothing selected');
  for (const k of ['f', '+', '=', '-', '-', '0', 'f', 'Shift+J', 'Backspace']) await key(k);
  for (const k of ['5', '6', '7', '8', '9']) await add(k);
  await key('Control+a'); await key('Backspace');
  // Buttons
  await click('button[title^="Undo"]', 'Undo button'); await click('button[title^="Undo"]', 'Undo button 2');
  await click('button[title^="Redo"]', 'Redo button');
  await click('button[title^="Zoom in"]'); await click('button[title^="Zoom out"]'); await click('button[title^="Fit"]');
  await key('Control+a');
  await click('text=Join in selection order (J)');
  await click('text=Group (Ctrl+G)'); await key('Escape');
  await click('button:text-is("Ungroup")');
  await z.act(async () => p.mouse.click(...await node(0)));
  await click('text=+ Comment'); await key('Escape');
  await z.act(async () => p.mouse.click(...await node(0)));
  await click('.panel button.danger', 'panel Delete');
  // Dialogs: Ctrl+S on an unsaved diagram opens Save as; Escape closes; Ctrl+Shift+S; save for real
  await key('Control+s'); await key('j', 'j while dialog open'); await key('Escape'); await key('Escape', 'Escape 2');
  await key('Control+Shift+s'); await key('Escape'); await key('Escape', 'Escape 2');
  await reveal('button[title^="Save as"]'); await click('button[title^="Save as"]', 'Save as button'); await key('Escape'); await key('Escape', 'Escape 2');
  await reveal('button[title^="Save ("]'); await click('button[title^="Save ("]', 'Save button');
  await p.fill('#saveas-name', 'keys'); await z.act(() => p.keyboard.press('Enter')); await state('saved');
  await key('Control+s', 'Ctrl+S saved');
  fs.writeFileSync(out, JSON.stringify(log, null, 1));
  console.log(log.length, 'states;', z.report() + '; errors:', errs);
  await b.close();
})();
