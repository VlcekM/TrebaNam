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

	// Handoff ma pod avatarmi mena clenov ("Ana + Luka"); pri jednom clenovi je to len jeho meno.
	const names = $derived(
		(household?.members ?? []).map((member) => member.givenName ?? member.name ?? '').join(' + ')
	);
</script>

<!--
	Bocny panel z handoffu (230px, hairline vpravo). Existuje len na sirokych displejoch -
	na uzkych tu istu navigaciu nesie hlavicka.
-->
<aside
	class="hidden w-[230px] flex-none flex-col gap-0.5 border-r border-tn-muted/40 px-3.5 py-5 lg:sticky lg:top-0 lg:flex lg:h-[100svh]"
>
	<a href="/app" class="flex items-center gap-2.5 px-2.5 pb-3.5">
		<Logo class="size-7" />
		<Wordmark class="text-sm tracking-[0.08em] uppercase" />
	</a>

	<!-- Bez domacnosti sa este nie je kam prepinat, tak sa navigacia neukazuje vobec. -->
	{#if household}
		{#each items as item (item.href)}
			{@const active = isActive(page.url.pathname, item.href)}

			<a
				href={item.href}
				aria-current={active ? 'page' : undefined}
				class="flex items-center justify-between rounded-lg px-2.5 py-2.5 text-[13.5px] transition {active
					? 'bg-tn-accent font-bold'
					: 'font-bold text-tn-meta hover:bg-muted'}"
			>
				<span>{item.label}</span>

				{#if item.href === '/app/household' && household}
					<span class="text-[11px] font-bold {active ? 'text-tn-primary-strong' : 'text-tn-faint'}">
						{household.members.length}
					</span>
				{/if}
			</a>
		{/each}
	{/if}

	<div class="flex-1"></div>

	<div class="mx-1.5 mb-2 h-px bg-tn-muted/40"></div>

	<div class="flex items-center gap-2 px-1.5 pb-0.5">
		{#if household}
			<!-- Avatary sa prekryvaju rovnako ako v handoffe, prstenec ich oddeli od pozadia. -->
			<div class="flex">
				{#each household.members as member, index (member.id)}
					<MemberAvatar {member} {index} class="size-8 text-[11px] {index > 0 ? '-ml-2.5' : ''}" />
				{/each}
			</div>

			<span class="min-w-0 truncate text-[13px] text-tn-meta">{names}</span>
		{/if}

		<MoreMenu class="ml-auto" panelClass="bottom-full left-0 mb-2" />
	</div>
</aside>
