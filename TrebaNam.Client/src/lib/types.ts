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
