export interface AdminTravelPlanOption {
  id: string;
  name: string;
  ownerUserId: string;
  ownerEmail: string;
  ownerDisplayName: string;
  startDate: string;
  endDate: string;
}

export interface AdminDestinationRow {
  id: string;
  travelPlanId: string;
  travelPlanName: string;
  ownerUserId: string;
  ownerEmail: string;
  ownerDisplayName: string;
  name: string;
  location: string;
  arrivalDate: string;
  departureDate: string;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}
