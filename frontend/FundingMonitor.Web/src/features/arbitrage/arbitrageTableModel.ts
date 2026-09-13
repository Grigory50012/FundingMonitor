import type { FundingArbitrageDto } from "../../types";

export const calcFundingSpread = (item: FundingArbitrageDto): number => {
  return item.fundingRateSpread * 100;
};

export const calcFundingRate = (rate: number): number => rate * 100;

export const getProfitability = (item: FundingArbitrageDto): number =>
  item.aprSpread;
