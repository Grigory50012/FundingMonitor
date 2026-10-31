import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), react()],
  server: {
    host: "127.0.0.1",
    proxy: {
      "/api": {
        target: "https://localhost:5001",
        changeOrigin: true,
        // ASP.NET Core's local development certificate is not trusted by Node.
        secure: false,
      },
    },
  },
});
