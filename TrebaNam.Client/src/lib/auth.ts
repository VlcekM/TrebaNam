import { forgetEverything } from '$lib/offline/db';

/** Odhlasenie s plnym reloadom - stav SPA po odhlaseni nema co prezit. */
export async function signOut() {
	await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });

	// Odlozene odpovede aj neodoslane zapisy patria tomu, kto sa prave odhlasil. Na spolocnom
	// telefone nema zoznam jednej domacnosti cakat na toho dalsieho.
	await forgetEverything();

	window.location.href = '/';
}
