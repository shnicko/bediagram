// Waiting for Blazor Server instead of sleeping.
//
// Blazor Server runs the app on the server and keeps the page up to date over one WebSocket. Every input it handles
// (a click, a key the app listens for, a pointer press) comes back as a render batch ("JS.RenderBatch"), and the
// browser answers each batch with "OnRenderCompleted" once the page has been redrawn. That happens even when nothing
// visible changed, usually 10–30 ms after the input. Each event (or key sent to OnKey) also goes out as a numbered call
// ("BeginInvokeDotNetFromJS" with a call id), and the server answers "JS.EndInvokeDotNet" with the same id once the
// handler has finished — including any awaited work such as writing a file. So instead of sleeping a fixed time and
// hoping the server has answered, a suite marks the traffic counters before an input and waits until:
//   - every numbered call the browser made has been answered (the handlers have finished),
//   - every batch received has been acknowledged (the page is drawn),
//   - every call the server made into the browser has finished (e.g. PNG export drawing the SVG onto a canvas), and
//   - nothing has crossed the socket for a short quiet spell.
// An input that sends nothing to the server (a key the app ignores) is over once a short idle time passes.
//
// Usage:
//   const blazor = require('./blazor').watch(page);   // before page.goto
//   await page.goto(url); await blazor.ready();
//   await blazor.act(() => page.click('…'));          // the click, then wait for the server's answer
//   console.log(blazor.report());                      // how many waits gave up (timed out)

// SignalR sends MessagePack frames; the method names appear in them as plain text, one per message.
const text = payload => typeof payload === 'string' ? payload : payload.toString('latin1');
const count = (payload, re) => (text(payload).match(re) || []).length;
// The call id follows the method name after a byte or two of MessagePack framing; calls that expect no answer have none.
const ids = (payload, re) => [...text(payload).matchAll(re)].map(m => m[1]);

function watch(page) {
  const c = { sent: 0, batches: 0, acks: 0, jsCalls: 0, jsDone: 0, last: Date.now() };
  const pending = new Set();   // ids of the browser's calls the server hasn't answered yet
  let timeouts = 0;
  const warnings = [];
  page.on('websocket', ws => {
    ws.on('framesent', f => {
      c.sent++;
      c.acks += count(f.payload, /OnRenderCompleted/g);
      c.jsDone += count(f.payload, /EndInvokeJSFromDotNet/g);
      for (const id of ids(f.payload, /BeginInvokeDotNetFromJS[^0-9]{1,3}(\d+)/g)) pending.add(id);
      c.last = Date.now();
    });
    ws.on('framereceived', f => {
      c.batches += count(f.payload, /JS\.RenderBatch/g);
      c.jsCalls += count(f.payload, /JS\.BeginInvokeJS/g);
      for (const id of ids(f.payload, /JS\.EndInvokeDotNet[^0-9]{1,3}(\d+)/g)) pending.delete(id);
      c.last = Date.now();
    });
  });
  const sleep = ms => new Promise(r => setTimeout(r, ms));

  /** The traffic counters now: pass to settled() after the input. */
  const mark = () => ({ ...c });

  /**
   * Waits until the page has caught up with everything sent to the server since `since` (see the top of the file).
   * quiet: ms with no traffic before calling it done; idle: how long to wait for traffic to start at all;
   * timeout: give up (counted in report()) — the comparison then shows whatever state the page was in.
   */
  async function settled(since, { quiet = 40, idle = 150, timeout = 15000 } = {}) {
    const start = Date.now();
    for (;;) {
      const now = Date.now();
      const busy = pending.size > 0 || c.acks < c.batches || c.jsDone < c.jsCalls;
      const answered = c.batches > since.batches;
      const asked = c.sent > since.sent;
      if (!asked && !answered && now - start >= idle) return;              // nothing went to the server
      if (answered && !busy && now - c.last >= quiet) return;              // answered, drawn, and quiet
      if (asked && !answered && !busy && now - c.last >= 5000) return;     // asked, but the app chose not to redraw
      if (now - start > timeout) { timeouts++; return; }
      await sleep(5);
    }
  }

  /** Runs an input, then waits for the server's answer. */
  async function act(input, opts) {
    const before = mark();
    await input();
    await settled(before, opts);
  }

  /** After page.goto: wait for the first render and for the start-up calls to finish. */
  async function ready() {
    await page.waitForSelector('.palette .tile, .palette', { timeout: 30000 });
    await settled({ ...c, batches: 0, sent: 0 }, { quiet: 300 });
  }

  /** Notes something that went wrong but shouldn't stop the run (e.g. a label box that never got focus). */
  const warn = message => warnings.push(message);

  const report = () => [timeouts ? `${timeouts} wait(s) timed out` : 'no waits timed out', ...warnings.map(w => `warning: ${w}`)].join('; ');

  return { mark, settled, act, ready, warn, report };
}

module.exports = { watch };
