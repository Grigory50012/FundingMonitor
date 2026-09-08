import { apiClient } from "../../../shared/api/client";
import type { paths } from "../../../types/generated/api";

type ExchangeHealth = paths["/api/v1/exchanges/health"]["get"]["responses"][200]["content"]["application/json"];

export async function getExchangeHealth(): Promise<ExchangeHealth> {
  const response = await apiClient.get<ExchangeHealth>("/exchanges/health");
  return response.data;
}
