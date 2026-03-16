import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authApi } from '../api/services';
import type { LoginRequest, RegisterRequest } from '../types';

// ===== React Context =====
// Context trong React giống Dependency Injection trong .NET:
// - Tạo 1 "container" chứa data/functions
// - Bất kỳ component con nào cũng truy cập được (không cần truyền props từng tầng)
//
// Ví dụ: User login → lưu token → TẤT CẢ components đều biết user đã login
// Giống cách .NET DI: đăng ký ICurrentUser 1 lần → inject ở bất kỳ Service nào

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
}

// Tạo Context (giống đăng ký interface trong DI)
const AuthContext = createContext<AuthContextType | null>(null);

// ===== AuthProvider =====
// Provider = component bao bọc app, cung cấp auth state cho tất cả children
// Giống AddScoped<IAuthService, AuthService>() trong .NET DI
export function AuthProvider({ children }: { children: ReactNode }) {
  // useState = cách React lưu trữ data có thể thay đổi
  // Khi state thay đổi → React tự động re-render UI (cập nhật giao diện)
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // useEffect = chạy code khi component mount (lần đầu render)
  // Giống constructor trong C# class
  // [] ở cuối = chỉ chạy 1 lần khi mount
  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    setIsAuthenticated(!!token); // !! chuyển string → boolean (có token = true)
    setIsLoading(false);
  }, []);

  const login = async (data: LoginRequest) => {
    const response = await authApi.login(data);
    // Lưu tokens vào localStorage (giống cookie nhưng đơn giản hơn)
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    setIsAuthenticated(true);
  };

  const register = async (data: RegisterRequest) => {
    const response = await authApi.register(data);
    localStorage.setItem('accessToken', response.data.accessToken);
    localStorage.setItem('refreshToken', response.data.refreshToken);
    setIsAuthenticated(true);
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

// ===== Custom Hook =====
// Hook = function đặc biệt bắt đầu bằng "use"
// Dùng để truy cập Context từ bất kỳ component nào
//
// Trong component: const { login, logout } = useAuth();
// Giống inject IAuthService trong constructor C#
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
