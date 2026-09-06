import { redirect } from '@sveltejs/kit';
import type { LayoutLoad } from './$types';
import type { User } from '$lib/types';
import { userStore } from '$lib/stores/user';

// /app/* je ciste SPA: ziadny prerender, data sa tahaju z API az v prehliadaci.
export const ssr = false;
export const prerender = false;

// Auth brana: bez platnej cookie posle /api/auth/me 401 a my cloveka poslem na Google.
export const load: LayoutLoad = async ({ fetch }): Promise<User> => {
	const res = await fetch('/api/auth/me', { credentials: 'include' });

	if (res.status === 401) {
		throw redirect(302, '/api/auth/login');
	}

	if (!res.ok) {
		throw new Error(`GET /api/auth/me failed with ${res.status}`);
	}

	const user = (await res.json()) as User;

	userStore.set(user);

	return user;
};
