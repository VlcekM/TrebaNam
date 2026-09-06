<script lang="ts">
	import MoonIcon from '@lucide/svelte/icons/moon';
	import SunIcon from '@lucide/svelte/icons/sun';
	import { m } from '$lib/paraglide/messages.js';
	import { toggleTheme } from '$lib/theme';
	import { cn } from '$lib/utils';

	// Na verejnych strankach visi prepinac fixne v rohu, v /app sedi priamo
	// v hlavicke - preto sa pozicovanie posiela zvonka.
	let { class: className }: { class?: string } = $props();
</script>

<button
	type="button"
	onclick={toggleTheme}
	title={m.theme_toggle()}
	aria-label={m.theme_toggle()}
	class={cn(
		'inline-flex size-10 cursor-pointer items-center justify-center rounded-lg border border-tn-muted/40 bg-card/80 shadow-xs backdrop-blur transition hover:bg-muted',
		className
	)}
>
	<!--
		Ktora ikona sa zobrazi riesi CSS, nie JS stav - inak by sa pri hydratacii
		na chvilu ukazala ikona pre nespravnu temu.
	-->
	<MoonIcon class="size-5 dark:hidden" />
	<SunIcon class="hidden size-5 dark:block" />
</button>
