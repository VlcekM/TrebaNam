<script lang="ts">
	import { page } from '$app/state';
	import Logo from '$lib/components/Logo.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import MoreMenu from '$lib/components/MoreMenu.svelte';
	import Wordmark from '$lib/components/Wordmark.svelte';
	import { isActive, navItems } from '$lib/nav';
	import type { Household } from '$lib/types';

	let { household }: { household?: Household } = $props();

	const items = $derived(navItems());
</script>

<!--
	Hlavicka je mobilny protipol bocneho panela, preto od lg mizne. Podla handoffu nesie
	znacku vlavo a stacknutych clenov vpravo; ostatne volby su schovane v menu.
-->
<header
	class="sticky top-0 z-40 border-b border-tn-muted/40 bg-background/85 backdrop-blur lg:hidden"
	style="padding-top: env(safe-area-inset-top)"
>
	<div class="mx-auto flex max-w-2xl flex-col gap-2 px-4 pt-2.5">
		<div class="flex items-center gap-3">
			<a href="/app" class="flex items-center gap-2">
				<Logo class="size-8" />
				<Wordmark class="text-[11px] tracking-[0.1em] uppercase" />
			</a>

			<div class="ml-auto flex items-center gap-2">
				{#if household}
					<div class="flex">
						{#each household.members as member, index (member.id)}
							<MemberAvatar {member} {index} class="size-8 text-xs {index > 0 ? '-ml-2.5' : ''}" />
						{/each}
					</div>
				{/if}

				<MoreMenu panelClass="top-full right-0 mt-2" />
			</div>
		</div>

		<!-- Bez domacnosti sa este nie je kam prepinat, tak sa navigacia neukazuje vobec. -->
		{#if household}
			<nav class="-mx-1 flex gap-1.5 overflow-x-auto px-1 pb-2">
				{#each items as item (item.href)}
					{@const active = isActive(page.url.pathname, item.href)}

					<a
						href={item.href}
						aria-current={active ? 'page' : undefined}
						class="rounded-lg px-3 py-1.5 text-[13.5px] font-bold whitespace-nowrap transition {active
							? 'bg-tn-accent'
							: 'text-tn-meta'}"
					>
						{item.label}
					</a>
				{/each}
			</nav>
		{:else}
			<div class="pb-2.5"></div>
		{/if}
	</div>
</header>
