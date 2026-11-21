import { readFile } from "node:fs/promises";
import ts from "typescript";

// Keep tests compatible with Node 20, which cannot import TypeScript directly.
const source = await readFile(new URL("../src/features/historical-rates/historyRefreshSchedule.ts", import.meta.url), "utf8");
const { outputText } = ts.transpileModule(source, {
  compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
});
export const { getNextHistoryRefreshAt, historyRefreshOptions } = await import(
  `data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`
);
