export type TravelDestination = {
  id: string;
  travelPlanId: string;
  name: string;
  location: string;
  arrivalDate: string;
  departureDate: string;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type TravelDestinationUpsert = {
  name: string;
  location: string;
  arrivalDate: string;
  departureDate: string;
  notes: string | null;
};
