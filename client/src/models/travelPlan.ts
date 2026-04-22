/** Odgovara JSON-u iz TravelPlansApi (DateOnly kao ISO datum). */
export type TravelPlan = {
  id: string;
  name: string;
  shortDescription: string;
  startDate: string;
  endDate: string;
  plannedBudget: number;
  generalNotes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type TravelPlanUpsert = {
  name: string;
  shortDescription: string;
  startDate: string;
  endDate: string;
  plannedBudget: number;
  generalNotes: string | null;
};
