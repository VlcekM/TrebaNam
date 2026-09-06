/** Odhlasenie s plnym reloadom - stav SPA po odhlaseni nema co prezit. */
export async function signOut() {
	await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
	window.location.href = '/';
}
