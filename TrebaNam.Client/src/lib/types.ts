// DTO tvary zrkadlime rucne podla TrebaNam.API - ziadny generovany klient.

/** Odpoved z GET /api/auth/me. */
export interface User {
	id: string;
	isAdmin: boolean;
	name?: string;
	emailAddress?: string;
	givenName?: string;
	surname?: string;
	pictureUrl?: string;
}

/** Clen domacnosti z GET /api/households/me. */
export interface HouseholdMember {
	id: string;
	name?: string;
	givenName?: string;
	pictureUrl?: string;
	joinedAt: string;
}

/** Odpoved z GET /api/households/me. Bez domacnosti vracia API 204, teda undefined. */
export interface Household {
	id: string;
	name: string;
	inviteCode: string;
	createdAt: string;
	members: HouseholdMember[];
}

/** Nahlad pozvanky z GET /api/households/invite/{code}. */
export interface HouseholdInvite {
	name: string;
	memberCount: number;
	alreadyMember: boolean;
	inAnotherHousehold: boolean;
}
