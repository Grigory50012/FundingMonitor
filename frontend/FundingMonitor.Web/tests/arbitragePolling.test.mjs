import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/features/arbitrage/arbitrageRefreshOptions.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext } });
const { arbitrageRefreshOptions } = await import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
const clockSource = await readFile(new URL("../src/widgets/dashboard/dashboardRefresh.ts", import.meta.url), "utf8");
const clockCode = ts.transpileModule(clockSource, { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { startDashboardRefresh } = await import(`data:text/javascript;base64,${Buffer.from(clockCode).toString("base64")}`);
globalThis.window = new EventTarget();
globalThis.document = new EventTarget();
document.visibilityState = "visible";
const { QueryClient, QueryObserver, focusManager } = await import("@tanstack/react-query");
const flush = () => new Promise((resolve) => setImmediate(resolve));

test("five-second polling deduplicates slow requests, preserves data, and supports pause", async (t) => {
  t.mock.timers.enable({ apis: ["Date", "setTimeout", "setInterval"], now: 1_000_000 });
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity } } });
  const key = ["arbitrage", {}];
  client.setQueryData(key, [{ symbol: "BTC-USDT" }]);
  focusManager.setFocused(true);
  let calls = 0;
  let complete;
  const queryFn = () => {
    calls++;
    return new Promise((resolve) => { complete = resolve; });
  };
  const options = (enabled) => ({ ...arbitrageRefreshOptions(enabled), queryKey: key, queryFn });
  const observer = new QueryObserver(client, options(true));
  const unsubscribe = observer.subscribe(() => {});
  let autoRefresh = true;
  const stop = startDashboardRefresh(() => {
    if (autoRefresh) void observer.refetch({ cancelRefetch: false });
  });
  t.after(() => { stop(); unsubscribe(); client.clear(); focusManager.setFocused(undefined); });

  t.mock.timers.tick(4_999);
  await flush();
  assert.equal(calls, 0);
  t.mock.timers.tick(1);
  await flush();
  assert.equal(calls, 1);
  assert.equal(observer.getCurrentResult().isLoading, false);
  assert.equal(observer.getCurrentResult().data[0].symbol, "BTC-USDT");
  t.mock.timers.tick(15_000);
  await flush();
  assert.equal(calls, 1);

  complete([{ symbol: "ETH-USDT" }]);
  await flush();
  autoRefresh = false;
  observer.setOptions(options(false));
  t.mock.timers.tick(20_000);
  await flush();
  assert.equal(calls, 1);
  assert.equal(observer.getCurrentResult().data[0].symbol, "ETH-USDT");

  autoRefresh = true;
  observer.setOptions(options(true));
  document.visibilityState = "hidden";
  t.mock.timers.tick(10_000);
  await flush();
  assert.equal(calls, 1);
  document.visibilityState = "visible";
  t.mock.timers.tick(5_000);
  await flush();
  assert.equal(calls, 2);
  complete([]);
  await flush();
});
