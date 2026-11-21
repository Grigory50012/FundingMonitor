import assert from "node:assert/strict";
import test from "node:test";
import { getNextHistoryRefreshAt } from "./historyRefreshFixture.mjs";

for (const [now, expected] of [
  ["2026-11-18T12:00:00Z", "2026-11-18T12:01:00Z"],
  ["2026-11-18T12:00:59.999Z", "2026-11-18T12:01:00Z"],
  ["2026-11-18T12:01:00Z", "2026-11-18T13:01:00Z"],
  ["2026-11-18T12:58:00Z", "2026-11-18T13:01:00Z"],
  ["2026-11-18T23:59:59Z", "2026-11-19T00:01:00Z"],
]) {
  test(`next refresh after ${now} is ${expected}`, () => {
    assert.equal(getNextHistoryRefreshAt(Date.parse(now)), Date.parse(expected));
  });
}
