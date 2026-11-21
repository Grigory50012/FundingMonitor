import assert from "node:assert/strict";
import test from "node:test";
import { QueryClient, QueryObserver, focusManager } from "@tanstack/react-query";
import { historyRefreshOptions } from "./historyRefreshFixture.mjs";

const flush = () => new Promise((resolve) => setImmediate(resolve));

function setup(t) {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity } } });
  client.mount();
  t.after(() => {
    client.unmount();
    client.clear();
    focusManager.setFocused(undefined);
  });
  return client;
}

test("focus refetches after HH:01 but preserves fresh data before that boundary", async (t) => {
  let now = Date.parse("2026-11-18T12:58:00Z");
  t.mock.method(Date, "now", () => now);
  const client = setup(t);
  const key = ["historical-rates", "BTC-USDT", "history"];
  const previous = [{ fundingRate: 0.001 }];
  client.setQueryData(key, previous);
  let calls = 0;
  let resolve;
  const observer = new QueryObserver(client, {
    ...historyRefreshOptions,
    queryKey: key,
    queryFn: () => {
      calls++;
      return new Promise((done) => { resolve = done; });
    },
  });
  const unsubscribe = observer.subscribe(() => {});
  t.after(unsubscribe);

  now = Date.parse("2026-11-18T13:00:59Z");
  focusManager.setFocused(false);
  focusManager.setFocused(true);
  await flush();
  assert.equal(calls, 0);

  now = Date.parse("2026-11-18T13:01:00Z");
  focusManager.setFocused(false);
  focusManager.setFocused(true);
  await flush();
  assert.equal(calls, 1);
  assert.equal(observer.getCurrentResult().isFetching, true);
  assert.equal(observer.getCurrentResult().isLoading, false);
  assert.deepEqual(observer.getCurrentResult().data, previous);

  resolve([{ fundingRate: 0.002 }]);
  await flush();
  assert.equal(observer.getCurrentResult().data[0].fundingRate, 0.002);
});

test("a failed background request preserves the last successful result", async (t) => {
  const client = setup(t);
  const key = ["historical-rates", "BTC-USDT", "apr"];
  const previous = [{ apr: 36.5 }];
  client.setQueryData(key, previous);
  const observer = new QueryObserver(client, {
    ...historyRefreshOptions,
    queryKey: key,
    queryFn: async () => { throw new Error("Network unavailable"); },
    retry: false,
  });
  const unsubscribe = observer.subscribe(() => {});
  t.after(unsubscribe);
  await observer.refetch();
  assert.deepEqual(observer.getCurrentResult().data, previous);
  assert.equal(observer.getCurrentResult().isLoading, false);
  assert.equal(observer.getCurrentResult().isRefetchError, true);
});

test("manual refresh invalidates history and APR only for the selected symbol", async (t) => {
  const client = setup(t);
  for (const symbol of ["BTC-USDT", "ETH-USDT"]) {
    for (const kind of ["history", "apr"]) {
      client.setQueryData(["historical-rates", symbol, kind], []);
    }
  }
  await client.invalidateQueries({ queryKey: ["historical-rates", "BTC-USDT"] });
  for (const kind of ["history", "apr"]) {
    assert.equal(client.getQueryState(["historical-rates", "BTC-USDT", kind]).isInvalidated, true);
    assert.equal(client.getQueryState(["historical-rates", "ETH-USDT", kind]).isInvalidated, false);
  }
});
