// Clicks through every control in the properties panel for each kind of selection, recording the editor state
// after each one. Run against two builds and compare the logs. Steps are named by context and control (not by
// position), and every step starts from the same diagram: its changes are undone (or the diagram reloaded) after it.
const { chromium } = require('playwright');
const fs = require('fs');
const [url, out] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1600, height: 1000 } });
  const errs = []; p.on('pageerror', e => errs.push(e.message)); p.on('console', m => { if (m.type() === 'error' && !m.text().includes('404')) errs.push(m.text()); });
  // Opens the test diagram and adds a text box and a comment so their panels exist. Run again whenever a step
  // leaves something that undo can't put back.
  const setup = async () => {
    await p.goto(url); await p.waitForTimeout(2500);
    await p.click('button[title="Open a saved diagram"]'); await p.waitForTimeout(400);
    await p.click('.files button.link:text-is("shapes")'); await p.waitForTimeout(800);
    await p.keyboard.press('f'); await p.waitForTimeout(300);
    await p.mouse.click(1250, 950); await p.waitForTimeout(150);
    await p.keyboard.press('t'); await p.waitForTimeout(250); await p.keyboard.press('Escape'); await p.keyboard.press('Escape');
    await p.keyboard.press('c'); await p.waitForTimeout(250); await p.keyboard.press('Escape'); await p.keyboard.press('Escape');
    await p.waitForTimeout(200);
  };
  await setup();
  const W = 180, log = [];
  const state = async (label) => log.push({ label, ...(await p.evaluate(() => {
    const ids = {}; let n = 0;
    const norm = t => t.replace(/\b[netg]_[0-9a-f]{8}\b/g, m => ids[m] || (ids[m] = '#' + (n++)));
    return {
      svg: norm(document.querySelector('svg.surface').outerHTML),
      panel: norm(document.querySelector('aside.panel').innerText),
      selected: document.querySelectorAll('.selected').length,
      editing: !!document.querySelector('.inline-edit, #edit-box'),
      toast: document.querySelector('.toast')?.textContent || null,
      undo: document.querySelector('button[title^="Undo"]').disabled,
    };
  })) });
  const centreOf = async (sel, i) => p.evaluate(([s, i]) => { const e = document.querySelectorAll(s)[i]; if (!e) return null; const r = e.getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; }, [sel, i]);
  const edgeMid = async (i) => p.evaluate(i => { const pth = document.querySelectorAll('.edge-hit')[i]; if (!pth) return null; const pt = pth.getPointAtLength(pth.getTotalLength() / 2); const m = pth.getScreenCTM(); return [pt.x * m.a + m.e, pt.y * m.d + m.f]; }, i);
  const contexts = [
    ['nothing', async () => { await p.keyboard.press('Escape'); await p.keyboard.press('Escape'); }],
    ...[0, 2, 3, 4, 5, 6, 8, 9, 13, 19, 20, 22].map(i => [`node ${i}`, async () => { const c = await centreOf('.node > path:not(.node-shadow)', i); if (c) await p.mouse.click(...c); }]),
    ['text box', async () => { const c = await centreOf('g.textbox:not(.comment) rect, g.textbox:not(.comment) path', 0); if (c) await p.mouse.click(...c); }],
    ['comment', async () => { const c = await centreOf('g.textbox.comment rect, g.textbox.comment path', 0); if (c) await p.mouse.click(...c); }],
    ['edge 0', async () => { const c = await edgeMid(0); if (c) await p.mouse.click(...c); }],
    ['edge 5', async () => { const c = await edgeMid(5); if (c) await p.mouse.click(...c); }],
    ['group', async () => { const c = await centreOf('.group-header', 0); if (c) await p.mouse.click(...c); }],
    ['multi', async () => { await p.keyboard.press('Escape'); await p.keyboard.press('Control+a'); }],
  ];
  const CONTROLS = 'aside.panel button:not(.icon-btn), aside.panel .icon-btn:nth-child(-n+2), aside.panel .alignbar .icon-btn, aside.panel input[type=checkbox], aside.panel select, aside.panel input[type=range]';
  // Each control is named by what it says (text, tooltip, or the label next to it), with " (2)", " (3)" … for repeats,
  // so a step keeps its name when a control is added or removed elsewhere in the panel.
  const controls = async () => {
    const hs = await p.$$(CONTROLS);
    const names = await p.evaluate(sel => {
      const seen = {};
      // The label or heading just before the control (in its own row, or before the row it sits in).
      const isTitle = x => ['LABEL', 'H3', 'H4'].includes(x.tagName) && !x.querySelector('input');
      const labelOf = e => {
        const own = e.closest('label'); if (own) return own.innerText;
        for (let from = e; from && from.tagName !== 'ASIDE'; from = from.parentElement) {
          let x = from.previousElementSibling;
          while (x && !isTitle(x) && !x.querySelector('input, select, button')) x = x.previousElementSibling;
          if (x && isTitle(x)) return x.innerText;
          if (x) return '';
        }
        return '';
      };
      return [...document.querySelectorAll(sel)].map(e => {
        const text = (e.tagName === 'BUTTON' ? (e.textContent.trim() || e.title) : labelOf(e) || e.title || e.value || '').trim().replace(/\s+/g, ' ').slice(0, 40);
        const base = `${e.tagName.toLowerCase()}${e.type && e.tagName === 'INPUT' ? ':' + e.type : ''} "${text}"`;
        seen[base] = (seen[base] || 0) + 1;
        return seen[base] > 1 ? `${base} (${seen[base]})` : base;
      });
    }, CONTROLS);
    return hs.map((h, i) => ({ h, name: names[i] }));
  };
  const canvas = async () => p.evaluate(() => document.querySelector('svg.surface').outerHTML.replace(/\b[netg]_[0-9a-f]{8}\b/g, ''));
  // Wait until the canvas stops changing (slow actions such as Auto-layout finish after the click returns).
  const settle = async () => {
    let last = await canvas();
    for (let k = 0; k < 20; k++) { await p.waitForTimeout(150); const now = await canvas(); if (now === last) return; last = now; }
  };
  const deselect = async () => { await p.keyboard.press('Escape'); await p.keyboard.press('Escape'); await p.mouse.click(1250, 950); await p.waitForTimeout(120); };
  // Undo until the canvas is back to how it was before the step; reload the diagram if undo can't get there.
  let reloads = 0;
  const restore = async (before) => {
    await deselect(); await settle();
    for (let k = 0; k < 8 && (await canvas()) !== before; k++) {
      if (await p.evaluate(() => document.querySelector('button[title^="Undo"]').disabled)) break;
      await p.keyboard.press('Control+z'); await settle();
    }
    if ((await canvas()) !== before) { reloads++; await setup(); }
  };
  for (const [name, select] of contexts) {
    await deselect(); await select(); await p.waitForTimeout(W);
    const names = (await controls()).map(c => c.name);
    for (const ctrl of names) {
      if (ctrl.startsWith('button "Open')) continue;
      await deselect(); await settle();
      const before = await canvas();
      await select(); await p.waitForTimeout(W);
      const c = (await controls()).find(x => x.name === ctrl)?.h;
      if (!c) { log.push({ label: `${name} · ${ctrl} — not there` }); continue; }
      const tag = await c.evaluate(e => e.tagName + ':' + (e.type || ''));
      try {
        if (tag.startsWith('SELECT')) {
          const opts = await c.evaluate(e => [...e.options].map(o => o.value));
          await c.selectOption(opts[Math.min(1, opts.length - 1)] === await c.inputValue() ? opts[opts.length - 1] : opts[Math.min(1, opts.length - 1)]);
        } else if (tag === 'INPUT:range') {
          await c.evaluate(e => { e.value = '0.25'; e.dispatchEvent(new Event('input', { bubbles: true })); e.dispatchEvent(new Event('change', { bubbles: true })); });
        } else await c.click({ timeout: 2000 });
      } catch (e) { log.push({ label: `${name} · ${ctrl} — could not interact` }); await restore(before); continue; }
      await p.waitForTimeout(W); await settle();
      // close a dialog if one opened (e.g. confirm leave on a link)
      if (await p.$('.modal')) { await p.keyboard.press('Escape'); await p.keyboard.press('Escape'); await p.waitForTimeout(150); }
      await state(`${name} · ${ctrl}`);
      await restore(before);
    }
  }
  fs.writeFileSync(out, JSON.stringify(log, null, 1));
  console.log(log.length, 'steps;', reloads, 'reloads; errors:', errs);
  await b.close();
})();
