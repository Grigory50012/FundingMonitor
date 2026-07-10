import type { FundingArbitrageDto } from "../../types";

export type ArbitrageExchangeRowsProps = {
  item: FundingArbitrageDto;
  showSymbol: boolean;
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void;
};

export function ArbitrageExchangeRows() {
  return null;
}
