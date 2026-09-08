import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs['recommended-latest'],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
  },
  {
    files: ["src/features/current-rates/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "../historical-rates",
                "../historical-rates/**",
                "../../historical-rates",
                "../../historical-rates/**",
                "**/features/historical-rates",
                "**/features/historical-rates/**",
                "../arbitrage",
                "../arbitrage/**",
                "../../arbitrage",
                "../../arbitrage/**",
                "**/features/arbitrage",
                "**/features/arbitrage/**",
                "../exchange-health",
                "../exchange-health/**",
                "../../exchange-health",
                "../../exchange-health/**",
                "**/features/exchange-health",
                "**/features/exchange-health/**",
              ],
              message: "Feature internals must not import other feature internals. Compose features in widgets or app.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/historical-rates/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "../current-rates",
                "../current-rates/**",
                "../../current-rates",
                "../../current-rates/**",
                "**/features/current-rates",
                "**/features/current-rates/**",
                "../arbitrage",
                "../arbitrage/**",
                "../../arbitrage",
                "../../arbitrage/**",
                "**/features/arbitrage",
                "**/features/arbitrage/**",
                "../exchange-health",
                "../exchange-health/**",
                "../../exchange-health",
                "../../exchange-health/**",
                "**/features/exchange-health",
                "**/features/exchange-health/**",
              ],
              message: "Feature internals must not import other feature internals. Compose features in widgets or app.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/arbitrage/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "../current-rates",
                "../current-rates/**",
                "../../current-rates",
                "../../current-rates/**",
                "**/features/current-rates",
                "**/features/current-rates/**",
                "../historical-rates",
                "../historical-rates/**",
                "../../historical-rates",
                "../../historical-rates/**",
                "**/features/historical-rates",
                "**/features/historical-rates/**",
                "../exchange-health",
                "../exchange-health/**",
                "../../exchange-health",
                "../../exchange-health/**",
                "**/features/exchange-health",
                "**/features/exchange-health/**",
              ],
              message: "Feature internals must not import other feature internals. Compose features in widgets or app.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/exchange-health/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "../current-rates",
                "../current-rates/**",
                "../../current-rates",
                "../../current-rates/**",
                "**/features/current-rates",
                "**/features/current-rates/**",
                "../historical-rates",
                "../historical-rates/**",
                "../../historical-rates",
                "../../historical-rates/**",
                "**/features/historical-rates",
                "**/features/historical-rates/**",
                "../arbitrage",
                "../arbitrage/**",
                "../../arbitrage",
                "../../arbitrage/**",
                "**/features/arbitrage",
                "**/features/arbitrage/**",
              ],
              message: "Feature internals must not import other feature internals. Compose features in widgets or app.",
            },
          ],
        },
      ],
    },
  },
])
