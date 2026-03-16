import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { boardApi, taskApi } from '../api/services';
import type { TaskBoard, TaskItem, TaskItemStatus, TaskPriority } from '../types';
import { ArrowLeft, Plus, X, Pencil, Trash2, Clock, AlertTriangle } from 'lucide-react';
import toast from 'react-hot-toast';

// ===== Kanban Board Page =====
// Hiển thị tasks theo columns: Todo | InProgress | Done | Cancelled
// Mỗi column chứa các task cards có thể click để thay đổi status

// Cấu hình cho mỗi status column
const STATUS_CONFIG: Record<TaskItemStatus, { label: string; color: string; bg: string }> = {
  Todo: { label: 'To Do', color: 'text-gray-700', bg: 'bg-gray-100' },
  InProgress: { label: 'In Progress', color: 'text-blue-700', bg: 'bg-blue-100' },
  Done: { label: 'Done', color: 'text-green-700', bg: 'bg-green-100' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bg: 'bg-red-100' },
};

const PRIORITY_CONFIG: Record<TaskPriority, { label: string; color: string }> = {
  Low: { label: 'Low', color: 'text-gray-500' },
  Medium: { label: 'Medium', color: 'text-yellow-600' },
  High: { label: 'High', color: 'text-orange-600' },
  Critical: { label: 'Critical', color: 'text-red-600' },
};

const ALL_STATUSES: TaskItemStatus[] = ['Todo', 'InProgress', 'Done', 'Cancelled'];
const ALL_PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical'];

