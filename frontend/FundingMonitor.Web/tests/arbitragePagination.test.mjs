import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/features/arbitrage/arbitragePagination.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext } });
const { paginateGroups } = await import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);

test("pagination keeps complete symbol groups together", () => {
  const groups = Array.from({ length: 60 }, (_, index) => ({ symbol: `COIN${index}`, others: [1, 2, 3] }));
  const page = paginateGroups(groups, 2, 25);
  assert.equal(page.pageCount, 3);
  assert.equal(page.page, 2);
  assert.equal(page.items.length, 25);
  assert.equal(page.items[0], groups[25]);
  assert.equal(page.items.at(-1), groups[49]);
});

test("shrinking results clamp the current page without returning an empty page", () => {
  assert.deepEqual(paginateGroups(["BTC", "ETH"], 4, 25), {
    items: ["BTC", "ETH"], page: 1, pageCount: 1, start: 1, end: 2,
  });
});

test("empty results have a valid page and a zero result range", () => {
  assert.deepEqual(paginateGroups([], 2, 25), { items: [], page: 1, pageCount: 1, start: 0, end: 0 });
});
