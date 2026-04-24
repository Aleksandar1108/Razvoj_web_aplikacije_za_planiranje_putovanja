export type ChecklistItem = {
  id: string;
  travelPlanId: string;
  title: string;
  isDone: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type ChecklistItemCreate = {
  title: string;
};
