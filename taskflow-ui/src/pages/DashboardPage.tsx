import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { boardApi } from '../api/services';
import type { TaskBoard } from '../types';
import { Plus, Clipboard, Trash2, X } from 'lucide-react';
import toast from 'react-hot-toast';

export default function DashboardPage() {
  // State management
  const [boards, setBoards] = useState<TaskBoard[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newBoardName, setNewBoardName] = useState('');
  const [newBoardDesc, setNewBoardDesc] = useState('');
  const [isCreating, setIsCreating] = useState(false);

  // useEffect chạy khi component mount → fetch boards từ API
  useEffect(() => {
    fetchBoards();
  }, []);

  const fetchBoards = async () => {
    try {
      const response = await boardApi.getAll();
      setBoards(response.data);
    } catch {
      toast.error('Không thể tải danh sách boards');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateBoard = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsCreating(true);
    try {
      await boardApi.create({ name: newBoardName, description: newBoardDesc || undefined });
      toast.success('Tạo board thành công!');
      setShowCreateModal(false);
      setNewBoardName('');
      setNewBoardDesc('');
      fetchBoards();  // Refresh danh sách
    } catch {
      toast.error('Tạo board thất bại');
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteBoard = async (id: string, name: string) => {
    if (!confirm(`Bạn chắc chắn muốn xóa board "${name}"?`)) return;
    try {
      await boardApi.delete(id);
      toast.success('Đã xóa board');
      setBoards(boards.filter((b) => b.id !== id));  // Xóa khỏi state (không cần gọi lại API)
    } catch {
      toast.error('Xóa board thất bại');
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-500">Đang tải...</div>
      </div>
    );
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Boards</h1>
          <p className="text-gray-500 mt-1">Quản lý các bảng công việc của bạn</p>
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-4 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
        >
          <Plus size={20} />
          Tạo Board
        </button>
      </div>

      {/* Board Grid */}
      {boards.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border-2 border-dashed border-gray-300">
          <Clipboard size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900">Chưa có board nào</h3>
          <p className="text-gray-500 mt-1">Tạo board đầu tiên để bắt đầu!</p>
          <button
            onClick={() => setShowCreateModal(true)}
            className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
          >
            Tạo Board
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {boards.map((board) => (
            <div
              key={board.id}
              className="bg-white rounded-xl border border-gray-200 p-5 hover:shadow-lg hover:border-indigo-200 transition-all group"
            >
              <div className="flex items-start justify-between">
                <Link to={`/boards/${board.id}`} className="flex-1">
                  <h3 className="font-semibold text-gray-900 group-hover:text-indigo-600 transition-colors">
                    {board.name}
                  </h3>
                  {board.description && (
                    <p className="text-sm text-gray-500 mt-1 line-clamp-2">{board.description}</p>
                  )}
                </Link>
                <button
                  onClick={() => handleDeleteBoard(board.id, board.name)}
                  className="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors opacity-0 group-hover:opacity-100 cursor-pointer"
                >
                  <Trash2 size={16} />
                </button>
              </div>

              <div className="mt-4 flex items-center justify-between text-sm">
                <span className="text-gray-500">
                  {board.taskCount} {board.taskCount === 1 ? 'task' : 'tasks'}
                </span>
                <span className="text-gray-400">
                  {new Date(board.createdAt).toLocaleDateString('vi-VN')}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Board Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-gray-900">Tạo Board mới</h2>
              <button
                onClick={() => setShowCreateModal(false)}
                className="p-1 hover:bg-gray-100 rounded-lg cursor-pointer"
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleCreateBoard} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên board *
                </label>
                <input
                  type="text"
                  value={newBoardName}
                  onChange={(e) => setNewBoardName(e.target.value)}
                  required
                  placeholder="Ví dụ: Sprint 1"
                  className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                  autoFocus
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Mô tả
                </label>
                <textarea
                  value={newBoardDesc}
                  onChange={(e) => setNewBoardDesc(e.target.value)}
                  placeholder="Mô tả board (tùy chọn)"
                  rows={3}
                  className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none resize-none"
                />
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="flex-1 py-2.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isCreating}
                  className="flex-1 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors cursor-pointer"
                >
                  {isCreating ? 'Đang tạo...' : 'Tạo Board'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
