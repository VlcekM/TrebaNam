<script lang="ts">
	import GlobeIcon from '@lucide/svelte/icons/globe';
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import MoonIcon from '@lucide/svelte/icons/moon';
	import MoreHorizontalIcon from '@lucide/svelte/icons/more-horizontal';
	import SunIcon from '@lucide/svelte/icons/sun';
	import { signOut } from '$lib/auth';
	import { LOCALE_NAMES, nextLocale, switchLocale } from '$lib/locale';
	import { m } from '$lib/paraglide/messages.js';
	import { toggleTheme } from '$lib/theme';
	import { cn } from '$lib/utils';

	// Kam sa panel rozvinie vie len volajuci: v bocnom paneli hore, v hlavicke dole.
	let { panelClass, class: className }: { panelClass?: string; class?: string } = $props();

	let open = $state(false);
	let root = $state<HTMLDivElement>();

	// Jazyky su dva, takze riadok ponuka rovno ten druhy a nie podponuku s vyberom.
	const other = nextLocale();

	function close() {
		open = false;
	}

	// Klik mimo a Escape menu zatvaraju - inak by ostalo visiet aj po navigacii.
	function onPointerDown(event: PointerEvent) {
		if (open && root && !root.contains(event.target as Node)) close();
	}

	function onKeyDown(event: KeyboardEvent) {
		if (event.key === 'Escape') close();
	}
</script>

<svelte:window onpointerdown={onPointerDown} onkeydown={onKeyDown} />

<div bind:this={root} class={cn('relative', className)}>
	<button
		type="button"
		onclick={() => (open = !open)}
		aria-haspopup="menu"
		aria-expanded={open}
		aria-label={m.app_menu()}
		title={m.app_menu()}
		class="inline-flex size-10 cursor-pointer items-center justify-center rounded-lg border border-tn-muted/40 bg-card/80 shadow-xs transition hover:bg-muted"
	>
		<MoreHorizontalIcon class="size-5" />
	</button>

	{#if open}
		<div
			class={cn(
				'absolute z-50 flex w-52 flex-col rounded-2xl border border-tn-muted/40 bg-card p-1.5 shadow-float',
				panelClass
			)}
		>
			<button
				type="button"
				onclick={() => {
					toggleTheme();
					close();
				}}
				class="inline-flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-left text-sm font-bold transition hover:bg-muted"
			>
				<!-- Ikonu vybera CSS, nie stav - inak by pri nacitani bliklo slnko na svetlej teme. -->
				<MoonIcon class="size-4 dark:hidden" />
				<SunIcon class="hidden size-4 dark:block" />
				{m.theme_toggle()}
			</button>

			<button
				type="button"
				onclick={() => switchLocale(other)}
				aria-label={m.language_switch()}
				class="inline-flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-left text-sm font-bold transition hover:bg-muted"
			>
				<GlobeIcon class="size-4" />
				<!-- Nazov jazyka sa neprekalada - kto ho hlada, hlada ho vo svojej reci. -->
				{LOCALE_NAMES[other]}
			</button>

			<button
				type="button"
				onclick={signOut}
				class="inline-flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-left text-sm font-bold transition hover:bg-muted"
			>
				<LogOutIcon class="size-4" />
				{m.app_logout()}
			</button>
		</div>
	{/if}
</div>
