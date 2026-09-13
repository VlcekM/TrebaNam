<script lang="ts">
	import { categoryLabel } from '$lib/categories';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import DeleteTripDialog from '$lib/components/DeleteTripDialog.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import TripTotalDialog from '$lib/components/TripTotalDialog.svelte';
	import MonthPicker from '$lib/components/MonthPicker.svelte';
	import { tripCount, tripItemCount, withoutTotalCount } from '$lib/counts';
	import { tripDate } from '$lib/dates';
	import { listDot } from '$lib/lists';
	import { money } from '$lib/money';
	import { monthOf, totals, tripsInMonth, type MonthKey } from '$lib/spending';
	import { m } from '$lib/paraglide/messages.js';
	import type { ShoppingRecord } from '$lib/types';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Filter podla mesiaca: bez neho su tu vsetky nakupy, s nim jeden mesiac a jeho sucet -
	// to je najlacnejsia odpoved na "kolko sme minuli v auguste". Neukladame ho do adresy,
	// lebo zalozka na domacej obrazovke ma otvarat vsetko.
	let month = $state<MonthKey | undefined>();
	const thisMonth = monthOf(new Date());

	const records = $derived(month ? tripsInMonth(data.records, month) : data.records);
	const sum = $derived(totals(records));
	const members = $derived(data.household?.members ?? []);
	const categories = $derived(data.household?.categories ?? []);

	// Suma je jedina vec, ktora sa na ukoncenom nakupe meni - uctenka sa najde aj neskor.
	let totalOf = $state<ShoppingRecord | undefined>();
	let totalOpen = $state(false);

	function editTotal(record: ShoppingRecord) {
		totalOf = record;
		totalOpen = true;
	}

	let deleteOf = $state<ShoppingRecord | undefined>();
	let deleteOpen = $state(false);

	function askDelete(record: ShoppingRecord) {
		deleteOf = record;
		deleteOpen = true;
	}

	const buyerOf = $derived((userID: string) => {
		const index = members.findIndex((member) => member.id === userID);

		return index < 0 ? undefined : { member: members[index], index };
	});
</script>

<svelte:head>
	<title>{m.history_page_title()}</title>
</svelte:head>

<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<div class="flex flex-col gap-1 px-1.5">
			<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
				{m.history_title()}
			</h1>
			<p class="text-sm text-tn-meta">{data.household?.name}</p>
		</div>

		{#if data.records.length > 0}
			<div class="flex flex-col gap-3 px-1.5 sm:flex-row sm:items-center">
				<div class="flex items-center gap-2">
					<button
						type="button"
						onclick={() => (month = undefined)}
						aria-pressed={!month}
						class="inline-flex h-9 cursor-pointer items-center rounded-lg px-3 text-[13.5px] font-bold whitespace-nowrap transition {month
							? 'text-tn-meta hover:bg-muted'
							: 'bg-tn-accent'}"
					>
						{m.history_all_months()}
					</button>
					{#if !month}
						<button
							type="button"
							onclick={() => (month = monthOf(data.records[0].completedAt))}
							class="inline-flex h-9 cursor-pointer items-center rounded-lg px-3 text-[13.5px] font-bold whitespace-nowrap text-tn-meta transition hover:bg-muted"
						>
							{m.history_filter_month()}
						</button>
					{/if}
				</div>
				{#if month}
					<MonthPicker bind:month disabledNext={month >= thisMonth} class="flex-1 sm:max-w-xs" />
				{/if}
			</div>

			<!-- Sucet toho, co je prave vidno: za mesiac alebo za vsetko, nakupy bez sumy zvlast. -->
			<section
				class="flex flex-wrap items-baseline gap-x-3 gap-y-1 rounded-2xl bg-tn-tint px-6 py-4"
			>
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.history_total()}
				</span>
				<span class="text-[22px] leading-none font-bold tracking-tight text-tn-primary-strong">
					{money(sum.spent)}
				</span>
				<span class="text-[13px] text-tn-meta">
					{tripCount(sum.trips)}{sum.withoutTotal > 0
						? ` · ${withoutTotalCount(sum.withoutTotal)}`
						: ''}
				</span>
			</section>
		{/if}

		{#each records as record (record.id)}
			{@const buyer = buyerOf(record.completedByUserID)}
			{@const date = tripDate(record.completedAt)}

			<section
				class="flex flex-col gap-3 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs"
			>
				<!--
					Na uzkom displeji sa suma a kos nezmestia vedla datumu a meta riadku; tak sa
					zalomia pod ne na vlastny riadok, namiesto toho, aby text stlacili do stlpca
					jednoslovnych riadkov. Meta riadok je obycajny text, ktory sa lame po slovach.
				-->
				<div class="flex flex-wrap items-center gap-x-3 gap-y-2">
					{#if buyer}
						<MemberAvatar member={buyer.member} index={buyer.index} class="size-8 text-[11px]" />
					{/if}

					<div class="flex min-w-0 flex-1 basis-40 flex-col">
						<h2 class="text-[17px] font-bold whitespace-nowrap">
							<span class="sm:hidden">{date.short}</span>
							<span class="hidden sm:inline">{date.long}</span>
						</h2>
						<p class="text-[13px] text-tn-meta">
							<!-- Z ktoreho zoznamu sa nakupovalo, tak ako sa vtedy volal - je to odpis. -->
							{#if record.listName}
								<span
									class="mr-1 inline-block size-2 rounded-full align-baseline {listDot(
										record.listColor
									)}"
								></span><span>{record.listName}</span> ·
							{/if}
							{#if buyer}
								{m.history_by({ name: buyer.member.givenName ?? buyer.member.name ?? '' })} ·
							{/if}
							{tripItemCount(record.items.length)}
						</p>
					</div>

					<div class="ml-auto flex items-center gap-1">
						<button
							type="button"
							onclick={() => editTotal(record)}
							aria-label={record.totalCost != null ? m.trip_total_edit() : m.trip_total_add()}
							class="inline-flex h-9 flex-none cursor-pointer items-center rounded-lg px-3 font-bold transition {record.totalCost !=
							null
								? 'bg-tn-tint text-tn-primary-strong'
								: 'text-[13px] text-tn-meta hover:bg-muted'}"
						>
							{record.totalCost != null ? money(record.totalCost) : m.trip_total_add()}
						</button>

						<button
							type="button"
							onclick={() => askDelete(record)}
							aria-label={m.trip_delete()}
							title={m.trip_delete()}
							class="inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg text-tn-meta transition hover:bg-muted hover:text-destructive"
						>
							<Trash2Icon class="size-4" />
						</button>
					</div>
				</div>

				<ul class="flex flex-col">
					{#each record.items as item (item.id)}
						<li class="flex items-center gap-3 border-t border-tn-muted/40 py-2.5 first:border-t-0">
							<span class="min-w-0 flex-1 font-bold">{item.name}</span>

							{#if item.quantity}
								<span class="text-[13px] text-tn-faint">{item.quantity}</span>
							{/if}

							<span class="text-[13px] text-tn-faint">
								{categoryLabel(item.category, categories)}
							</span>
						</li>
					{/each}
				</ul>
			</section>
		{:else}
			<section
				class="rounded-2xl border border-tn-muted/40 bg-card px-6 py-12 text-center shadow-xs"
			>
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.history_empty()}</p>
			</section>
		{/each}
	</div>
</main>

<TripTotalDialog bind:open={totalOpen} record={totalOf} />
<DeleteTripDialog bind:open={deleteOpen} record={deleteOf} />
