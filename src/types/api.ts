/**
 * This file was auto-generated based on the OpenAPI specification.
 * Do not modify it manually unless necessary.
 */

export type UserRole = 0 | 1;

export type MatchRequestStatus = 0 | 1 | 2 | 3;

export type TrainingRequestStatus = 0 | 1 | 2 | 3;

export interface AddSportDto {
  sportId: string;
  typicalDistanceKm?: number | null;
}

export interface AssignSportsDto {
  sportIds?: string[] | null;
}

export interface CreateTrainingRequestDto {
  receiverId?: string;
  trainingDateTime?: string | null;
  location?: string | null;
  message?: string | null;
}

export interface LoginDto {
  email?: string | null;
  password?: string | null;
}

export interface MatchRequestDto {
  fromUserId?: string;
  toUserId?: string;
  sportId?: string;
}

export interface MatchRequestStatusDto {
  status: MatchRequestStatus;
}

export interface MatchRequestViewDto {
  id?: string;
  fromUserId?: string;
  toUserId?: string;
  sportId?: string;
  sportName?: string | null;
  status?: string | null;
  createdAt?: string;
}

export interface RegisterDto {
  email?: string | null;
  password?: string | null;
  name?: string | null;
  role?: string | null;
}

export interface RequestPasswordResetDto {
  email?: string | null;
}

export interface ResetPasswordDto {
  token?: string | null;
  newPassword?: string | null;
}

export interface Sport {
  id?: string;
  name: string;
  description?: string | null;
  typicalDistanceKm?: number | null;
  userSports?: UserSport[] | null;
}

export interface TrainingRequestStatsDto {
  dailyRequests?: number;
  weeklyRequests?: number;
  monthlyRequests?: number;
  dailyResponses?: number;
  weeklyResponses?: number;
  monthlyResponses?: number;
}

export interface UpdateLocationDto {
  latitude?: number;
  longitude?: number;
  searchRadiusKm?: number;
}

export interface UpdateProfileDto {
  name?: string | null;
  email?: string | null;
  age?: number | null;
  description?: string | null;
}

export interface UpdateSportDistanceDto {
  typicalDistanceKm?: number;
}

export interface UpdateTrainingRequestStatusDto {
  status: TrainingRequestStatus;
}

export interface User {
  id?: string;
  name: string;
  email: string;
  isAvailableNow?: boolean;
  searchRadiusKm?: number;
  age?: number | null;
  description?: string | null;
  userSports?: UserSport[] | null;
  role?: UserRole;
  isBlocked?: boolean;
  latitude?: number;
  longitude?: number;
  profilePicturePath?: string | null;
}

export interface UserMatchDto {
  id?: string;
  name?: string | null;
  email?: string | null;
  isAvailableNow?: boolean;
  searchRadiusKm?: number;
  distanceKm?: number;
  sharedSportIds?: string[] | null;
}

export interface UserSport {
  userId: string;
  user?: User;
  typicalDistanceKm?: number;
  sportId: string;
  sport?: Sport;
}

export interface UserSportDto {
  sportId?: string;
  name?: string | null;
  description?: string | null;
  typicalDistanceKm?: number | null;
}
