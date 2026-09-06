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

/** Polozka nakupneho zoznamu z GET /api/items. */
export interface Item {
	id: string;
	name: string;
	quantity?: string;
	category: string;
	isChecked: boolean;
	/** Odnesena cast mnozstva, ked sa v obchode kupila len cast. */
	boughtQuantity?: string;
	/** Poznamka pre toho, kto pojde nakupovat. */
	note?: string;
	addedByUserID: string;
	createdAt: string;
}

/** Riadok ukonceneho nakupu - odpis polozky, ktora uz v zozname nie je. */
export interface ShoppingRecordItem {
	id: string;
	name: string;
	quantity?: string;
	category: string;
}

/** Ukonceny nakup z GET /api/shopping-records. */
export interface ShoppingRecord {
	id: string;
	completedByUserID: string;
	completedAt: string;
	/** Cena celeho nakupu v eurach; chyba, kym ju nikto nezadal. */
	totalCost?: number;
	items: ShoppingRecordItem[];
}

/** Vec, ktoru domacnost uz kupovala, z GET /api/items/suggestions. */
export interface ItemSuggestion {
	name: string;
	quantity?: string;
	category: string;
	count: number;
}
