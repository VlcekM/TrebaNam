<script lang="ts">
	import { categoryLabel } from '$lib/categories';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import DeleteTripDialog from '$lib/components/DeleteTripDialog.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import TripTotalDialog from '$lib/components/TripTotalDialog.svelte';
	import { tripItemCount } from '$lib/counts';
	import { tripDate } from '$lib/dates';
	import { listDot } from '$lib/lists';
	import { money } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import type { ShoppingRecord } from '$lib/types';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const records = $derived(data.records);
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

		{#each records as record (record.id)}
			{@const buyer = buyerOf(record.completedByUserID)}
			{@const date = tripDate(record.completedAt)}

			<section
				class="flex flex-col gap-3 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs"
			>
				<div class="flex items-center gap-3">
					{#if buyer}
						<MemberAvatar member={buyer.member} index={buyer.index} class="size-8 text-[11px]" />
					{/if}

					<div class="flex min-w-0 flex-1 flex-col">
						<h2 class="text-[17px] font-bold">
							<span class="sm:hidden">{date.short}</span>
							<span class="hidden sm:inline">{date.long}</span>
						</h2>
						<p class="flex items-center gap-1.5 text-[13px] text-tn-meta">
							<!-- Z ktoreho zoznamu sa nakupovalo, tak ako sa vtedy volal - je to odpis. -->
							{#if record.listName}
								<span class="size-2 flex-none rounded-full {listDot(record.listColor)}"></span>
								<span class="truncate">{record.listName}</span> ·
							{/if}
							{#if buyer}
								{m.history_by({ name: buyer.member.givenName ?? buyer.member.name ?? '' })} ·
							{/if}
							{tripItemCount(record.items.length)}
						</p>
					</div>

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
