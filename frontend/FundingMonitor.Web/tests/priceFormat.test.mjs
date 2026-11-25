import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("../src/shared/lib/priceFormat.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
});
const { buildPriceFormats, formatAssetPrice, toFullPrice } =
  await import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
const normalized = (value) => value.replace(/[\u00a0\u202f]/g, " ");
const samples = (values, symbol = "X-USDT") => values.map((value) => ({ symbol, value }));
const formatFor = (...values) => buildPriceFormats(samples(values)).get("X-USDT");

for (const [input, expected] of [
  [67432.1299, "$67 432,12"],
  [12.345678, "$12,3456"],
  [0.0234567, "$0,023456"],
  [0.0002323, "$0,0₃23230"],
  [0.000000123456, "$0,0₆12345"],
  [999.99999, "$999,9999"],
  [0.000999999, "$0,0₃99999"],
  [0.999999, "$0,99999"],
  [1, "$1,0000"],
  [1000, "$1 000,00"],
  [0.001, "$0,0010000"],
  [-12.345678, "-$12,3456"],
  [-0.000000123456, "-$0,0₆12345"],
  [1.00000001e-12, "$0,0₁₁10000"],
]) {
  test(`price truncates without crossing a range: ${input}`, () => {
    assert.equal(normalized(formatAssetPrice(input, formatFor(input), "ru-RU")), expected);
  });
}

test("three exchanges share precision across a range boundary", () => {
  const format = formatFor(999.98765, 1000.12345, 1001.5);
  assert.deepEqual(format, { fractionDigits: 4, compact: false });
  assert.equal(normalized(formatAssetPrice(999.98765, format, "ru-RU")), "$999,9876");
  assert.equal(normalized(formatAssetPrice(1000.12345, format, "ru-RU")), "$1 000,1234");
  assert.equal(normalized(formatAssetPrice(1001.5, format, "ru-RU")), "$1 001,5000");
});

test("crossing the compact threshold switches the whole asset to decimal notation", () => {
  const format = formatFor(0.000999999, 0.00100001);
  assert.deepEqual(format, { fractionDigits: 8, compact: false });
  assert.equal(formatAssetPrice(0.000999999, format, "ru-RU"), "$0,00099999");
  assert.equal(formatAssetPrice(0.00100001, format, "ru-RU"), "$0,00100001");
});

test("asset formats ignore order, duplicates, invalid values and zeros", () => {
  const values = [12.34, 12.345678, 1000.1, 0, -0, null, undefined, NaN, Infinity];
  const rows = samples(values);
  const before = [...rows];
  const formats = buildPriceFormats(rows);
  assert.deepEqual(rows, before);
  assert.deepEqual(formats, buildPriceFormats([...rows].reverse().concat(rows)));
  assert.deepEqual(formats.get("X-USDT"), { fractionDigits: 4, compact: false });
  assert.deepEqual(formatFor(0, null, NaN), { fractionDigits: 2, compact: false });
  assert.deepEqual(formatFor(0.0002323, 0, null), { fractionDigits: 8, compact: true });
});

test("separate assets and refreshed snapshots choose independent formats", () => {
  const rows = [...samples([1000, 1001], "BTC-USDT"), ...samples([0.00001], "MEME-USDT")];
  const formats = buildPriceFormats(rows);
  assert.deepEqual(formats.get("BTC-USDT"), { fractionDigits: 2, compact: false });
  assert.deepEqual(formats.get("MEME-USDT"), { fractionDigits: 9, compact: true });
  const visible = rows.slice(0, 1);
  assert.equal(formatAssetPrice(visible[0].value, formats.get(visible[0].symbol), "en-US"), "$1,000.00");
  assert.deepEqual(formatFor(999.99, 1000.1), { fractionDigits: 4, compact: false });
  assert.deepEqual(formatFor(1000, 1000.1), { fractionDigits: 2, compact: false });
});

test("money spread inherits asset precision and its own sign", () => {
  const format = formatFor(12.345678, 12.348999);
  assert.equal(formatAssetPrice(0.003321, format, "ru-RU", true), "+$0,0033");
  assert.equal(formatAssetPrice(-0.003321, format, "ru-RU", true), "-$0,0033");
  assert.equal(formatAssetPrice(0.000001, format, "ru-RU", true), "$0,0000");
  assert.equal(formatAssetPrice(-0.000001, format, "ru-RU", true), "$0,0000");
  assert.equal(formatAssetPrice(0, format, "ru-RU", true), "$0,0000");
  assert.equal(formatAssetPrice(0.001, formatFor(67432), "ru-RU", true), "$0,00");
});

test("compact spread uses the asset decimals with its own leading-zero count", () => {
  const format = formatFor(0.0002323);
  assert.equal(formatAssetPrice(0.00000232, format, "ru-RU", true), "+$0,0₅232");
  assert.equal(formatAssetPrice(-0.00000232999, format, "ru-RU", true), "-$0,0₅232");
  assert.equal(formatAssetPrice(1e-9, format, "ru-RU", true), "$0,00000000");
});

test("missing prices are non-numeric placeholders and actual zeros have no sign", () => {
  const format = formatFor(12.345678);
  for (const value of [null, undefined, NaN, Infinity, -Infinity]) {
    assert.equal(formatAssetPrice(value, format, "en-US"), "—");
  }
  assert.equal(formatAssetPrice(-0, format, "en-US"), "$0.00");
  assert.equal(formatAssetPrice(0, format, "en-US"), "$0.00");
});

test("locale separators are applied after truncation", () => {
  const format = formatFor(1234.1299);
  assert.equal(formatAssetPrice(1234.1299, format, "en-US"), "$1,234.12");
  assert.equal(formatAssetPrice(1234.1299, format, "de-DE"), "$1.234,12");
});

test("decimal truncation avoids losing a digit to binary multiplication", () => {
  assert.equal(formatAssetPrice(1.005, { fractionDigits: 3, compact: false }, "en-US"), "$1.005");
  assert.equal(formatAssetPrice(0.29, { fractionDigits: 2, compact: false }, "en-US"), "$0.29");
});

test("full values expand exponents, retain source digits and normalize negative zero", () => {
  assert.equal(toFullPrice(1.23456e-7), "0.000000123456");
  assert.equal(toFullPrice(-1.23456e-7), "-0.000000123456");
  assert.equal(toFullPrice(1.23456e21), "1234560000000000000000");
  assert.equal(toFullPrice(12.345678), "12.345678");
  assert.equal(toFullPrice(-0), "0");
  assert.equal(toFullPrice(Number.MIN_VALUE), `0.${"0".repeat(323)}5`);
});

test("extreme finite numbers never become zero or scientific notation", () => {
  assert.equal(formatAssetPrice(Number.MIN_VALUE, formatFor(Number.MIN_VALUE), "en-US"), "$0.0₃₂₃50000");
  const formatted = formatAssetPrice(1.23456e21, formatFor(1.23456e21), "en-US");
  assert.equal(formatted, "$1,234,560,000,000,000,000,000.00");
  assert.ok(!formatAssetPrice(Number.MAX_VALUE, formatFor(Number.MAX_VALUE), "en-US").includes("e"));
});
