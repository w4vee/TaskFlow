// ===== Types cho Frontend =====
// Giống DTOs bên .NET, nhưng dùng TypeScript interface
// interface = định nghĩa "hình dạng" của data (có những field nào, kiểu gì)

// --- Auth ---
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

// --- Board ---
export interface TaskBoard {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  createdAt: string;
  taskCount: number;
}

export interface CreateBoardRequest {
  name: string;
  description?: string;
}

export interface UpdateBoardRequest {
  name: string;
  description?: string;
}

// --- Task ---
// Enum trong TypeScript - tương tự enum trong C#
export type TaskItemStatus = 'Todo' | 'InProgress' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  status: TaskItemStatus;
  priority: TaskPriority;
  deadline: string | null;
  isOverdue: boolean;
  boardId: string;
  assignedToId: string | null;
  assignedToName: string | null;
  createdAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  priority: TaskPriority;
  deadline?: string;
  assignedToId?: string;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  status: TaskItemStatus;
  priority: TaskPriority;
  deadline?: string;
  assignedToId?: string;
}
