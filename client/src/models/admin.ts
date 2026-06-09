export interface AdminUserRow {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface AdminSystemStats {
  totalUsers: number;
  activeUsers: number;
  adminUsers: number;
}

export interface CreateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleId: number;
  isActive: boolean;
}

export interface UpdateAdminUserRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  password?: string;
  isActive?: boolean;
  roleId?: number;
}
