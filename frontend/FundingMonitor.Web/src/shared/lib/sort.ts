export type SortDirection = "asc" | "desc" | null;

export function nextSortDirection(direction: SortDirection): SortDirection {
  if (direction === "asc") return "desc";
  if (direction === "desc") return null;
  return "asc";
}
