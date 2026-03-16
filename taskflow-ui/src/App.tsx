import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import AppLayout from './components/layout/AppLayout';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import BoardDetailPage from './pages/BoardDetailPage';

// ===== React Router =====
// Routing trong React tương tự routing trong ASP.NET:
//   .NET: [Route("api/boards/{id}")] → BoardsController.GetBoard()
//   React: <Route path="/boards/:boardId" element={<BoardDetailPage />} />
//
// BrowserRouter bọc toàn bộ app, giống app.MapControllers() trong .NET

// ===== Protected Route =====
// Component bảo vệ route - chỉ cho user đã login truy cập
// Giống [Authorize] attribute trong .NET Controller
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-gray-500">Loading...</div>
      </div>
    );
  }

  // Chưa login → redirect về /login
  // Giống 401 → redirect trong .NET Authentication middleware
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

// ===== Guest Route =====
// Ngược lại: nếu đã login mà vào /login → redirect về Dashboard
function GuestRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-gray-500">Loading...</div>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

function App() {
  return (
    <BrowserRouter>
      {/* AuthProvider bọc app → mọi component con đều truy cập được auth state */}
      <AuthProvider>
        {/* Toaster: hiển thị toast notifications (góc trên phải) */}
        <Toaster position="top-right" toastOptions={{ duration: 3000 }} />

        <Routes>
          {/* Guest routes - chỉ khi CHƯA login */}
          <Route path="/login" element={<GuestRoute><LoginPage /></GuestRoute>} />
          <Route path="/register" element={<GuestRoute><RegisterPage /></GuestRoute>} />

          {/* Protected routes - cần login */}
          {/* AppLayout bọc các trang con → có Navbar chung */}
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<DashboardPage />} />
            <Route path="/boards/:boardId" element={<BoardDetailPage />} />
          </Route>

          {/* Fallback: URL không tồn tại → về Dashboard */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