export default function BoardDetailPage() {
  // useParams: lấy params từ URL (vd: /boards/:boardId → boardId)
  // Giống {boardId} trong ASP.NET route [Route("api/boards/{boardId}")]
  const { boardId } = useParams<{ boardId: string }>();

  const [board, setBoard] = useState<TaskBoard | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskItem | null>(null);

  // Form state cho create/edit task
  const [formTitle, setFormTitle] = useState('');
  const [formDesc, setFormDesc] = useState('');
  const [formPriority, setFormPriority] = useState<TaskPriority>('Medium');
  const [formDeadline, setFormDeadline] = useState('');
  const [formStatus, setFormStatus] = useState<TaskItemStatus>('Todo');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchData = useCallback(async () => {
    if (!boardId) return;
    try {
      const [boardRes, tasksRes] = await Promise.all([
        boardApi.getById(boardId),
        taskApi.getByBoard(boardId),
      ]);
      setBoard(boardRes.data);
      setTasks(tasksRes.data);
    } catch {
      toast.error('Không thể tải dữ liệu board');
    } finally {
      setIsLoading(false);
    }
  }, [boardId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Lọc tasks theo status cho mỗi column
  const getTasksByStatus = (status: TaskItemStatus) =>
    tasks.filter((t) => t.status === status);

  const openCreateModal = () => {
    setEditingTask(null);
    setFormTitle('');
    setFormDesc('');
    setFormPriority('Medium');
    setFormDeadline('');
    setFormStatus('Todo');
    setShowCreateModal(true);
  };

  const openEditModal = (task: TaskItem) => {
    setEditingTask(task);
    setFormTitle(task.title);
    setFormDesc(task.description || '');
    setFormPriority(task.priority);
    setFormDeadline(task.deadline ? task.deadline.split('T')[0] : '');
    setFormStatus(task.status);
    setShowCreateModal(true);
  };

  const handleSubmitTask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!boardId) return;
    setIsSubmitting(true);

    try {
      if (editingTask) {
        // Update existing task
        await taskApi.update(boardId, editingTask.id, {
          title: formTitle,
          description: formDesc || undefined,
          priority: formPriority,
          deadline: formDeadline ? `${formDeadline}T00:00:00Z` : undefined,
          status: formStatus,
        });
        toast.success('Cập nhật task thành công!');
      } else {
        // Create new task
        await taskApi.create(boardId, {
          title: formTitle,
          description: formDesc || undefined,
          priority: formPriority,
          deadline: formDeadline ? `${formDeadline}T00:00:00Z` : undefined,
        });
        toast.success('Tạo task thành công!');
      }
      setShowCreateModal(false);
      fetchData();
    } catch {
      toast.error(editingTask ? 'Cập nhật thất bại' : 'Tạo task thất bại');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleStatusChange = async (task: TaskItem, newStatus: TaskItemStatus) => {
    if (!boardId || task.status === newStatus) return;
    try {
      await taskApi.updateStatus(boardId, task.id, newStatus);
      fetchData();
      toast.success(`Chuyển sang ${STATUS_CONFIG[newStatus].label}`);
    } catch {
      toast.error('Cập nhật status thất bại');
    }
  };

  const handleDeleteTask = async (taskId: string) => {
    if (!boardId || !confirm('Xóa task này?')) return;
    try {
      await taskApi.delete(boardId, taskId);
      setTasks(tasks.filter((t) => t.id !== taskId));
      toast.success('Đã xóa task');
    } catch {
      toast.error('Xóa task thất bại');
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-gray-500">Đang tải...</div>
      </div>
    );
  }

  if (!board) {
    return <div className="text-center py-16 text-gray-500">Board không tồn tại</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <Link
            to="/"
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <ArrowLeft size={20} />
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{board.name}</h1>
            {board.description && (
              <p className="text-gray-500 text-sm mt-0.5">{board.description}</p>
            )}
          </div>
        </div>

        <button
          onClick={openCreateModal}
          className="flex items-center gap-2 px-4 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
        >
          <Plus size={20} />
          Thêm Task
        </button>
      </div>

      {/* Kanban Columns */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {ALL_STATUSES.map((status) => {
          const config = STATUS_CONFIG[status];
          const columnTasks = getTasksByStatus(status);

          return (
            <div key={status} className="bg-gray-50 rounded-xl p-3 min-h-[200px]">
              {/* Column Header */}
              <div className={`flex items-center gap-2 px-2 py-1.5 rounded-lg ${config.bg} mb-3`}>
                <span className={`text-sm font-semibold ${config.color}`}>
                  {config.label}
                </span>
                <span className={`text-xs font-medium ${config.color} bg-white/60 px-1.5 py-0.5 rounded-full`}>
                  {columnTasks.length}
                </span>
              </div>

              {/* Task Cards */}
              <div className="space-y-2">
                {columnTasks.map((task) => (
                  <TaskCard
                    key={task.id}
                    task={task}
                    onEdit={() => openEditModal(task)}
                    onDelete={() => handleDeleteTask(task.id)}
                    onStatusChange={(newStatus) => handleStatusChange(task, newStatus)}
                    currentStatus={status}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </div>

      {/* Create/Edit Task Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-gray-900">
                {editingTask ? 'Chỉnh sửa Task' : 'Tạo Task mới'}
              </h2>
              <button
                onClick={() => setShowCreateModal(false)}
                className="p-1 hover:bg-gray-100 rounded-lg cursor-pointer"
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleSubmitTask} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tiêu đề *</label>
                <input
                  type="text"
                  value={formTitle}
                  onChange={(e) => setFormTitle(e.target.value)}
                  required
                  placeholder="Tên task"
                  className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                  autoFocus
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Mô tả</label>
                <textarea
                  value={formDesc}
                  onChange={(e) => setFormDesc(e.target.value)}
                  placeholder="Chi tiết task..."
                  rows={3}
                  className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none resize-none"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Ưu tiên</label>
                  <select
                    value={formPriority}
                    onChange={(e) => setFormPriority(e.target.value as TaskPriority)}
                    className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                  >
                    {ALL_PRIORITIES.map((p) => (
                      <option key={p} value={p}>{PRIORITY_CONFIG[p].label}</option>
                    ))}
                  </select>
                </div>

                {editingTask && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
                    <select
                      value={formStatus}
                      onChange={(e) => setFormStatus(e.target.value as TaskItemStatus)}
                      className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                    >
                      {ALL_STATUSES.map((s) => (
                        <option key={s} value={s}>{STATUS_CONFIG[s].label}</option>
                      ))}
                    </select>
                  </div>
                )}

                <div className={editingTask ? 'col-span-2' : ''}>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Deadline</label>
                  <input
                    type="date"
                    value={formDeadline}
                    onChange={(e) => setFormDeadline(e.target.value)}
                    className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none"
                  />
                </div>
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
                  disabled={isSubmitting}
                  className="flex-1 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors cursor-pointer"
                >
                  {isSubmitting ? 'Đang lưu...' : editingTask ? 'Cập nhật' : 'Tạo Task'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

// ===== Task Card Component =====
// Component riêng cho mỗi task card trong Kanban column
function TaskCard({
  task,
  onEdit,
  onDelete,
  onStatusChange,
  currentStatus,
}: {
  task: TaskItem;
  onEdit: () => void;
  onDelete: () => void;
  onStatusChange: (status: TaskItemStatus) => void;
  currentStatus: TaskItemStatus;
}) {
  const priorityConfig = PRIORITY_CONFIG[task.priority];

  // Tìm status tiếp theo hợp lý (Todo→InProgress→Done)
  const nextStatus: TaskItemStatus | null =
    currentStatus === 'Todo' ? 'InProgress' :
    currentStatus === 'InProgress' ? 'Done' :
    null;

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-3 hover:shadow-md transition-all group">
      {/* Title + Actions */}
      <div className="flex items-start justify-between gap-2">
        <h4 className="text-sm font-medium text-gray-900 flex-1">{task.title}</h4>
        <div className="flex gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
          <button onClick={onEdit} className="p-1 hover:bg-gray-100 rounded cursor-pointer">
            <Pencil size={14} className="text-gray-400" />
          </button>
          <button onClick={onDelete} className="p-1 hover:bg-red-50 rounded cursor-pointer">
            <Trash2 size={14} className="text-gray-400 hover:text-red-500" />
          </button>
        </div>
      </div>

      {/* Description preview */}
      {task.description && (
        <p className="text-xs text-gray-500 mt-1 line-clamp-2">{task.description}</p>
      )}

      {/* Meta info */}
      <div className="flex items-center justify-between mt-3">
        <div className="flex items-center gap-2">
          {/* Priority badge */}
          <span className={`text-xs font-medium ${priorityConfig.color}`}>
            {task.priority === 'Critical' && <AlertTriangle size={12} className="inline mr-0.5" />}
            {priorityConfig.label}
          </span>

          {/* Deadline */}
          {task.deadline && (
            <span className={`text-xs flex items-center gap-0.5 ${task.isOverdue ? 'text-red-500' : 'text-gray-400'}`}>
              <Clock size={12} />
              {new Date(task.deadline).toLocaleDateString('vi-VN')}
            </span>
          )}
        </div>
      </div>

      {/* Quick status change button */}
      {nextStatus && (
        <button
          onClick={() => onStatusChange(nextStatus)}
          className="w-full mt-2 py-1.5 text-xs font-medium text-indigo-600 bg-indigo-50 hover:bg-indigo-100 rounded-md transition-colors cursor-pointer"
        >
          → {STATUS_CONFIG[nextStatus].label}
        </button>
      )}
    </div>
  );
}
