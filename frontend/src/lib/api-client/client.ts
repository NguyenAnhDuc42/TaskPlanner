import axios from "axios";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
  // ASP.NET Core [FromQuery] array binding expects repeating keys (priorities=High&priorities=Normal)
  // not the default indexed bracket form (priorities[0]=High) that axios uses for arrays.
  paramsSerializer: {
    serialize: (params) => {
      const sp = new URLSearchParams();
      for (const [key, val] of Object.entries(params)) {
        if (val === undefined || val === null) continue;
        if (Array.isArray(val)) {
          val.forEach((v) => sp.append(key, String(v)));
        } else {
          sp.append(key, String(val));
        }
      }
      return sp.toString();
    },
  },
});
