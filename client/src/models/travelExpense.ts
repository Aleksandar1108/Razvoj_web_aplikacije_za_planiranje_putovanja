export const EXPENSE_CATEGORIES = [
  'transport',
  'accommodation',
  'food',
  'tickets',
  'shopping',
  'other',
] as const;

export type TravelExpenseCategory = (typeof EXPENSE_CATEGORIES)[number];

export type TravelExpense = {
  id: string;
  travelPlanId: string;
  name: string;
  category: TravelExpenseCategory;
  amount: number;
  expenseDate: string;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type TravelExpenseUpsert = {
  name: string;
  category: TravelExpenseCategory;
  amount: number;
  expenseDate: string;
  description: string | null;
};

export type ExpenseSummary = {
  plannedBudget: number;
  totalExpenses: number;
  totalExpenseEntries: number;
  totalActivityEstimatedCosts: number;
  remainingBudget: number;
};
