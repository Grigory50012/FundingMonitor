import assert from "node:assert/strict";
import test from "node:test";
import { historyRefreshOptions } from "./historyRefreshFixture.mjs";

// QueryObserver enables interval timers in a browser environment.
globalThis.window = {};
const { QueryClient, QueryObserver, focusManager } = await import("@tanstack/react-query");
const flush = () => new Promise((resolve) => setImmediate(resolve));

test("polls at HH:01, stays idle between hours, and skips hidden-tab polling", async (t) => {
  t.mock.timers.enable({
    apis: ["Date", "setTimeout", "setInterval"],
    now: Date.parse("2026-11-18T12:00:00Z"),
  });
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity } } });
  const key = ["historical-rates", "BTC-USDT", "history"];
  client.setQueryData(key, [{ value: 0 }]);
  focusManager.setFocused(true);
  let calls = 0;
  const observer = new QueryObserver(client, {
    ...historyRefreshOptions,
    queryKey: key,
    queryFn: async () => [{ value: ++calls }],
  });
  const unsubscribe = observer.subscribe(() => {});
  t.after(() => {
    unsubscribe();
    client.clear();
    focusManager.setFocused(undefined);
  });

  t.mock.timers.tick(59_999);
  await flush();
  assert.equal(calls, 0);
  t.mock.timers.tick(1);
  await flush();
  assert.equal(calls, 1);
  assert.equal(observer.getCurrentResult().isLoading, false);
  t.mock.timers.tick(3_599_999);
  await flush();
  assert.equal(calls, 1);
  t.mock.timers.tick(1);
  await flush();
  assert.equal(calls, 2);

  focusManager.setFocused(false);
  t.mock.timers.tick(3_600_000);
  await flush();
  assert.equal(calls, 2);
});
