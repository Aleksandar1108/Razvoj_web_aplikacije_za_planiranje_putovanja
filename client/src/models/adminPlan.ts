export interface AdminTravelPlanRow {
  id: string;
  name: string;
  ownerUserId: string;
  ownerEmail: string;
  ownerDisplayName: string;
  startDate: string;
  endDate: string;
}

export type AdminCreateTravelPlanRequest = {
  ownerUserId: string;
  name: string;
  shortDescription: string;
  startDate: string;
  endDate: string;
  plannedBudget: number;
  generalNotes: string | null;
};
