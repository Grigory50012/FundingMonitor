import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/widgets/dashboard/dashboardRefresh.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext } });
const { startDashboardRefresh } = await import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);

test("both refresh requests start on the same tick without waiting for responses", (t) => {
  t.mock.timers.enable({ apis: ["Date", "setTimeout", "setInterval"], now: 1_000_000 });
  globalThis.document = new EventTarget();
  document.visibilityState = "visible";
  globalThis.window = new EventTarget();
  const calls = [];
  const stop = startDashboardRefresh(() => {
    calls.push(["current", Date.now()]);
    void new Promise(() => {});
    calls.push(["arbitrage", Date.now()]);
  });
  t.after(() => { stop(); delete globalThis.document; delete globalThis.window; });

  t.mock.timers.tick(4_999);
  assert.deepEqual(calls, []);
  t.mock.timers.tick(1);
  assert.deepEqual(calls, [["current", 1_005_000], ["arbitrage", 1_005_000]]);

  document.visibilityState = "hidden";
  t.mock.timers.tick(10_000);
  assert.equal(calls.length, 2);
  document.visibilityState = "visible";
  document.dispatchEvent(new Event("visibilitychange"));
  assert.deepEqual(calls.slice(-2), [["current", 1_015_000], ["arbitrage", 1_015_000]]);

  window.dispatchEvent(new Event("online"));
  assert.equal(calls.length, 6);
  stop();
  t.mock.timers.tick(10_000);
  window.dispatchEvent(new Event("online"));
  document.dispatchEvent(new Event("visibilitychange"));
  assert.equal(calls.length, 6);
});
