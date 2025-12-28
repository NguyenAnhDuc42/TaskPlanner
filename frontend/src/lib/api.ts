import axios from "axios";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,

  headers: {
    "Content-Type": "application/json",
  },
});

// Add a "Middleware" (Interceptor) to handle 401s globally
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // If your ASP.NET backend returns 401 Unauthorized
    if (error.response?.status === 401) {
      console.log("Not logged in or session expired.");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);
