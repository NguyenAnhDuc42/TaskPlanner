import path from "path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { tanstackRouter } from "@tanstack/router-plugin/vite";
// https://vite.dev/config/
export default defineConfig({
  plugins: [
    tanstackRouter({
      target: "react",
      autoCodeSplitting: true,
    }),
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {
      // This intercepting any call starting with /api
      "/api": {
        // Change this to https://localhost:5001 if you are using HTTPS in .NET
        target: "http://localhost:5000",
        changeOrigin: true,
        secure: false, // Allows self-signed certs in development
      },
    },
  },
});
