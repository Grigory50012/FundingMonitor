import { describe, expect, it } from "vitest";
import type { FundingArbitrageDto } from "../../types";
import { getProfitability } from "./arbitrageTableModel";

describe("getProfitability", () => {
  it("returns the backend-calculated APR spread without recalculating it", () => {
    const item = { aprSpread: -12.5 } as FundingArbitrageDto;

    expect(getProfitability(item)).toBe(-12.5);
  });
});
