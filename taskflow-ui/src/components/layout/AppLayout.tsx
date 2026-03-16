import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { LogOut, LayoutDashboard } from 'lucide-react';

// ===== Layout Component =====
// Layout bao bọc tất cả trang authenticated (Dashboard, Board...)
// Chứa Navbar ở trên + content ở dưới
//
// <Outlet /> là placeholder của react-router:
// Trang con (Dashboard, Board...) sẽ được render vào vị trí <Outlet />
// Giống @RenderBody() trong ASP.NET Razor Layout

export default function AppLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navbar */}
      <nav className="bg-white border-b border-gray-200 px-6 py-3">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          {/* Logo / Brand */}
          <Link to="/" className="flex items-center gap-2 text-xl font-bold text-indigo-600">
            <LayoutDashboard size={24} />
            TaskFlow
          </Link>

          {/* Logout button */}
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 px-4 py-2 text-sm text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
          >
            <LogOut size={18} />
            Logout
          </button>
        </div>
      </nav>

      {/* Main content - trang con render ở đây */}
      <main className="max-w-7xl mx-auto p-6">
        <Outlet />
      </main>
    </div>
  );
}
