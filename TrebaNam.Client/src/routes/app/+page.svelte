<script lang="ts">
	import ArrowRightIcon from '@lucide/svelte/icons/arrow-right';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import ShoppingCartIcon from '@lucide/svelte/icons/shopping-cart';
	import ItemDialog from '$lib/components/ItemDialog.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import { listCount, listMore, tripItemCount } from '$lib/counts';
	import { tripDate } from '$lib/dates';
	import { listDot } from '$lib/lists';
	import { money } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let dialogOpen = $state(false);

	const items = $derived(data.items);
	const lists = $derived(data.lists);
	const members = $derived(data.household?.members ?? []);
	const trips = $derived(data.records.slice(0, 3));

	// Kazdy zoznam ma svoju kartu: nahlad, nie zoznam - zvysok je za odkazom na samotnu obrazovku.
	const previewOf = $derived((listID: string) => {
		const own = items.filter((item) => item.listID === listID);

		return { preview: own.slice(0, 5), rest: Math.max(own.length - 5, 0) };
	});

	// Nakupuje sa vzdy z jedneho zoznamu, takze pri viacerych sa vybera na zozname a nie tu -
	// tlacidlo na prehlade by muselo hadat, do ktoreho obchodu sa clovek prave chysta.
	const only = $derived(
		lists.filter((one) => items.some((item) => item.listID === one.id)).length === 1
			? lists.find((one) => items.some((item) => item.listID === one.id))
			: undefined
	);

	const buyerOf = $derived((userID: string) => {
		const index = members.findIndex((member) => member.id === userID);

		return index < 0 ? undefined : { member: members[index], index };
	});
</script>

<svelte:head>
	<title>{m.app_home_page_title()}</title>
</svelte:head>

