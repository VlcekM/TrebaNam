<script lang="ts">
	import { cn } from '$lib/utils';

	// Rovnaka kresba ako favicon a PWA ikony - nakupny zoznam, dva riadky odskrtnute a tretia
	// polozka este caka. Zaoblenie dlazdice drzime na 12/48, teda rovnaky pomer ako 16/64 vo
	// favicone. Dlazdica, listok, fajky a ciary beru farby stranky, takze v tmavej teme logo
	// stmavne rovnako, ako iOS stmavuje ikony; cervena a modra su literaly, lebo favicon.svg
	// ziadne premenne necita a kresba sa s nim musi zhodovat.
	let { class: className }: { class?: string } = $props();
</script>

<svg viewBox="0 0 64 64" aria-hidden="true" class={cn('size-9', className)}>
	<rect width="64" height="64" rx="16" fill="var(--background)" />
	<rect x="11" y="7" width="42" height="50" rx="7" fill="var(--card)" stroke="var(--tn-muted)" stroke-opacity="0.4" stroke-width="1.5" />
	<rect x="17" y="13.5" width="11" height="11" rx="3" fill="#e0483f" />
	<path d="m19.6 19 2.1 2.1 4.4-4.6" fill="none" stroke="var(--primary-foreground)" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" />
	<rect x="32" y="16.5" width="15" height="5" rx="2.5" fill="var(--foreground)" />
	<rect x="17" y="26.5" width="11" height="11" rx="3" fill="#4f8fd6" />
	<path d="m19.6 32 2.1 2.1 4.4-4.6" fill="none" stroke="var(--primary-foreground)" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" />
	<rect x="32" y="29.5" width="11" height="5" rx="2.5" fill="var(--foreground)" />
	<rect x="18" y="40.5" width="9" height="9" rx="2.2" fill="none" stroke="var(--tn-primary)" stroke-width="2" />
	<rect x="32" y="42.5" width="13" height="5" rx="2.5" fill="var(--foreground)" />
</svg>
