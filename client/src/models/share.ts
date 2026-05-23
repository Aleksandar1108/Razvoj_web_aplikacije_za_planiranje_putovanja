export type SharePermission = 'view' | 'edit';

export type CreateShareLinkResponse = {
  travelPlanId: string;
  permission: SharePermission;
  token: string;
  qrPayloadJson: string;
};

export type SharedTravelPlanListItem = {
  travelPlanId: string;
  permission: SharePermission;
  name: string;
  shortDescription: string;
  startDate: string;
  endDate: string;
  updatedAtUtc: string;
};

export type QrPayloadV1 = {
  v: 1;
  pid: string;
  p: SharePermission;
  t: string;
};
