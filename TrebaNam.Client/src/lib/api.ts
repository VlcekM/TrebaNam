/**
 * Tenky wrapper nad fetch pre /api/*. Rovnaky origin, takze staci relativna URL
 * a cookie ide sama; credentials: 'include' je len poistka.
 */
export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
	const res = await fetch(path, {
		credentials: 'include',
		...init,
		headers: {
			...(init.body ? { 'Content-Type': 'application/json' } : {}),
			...(init.headers ?? {})
		}
	});

	if (!res.ok) {
		throw new ApiError(res.status, await res.text().catch(() => ''));
	}

	// 204 alebo prazdne telo
	const text = await res.text();
	return (text ? JSON.parse(text) : undefined) as T;
}

export class ApiError extends Error {
	constructor(
		public status: number,
		public body: string
	) {
		super(`API ${status}: ${body}`);
	}

	/** Telo ako objekt, ked ho API poslalo ako JSON; inak undefined. */
	json<T>(): T | undefined {
		try {
			return JSON.parse(this.body) as T;
		} catch {
			return undefined;
		}
	}
}
