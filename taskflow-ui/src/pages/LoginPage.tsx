import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { LogIn } from 'lucide-react';
import toast from 'react-hot-toast';

// ===== Login Page =====
// useState: lưu giá trị input (email, password)
// Khi user gõ → setState → React update UI tự động
// Gọi là "Controlled Component" - React kiểm soát giá trị input

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { login } = useAuth();       // Lấy hàm login từ Context (giống inject)
  const navigate = useNavigate();      // Hook để redirect sau khi login

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();  // Ngăn form reload trang (default behavior của HTML form)
    setIsSubmitting(true);

    try {
      await login({ email, password });
      toast.success('Đăng nhập thành công!');
      navigate('/');  // Redirect về Dashboard
    } catch (err: unknown) {
      // Xử lý lỗi từ API
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Đăng nhập thất bại');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 to-blue-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl p-8 w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-indigo-100 rounded-full mb-4">
            <LogIn size={32} className="text-indigo-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900">Đăng nhập TaskFlow</h1>
          <p className="text-gray-500 mt-2">Quản lý công việc hiệu quả</p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              placeholder="user@example.com"
              className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Password
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="••••••••"
              className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all"
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full py-2.5 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors cursor-pointer"
          >
            {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>

        {/* Link to Register */}
        <p className="text-center mt-6 text-sm text-gray-500">
          Chưa có tài khoản?{' '}
          <Link to="/register" className="text-indigo-600 font-medium hover:underline">
            Đăng ký ngay
          </Link>
        </p>
      </div>
    </div>
  );
}