{#snippet opens(label: string)}
	<span
		aria-hidden="true"
		class="pointer-events-none absolute inset-0 flex items-center justify-center bg-card/75 opacity-0 backdrop-blur-[1px] transition-opacity duration-200 group-hover:opacity-100 group-focus-visible:opacity-100"
	>
		<span
			class="inline-flex translate-y-1 items-center gap-2 rounded-lg bg-tn-primary px-4 py-2 font-bold text-primary-foreground shadow-float transition-transform duration-200 group-hover:translate-y-0 group-focus-visible:translate-y-0"
		>
			{label}
			<ArrowRightIcon class="size-4 flex-none" />
		</span>
	</span>
{/snippet}

<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<div class="flex items-start justify-between gap-3 px-1.5">
			<div class="flex min-w-0 flex-col gap-1">
				<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
					{data.user.givenName
						? m.app_home_greeting({ name: data.user.givenName })
						: m.app_home_greeting_plain()}
				</h1>
				<p class="text-sm text-tn-meta">
					{data.household?.name}{items.length ? ` · ${listCount(items.length)}` : ''}
				</p>
			</div>

			<!--
				Pridat vec je to, kvoli comu sa appka otvara najcastejsie, takze stoji pri nadpise
				na kazdej sirke - rovnako ako na zozname. Na uzkom displeji ostava stvorcove 44px
				tlacidlo bez popisu, aby sa vedla 30px nadpisu zmestilo.
			-->
			<button
				type="button"
				onclick={() => (dialogOpen = true)}
				aria-label={m.item_add()}
				class="inline-flex size-11 flex-none cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-tn-primary font-bold text-primary-foreground transition hover:bg-tn-primary-hover sm:size-auto sm:h-11 sm:px-4 lg:h-[38px]"
			>
				<PlusIcon class="size-4 flex-none" />
				<span class="hidden sm:inline">{m.item_add()}</span>
			</button>
		</div>

		<!-- Bez poloziek nie je co odskrtavat, tak rezim nakupu ani neponukam. -->
		{#if only}
			<a
				href="/app/shop/{only.id}"
				class="inline-flex h-[50px] cursor-pointer items-center justify-center gap-2 rounded-lg bg-tn-tint px-5 font-bold text-tn-primary-strong transition hover:brightness-95 sm:self-start"
			>
				<ShoppingCartIcon class="size-4 flex-none" />
				{m.shop_start()}
			</a>
		{/if}

		{#each lists as one (one.id)}
			{@const own = previewOf(one.id)}

			<a
				href="/app/list/{one.id}"
				aria-label={m.app_home_open_list()}
				class="group relative flex flex-col gap-3 overflow-hidden rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs transition duration-200 hover:-translate-y-0.5 hover:shadow-float focus-visible:-translate-y-0.5 focus-visible:shadow-float focus-visible:outline-none active:translate-y-0"
			>
				<div class="flex items-center justify-between gap-3">
					<h2 class="flex min-w-0 items-center gap-2 text-[17px] font-bold">
						<span class="size-2.5 flex-none rounded-full {listDot(one.color)}"></span>
						<span class="truncate">{one.name}</span>
					</h2>

					<span class="flex-none text-[13px] font-bold text-tn-primary-strong">
						{m.app_home_open_list()}
					</span>
				</div>

				{#if one.note}
					<p class="text-[13px] leading-snug text-tn-meta">{one.note}</p>
				{/if}

				{#if own.preview.length}
					<ul class="flex flex-col">
						{#each own.preview as item (item.id)}
							<li
								class="flex items-center gap-3 border-t border-tn-muted/40 py-2.5 first:border-t-0"
							>
								<span class="min-w-0 flex-1 font-bold">{item.name}</span>

								{#if item.quantity}
									<span class="flex-none text-[13px] text-tn-faint">{item.quantity}</span>
								{/if}
							</li>
						{/each}
					</ul>

					{#if own.rest > 0}
						<p class="text-[13px] text-tn-meta">{listMore(own.rest)}</p>
					{/if}
				{:else}
					<p class="text-[15px] text-muted-foreground">{m.app_home_list_empty()}</p>
				{/if}

				{@render opens(m.app_home_open_list())}
			</a>
		{/each}

		<a
			href="/app/history"
			aria-label={m.app_home_open_trips()}
			class="group relative flex flex-col gap-3 overflow-hidden rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs transition duration-200 hover:-translate-y-0.5 hover:shadow-float focus-visible:-translate-y-0.5 focus-visible:shadow-float focus-visible:outline-none active:translate-y-0"
		>
			<div class="flex items-center justify-between gap-3">
				<h2 class="text-[17px] font-bold">{m.app_home_trips()}</h2>

				<span class="text-[13px] font-bold text-tn-primary-strong">
					{m.app_home_trips_all()}
				</span>
			</div>

			{#each trips as trip (trip.id)}
				{@const buyer = buyerOf(trip.completedByUserID)}
				{@const date = tripDate(trip.completedAt)}

				<div
					class="flex items-center gap-3 border-t border-tn-muted/40 pt-3 first:border-t-0 first:pt-0"
				>
					{#if buyer}
						<MemberAvatar member={buyer.member} index={buyer.index} class="size-8 text-[11px]" />
					{/if}

					<div class="flex min-w-0 flex-col">
						<p class="font-bold">
							<span class="sm:hidden">{date.short}</span>
							<span class="hidden sm:inline">{date.long}</span>
						</p>
						<p class="text-[13px] text-tn-meta">
							{#if buyer}
								{m.history_by({ name: buyer.member.givenName ?? buyer.member.name ?? '' })} ·
							{/if}
							{tripItemCount(trip.items.length)}
						</p>
					</div>

					{#if trip.totalCost != null}
						<span class="ml-auto flex-none font-bold text-tn-primary-strong">
							{money(trip.totalCost)}
						</span>
					{/if}
				</div>
			{:else}
				<p class="text-[15px] text-muted-foreground">{m.app_home_trips_empty()}</p>
			{/each}

			{@render opens(m.app_home_open_trips())}
		</a>

		<a
			href="/app/household"
			aria-label={m.app_home_open_household()}
			class="group relative flex flex-col gap-3 overflow-hidden rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs transition duration-200 hover:-translate-y-0.5 hover:shadow-float focus-visible:-translate-y-0.5 focus-visible:shadow-float focus-visible:outline-none active:translate-y-0"
		>
			<div class="flex items-center justify-between gap-3">
				<h2 class="text-[17px] font-bold">{m.app_home_household_open()}</h2>

				<span class="text-[13px] font-bold text-tn-primary-strong">
					{members.length === 1 ? m.app_home_household_invite() : m.app_home_household_manage()}
				</span>
			</div>

			<div class="flex items-center gap-3">
				<div class="flex">
					{#each members as member, index (member.id)}
						<MemberAvatar
							{member}
							{index}
							class="size-8 text-[11px] {index > 0 ? '-ml-2.5' : ''}"
						/>
					{/each}
				</div>

				<p class="min-w-0 truncate text-[13px] text-tn-meta">
					{members.map((member) => member.givenName ?? member.name ?? '').join(' + ')}
				</p>
			</div>

			{@render opens(m.app_home_open_household())}
		</a>
	</div>
</main>

<ItemDialog bind:open={dialogOpen} {lists} categories={data.household?.categories} />
