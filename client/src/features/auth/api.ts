import apiClient from "@/lib/api-client";
import { LoginRequest, RegisterCommand, RegisterResponse, LoginResponse, MeResponse, RefreshTokenResponse, LogoutResponse } from "./type";


export const Register = async ( data: RegisterCommand) : Promise<RegisterResponse> => {
    try {
        const rep = await apiClient.post<RegisterResponse>("/auth/register", data)
        return rep.data
    } catch (error) {
        console.error("Error registering:", error)
        throw error
    }
}
export const Login = async ( data: LoginRequest) : Promise<LoginResponse> => {
    try {
        const rep = await apiClient.post<LoginResponse>("/auth/login", data)
        return rep.data
    } catch (error) {
        console.error("Error logging in:", error)
        throw error
    }
}
export const Logout = async () : Promise<LogoutResponse> => {
    try {
        const rep = await apiClient.post("/auth/logout", {})
        return rep.data
    } catch (error) {
        console.error("Error logging out:", error)
        throw error
    }
}
export const Me = async () : Promise<MeResponse> => {
    try {
        const rep = await apiClient.get<MeResponse>("/auth/me")
        return rep.data
    } catch (error) {
        console.error("Error fetching user:", error)
        throw error
    }
}

export const RefreshToken = async () : Promise<RefreshTokenResponse> => {
   try {
     const rep = await apiClient.post<RefreshTokenResponse>("/auth/refresh")
     return rep.data
   } catch (error) {
    console.error("Error refreshing token:", error)
    throw error
   }
}




