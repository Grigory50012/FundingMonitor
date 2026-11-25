import { useEffect, useState } from "react";

// Simple generic localStorage hook with auto-persistence
export function useLocalStorage<T>(
  key: string,
  initialValue: T,
  isValid?: (value: unknown) => boolean,
) {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const raw = localStorage.getItem(key);
      if (raw != null) {
        const parsed: unknown = JSON.parse(raw);
        if (!isValid || isValid(parsed)) return parsed as T;
      }
    } catch {
      // ignore parse errors
    }
    return initialValue;
  });

  useEffect(() => {
    try {
      localStorage.setItem(key, JSON.stringify(storedValue));
    } catch {
      // ignore write errors (e.g., storage disabled)
    }
  }, [key, storedValue]);

  return [storedValue, setStoredValue] as const;
}
