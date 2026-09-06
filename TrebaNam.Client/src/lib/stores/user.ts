import { writable } from 'svelte/store';
import type { User } from '$lib/types';

/** Prihlaseny pouzivatel - plni ho load v src/routes/app/+layout.ts. */
export const userStore = writable<User | null>(null);
