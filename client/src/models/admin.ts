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

export interface UpdateAdminUserRequest {
  isActive?: boolean;
  roleId?: number;
}
