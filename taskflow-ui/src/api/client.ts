import axios from 'axios';

// ===== Axios Instance =====
// Tạo 1 instance axios với config mặc định
// Giống HttpClient trong C# - cấu hình baseURL 1 lần, dùng lại khắp nơi
const api = axios.create({
  baseURL: 'http://localhost:5156/api',  // URL của .NET API
  headers: {
    'Content-Type': 'application/json',
  },
});

// ===== Request Interceptor =====
// Interceptor = "người gác cổng" chặn MỌI request trước khi gửi đi
// Giống Pipeline Behavior trong MediatR - xen vào giữa flow
//
// Mục đích: Tự động gắn JWT token vào header Authorization
// → Không cần viết headers: { Authorization: 'Bearer ...' } mỗi lần gọi API
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ===== Response Interceptor =====
// Chặn MỌI response trả về, xử lý lỗi chung
// Nếu 401 (Unauthorized) → token hết hạn → redirect về login
api.interceptors.response.use(
  (response) => response,  // Response OK → trả về bình thường
  (error) => {
    if (error.response?.status === 401) {
      // Token hết hạn hoặc invalid → xóa token + redirect login
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
