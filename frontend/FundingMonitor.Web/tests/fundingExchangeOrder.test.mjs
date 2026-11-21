import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/widgets/dashboard/fundingExchangeOrder.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
const { getFundingExchangeOrder } = await import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
const unsorted = { source: "current", config: { column: "apr", direction: null } };

test("opposite API orders produce a single canonical exchange order", () => {
  assert.deepEqual(getFundingExchangeOrder(
    [{ exchange: "Binance" }, { exchange: "Bybit" }],
    [{ exchange: "Bybit" }, { exchange: "Binance" }], [], unsorted,
  ), ["Binance", "Bybit"]);
});

test("missing data keeps an exchange row and selected exchanges limit the universe", () => {
  assert.deepEqual(getFundingExchangeOrder(
    [{ exchange: "Bybit" }, { exchange: "OKX" }], [{ exchange: "Binance" }],
    ["Binance", "Bybit"], unsorted,
  ), ["Binance", "Bybit"]);
});

test("current-rate sorting controls both tables and keeps missing values last", () => {
  assert.deepEqual(getFundingExchangeOrder(
    [{ exchange: "Binance", apr: 10 }, { exchange: "Bybit", apr: 20 }], [{ exchange: "OKX" }], [],
    { source: "current", config: { column: "apr", direction: "desc" } },
  ), ["Bybit", "Binance", "OKX"]);
});

test("history sorting uses the selected period and stable exchange tie breaks", () => {
  assert.deepEqual(getFundingExchangeOrder([], [
    { exchange: "Bybit", period: "1 день", apr: 10 },
    { exchange: "Binance", period: "1 день", apr: 10 },
    { exchange: "Binance", period: "7 дней", apr: 100 },
  ], [], { source: "history", config: { column: "apr", period: "1 день", direction: "asc" } }),
  ["Binance", "Bybit"]);
});
