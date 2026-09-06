<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { flip } from 'svelte/animate';
	import CopyIcon from '@lucide/svelte/icons/copy';
	import CheckIcon from '@lucide/svelte/icons/check';
	import ChevronDownIcon from '@lucide/svelte/icons/chevron-down';
	import ChevronUpIcon from '@lucide/svelte/icons/chevron-up';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import UserMinusIcon from '@lucide/svelte/icons/user-minus';
	import { categoryName } from '$lib/categories';
	import { memberCount as memberCountText } from '$lib/counts';
	import CategoryDialog from '$lib/components/CategoryDialog.svelte';
	import ConfirmDialog from '$lib/components/ConfirmDialog.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import {
		createHousehold,
		inviteLink,
		leaveHousehold,
		removeMember,
		renameHousehold,
		setCategoryOrder
	} from '$lib/households';
	import { formatLocale } from '$lib/locale';
	import { ms } from '$lib/motion';
	import { userStore } from '$lib/stores/user';
	import { m } from '$lib/paraglide/messages.js';
	import type { Category, HouseholdMember } from '$lib/types';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Domacnost nesie layout, lebo z nej zije aj bocny panel.
	const household = $derived(data.household);
	let name = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let copied = $state(false);

	// Premenovanie je jedno pole, tak sa deje na mieste nazvu a nie v dialogu.
	let renaming = $state(false);
	let newName = $state('');

	// Poradie skupin prekreslime hned a zapis posleme popri tom - inak by kazde klepnutie
	// cakalo na server a preskladat zoznam by trvalo dlhsie nez prejst obchod.
	let movedOrder = $state<Category[] | undefined>();

	// Skupinu zaklada aj premenuva ten isty dialog; bez skupiny je to nova.
	let editingCategory = $state<Category | undefined>();
	let categoryOpen = $state(false);

	let leaveOpen = $state(false);
	let removing = $state<HouseholdMember | undefined>();
	let removeOpen = $state(false);

	const memberCount = $derived(household?.members.length ?? 0);
	const order = $derived(movedOrder ?? household?.categories ?? []);

	const since = $derived(
		household
			? new Intl.DateTimeFormat(formatLocale(), { month: 'long', year: 'numeric' }).format(
					new Date(household.createdAt)
				)
			: ''
	);

	const link = $derived(household ? inviteLink(household.inviteCode) : '');

	async function submit(event: SubmitEvent) {
		event.preventDefault();

		if (pending || !name.trim()) return;

		pending = true;
		failed = false;

		try {
			// Prvy zoznam sa vola tak, ako sa zoznam vola v jazyku toho, kto domacnost zaklada.
			await createHousehold(name.trim(), m.list_title());
			// Nacitanie zopakuje aj layout, takze panel aj stranka dostanu novu domacnost naraz.
			await invalidateAll();
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
	}

	function startRename() {
		newName = household?.name ?? '';
		renaming = true;
	}

	async function saveName(event: SubmitEvent) {
		event.preventDefault();

		if (pending || !newName.trim()) return;

		pending = true;
		failed = false;

		try {
			await renameHousehold(newName.trim());
			await invalidateAll();
			renaming = false;
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
	}

	async function move(index: number, delta: number) {
		const next = [...order];

		[next[index], next[index + delta]] = [next[index + delta], next[index]];
		movedOrder = next;
		failed = false;

		try {
			await setCategoryOrder(next.map((category) => category.id));
			await invalidateAll();
			// Od tejto chvile plati poradie zo servera - inak by tu ostalo aj vtedy, keby ho
			// druhy clovek medzitym prestavil inak.
			movedOrder = undefined;
		} catch {
			movedOrder = undefined;
			failed = true;
		}
	}

	function openCategory(category?: Category) {
		editingCategory = category;
		categoryOpen = true;
	}

	function askRemove(member: HouseholdMember) {
		removing = member;
		removeOpen = true;
	}

	async function copyLink() {
		try {
			await navigator.clipboard.writeText(link);
			copied = true;
			setTimeout(() => (copied = false), 2000);
		} catch {
			// Bez schranky (starsi prehliadac, odopreta permission) ostava odkaz na obrazovke
			// a da sa oznacit rukou, takze tu nie je co hlasit.
		}
	}
</script>

<svelte:head>
	<title>{m.household_page_title()}</title>
</svelte:head>

<!-- Na sirokom displeji sedi obsah hned vedla panela, tak ako v handoffe. -->
<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		{#if household}
			{#if renaming}
				<form onsubmit={saveName} class="flex flex-col gap-3 px-1.5">
					<input
						bind:value={newName}
						required
						maxlength="80"
						aria-label={m.household_name_label()}
						class="h-12 rounded-lg border border-input bg-background px-3 text-[20px] font-bold outline-none focus-visible:border-tn-primary"
					/>

					<div class="flex items-center gap-2">
						<button
							type="submit"
							disabled={pending || !newName.trim()}
							class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
						>
							{pending ? m.item_save_pending() : m.item_save()}
						</button>

						<button
							type="button"
							onclick={() => (renaming = false)}
							class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
						>
							{m.item_cancel()}
						</button>
					</div>
				</form>
			{:else}
				<div class="flex items-start justify-between gap-3 px-1.5">
					<div class="flex min-w-0 flex-col gap-1">
						<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
							{household.name}
						</h1>
						<p class="text-sm text-tn-meta">
							{memberCountText(memberCount)} · {m.household_since({ date: since })}
						</p>
					</div>

					<button
						type="button"
						onclick={startRename}
						aria-label={m.household_rename()}
						class="inline-flex size-11 flex-none cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-tn-tint font-bold text-tn-primary-strong transition hover:brightness-95 sm:size-auto sm:h-11 sm:px-4 lg:h-[38px]"
					>
						<PencilIcon class="size-4 flex-none" />
						<span class="hidden sm:inline">{m.household_rename()}</span>
					</button>
				</div>
			{/if}

			{#if failed}
				<p class="px-1.5 text-sm font-semibold text-destructive">{m.error_generic()}</p>
			{/if}

			<section class="rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs">
				<h2 class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.household_members_heading()}
				</h2>

				<ul class="mt-3 flex flex-col">
					{#each household.members as member, index (member.id)}
						<li
							class="flex items-center gap-3 border-t border-tn-muted/40 py-3 first:border-t-0 first:pt-0"
						>
							<MemberAvatar {member} {index} />

							<span class="min-w-0 flex-1 truncate font-bold">
								{member.givenName ?? member.name}
							</span>

							{#if member.id === $userStore?.id}
								<span
									class="flex-none rounded-lg bg-tn-tint px-2 py-0.5 text-xs font-bold text-tn-primary-strong"
								>
									{m.household_you()}
								</span>
							{:else}
								<!-- Vyhodit vie ktokolvek z domacnosti; nikto v nej nie je nad ostatnymi. -->
								<button
									type="button"
									onclick={() => askRemove(member)}
									aria-label={m.household_remove()}
									class="inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg text-tn-meta transition hover:bg-muted hover:text-destructive"
								>
									<UserMinusIcon class="size-4" />
								</button>
							{/if}
						</li>
					{/each}
				</ul>
			</section>

			<!--
				Poradie skupin je poradie oddeleni v obchode, do ktoreho chodite - preto ho drzi
				domacnost a nie appka. Sipky namiesto tahania: na mobile sa tahat neda spolahlivo
				a s klavesnicou uz vobec.
			-->
			<section class="rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs">
				<div class="flex items-center justify-between gap-3">
					<h2 class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{m.household_order_heading()}
					</h2>

					<button
						type="button"
						onclick={() => openCategory()}
						aria-label={m.category_new()}
						title={m.category_new()}
						class="inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg text-tn-meta transition hover:bg-muted"
					>
						<PlusIcon class="size-4" />
					</button>
				</div>

				<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
					{m.household_order_body()}
				</p>

				<ul class="mt-3 flex flex-col">
					{#each order as category, index (category.id)}
						{@const label = categoryName(category)}

						<li
							class="flex items-center gap-3 border-t border-tn-muted/40 py-2 first:border-t-0 first:pt-0"
							animate:flip={{ duration: ms(220) }}
						>
							<span class="w-6 flex-none text-[13px] font-bold text-tn-faint">{index + 1}</span>

							<!-- Nazov je zaroven tlacidlom: premenovat aj zrusit skupinu sa da odtialto. -->
							<button
								type="button"
								onclick={() => openCategory(category)}
								class="min-w-0 flex-1 cursor-pointer truncate text-left font-bold"
							>
								{label}
							</button>

							<button
								type="button"
								onclick={() => move(index, -1)}
								disabled={index === 0}
								aria-label={m.household_order_up({ name: label })}
								class="inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg text-tn-meta transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-30"
							>
								<ChevronUpIcon class="size-4" />
							</button>

							<button
								type="button"
								onclick={() => move(index, 1)}
								disabled={index === order.length - 1}
								aria-label={m.household_order_down({ name: label })}
								class="inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg text-tn-meta transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-30"
							>
								<ChevronDownIcon class="size-4" />
							</button>
						</li>
					{/each}
				</ul>
			</section>

			<section class="flex flex-col gap-3 rounded-2xl bg-tn-tint p-6">
				<div class="flex flex-col gap-1">
					<h2 class="text-[17px] font-bold text-tn-primary-strong">
						{m.household_invite_heading()}
					</h2>
					<p class="text-sm leading-relaxed text-muted-foreground">{m.household_invite_body()}</p>
				</div>

				<p
					class="rounded-lg bg-card px-3 py-2.5 font-mono text-[13px] break-all text-muted-foreground"
				>
					{link}
				</p>

				<div class="flex flex-wrap items-center gap-3">
					<button
						type="button"
						onclick={copyLink}
						class="inline-flex h-11 cursor-pointer items-center gap-2 rounded-lg bg-tn-primary px-4 font-bold text-primary-foreground transition hover:bg-tn-primary-hover"
					>
						{#if copied}
							<CheckIcon class="size-4" />
							{m.household_invite_copied()}
						{:else}
							<CopyIcon class="size-4" />
							{m.household_invite_copy()}
						{/if}
					</button>

					<span class="font-mono text-sm font-bold text-tn-primary-strong">
						{m.household_invite_code({ code: household.inviteCode })}
					</span>
				</div>
			</section>

			<!-- Odchod stoji az uplne dole a mimo kariet, aby sa ho nikto neobtrel omylom. -->
			<button
				type="button"
				onclick={() => (leaveOpen = true)}
				class="mx-1.5 inline-flex h-11 cursor-pointer items-center justify-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted"
			>
				{m.household_leave()}
			</button>
		{:else}
			<div class="flex flex-col gap-1 px-1.5">
				<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
					{m.household_none_title()}
				</h1>
				<p class="text-sm text-tn-meta">{m.household_none_body()}</p>
			</div>

			<form
				onsubmit={submit}
				class="flex flex-col gap-4 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs"
			>
				<label class="flex flex-col gap-2">
					<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{m.household_name_label()}
					</span>
					<input
						bind:value={name}
						required
						maxlength="80"
						placeholder={m.household_name_placeholder()}
						class="h-12 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
					/>
				</label>

				{#if failed}
					<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
				{/if}

				<button
					type="submit"
					disabled={pending || !name.trim()}
					class="inline-flex h-[50px] cursor-pointer items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
				>
					{pending ? m.household_create_pending() : m.household_create()}
				</button>
			</form>
		{/if}
	</div>
</main>

<CategoryDialog bind:open={categoryOpen} category={editingCategory} />

<ConfirmDialog
	bind:open={leaveOpen}
	title={m.household_leave_title()}
	body={memberCount === 1 ? m.household_leave_body_last() : m.household_leave_body()}
	confirmLabel={m.household_leave_confirm()}
	pendingLabel={m.household_leave_pending()}
	confirm={async () => {
		await leaveHousehold();
		await invalidateAll();
	}}
/>

<ConfirmDialog
	bind:open={removeOpen}
	title={m.household_remove_title({ name: removing?.givenName ?? removing?.name ?? '' })}
	body={m.household_remove_body({ name: removing?.givenName ?? removing?.name ?? '' })}
	confirmLabel={m.household_remove_confirm()}
	pendingLabel={m.household_remove_pending()}
	confirm={async () => {
		if (removing) await removeMember(removing.id);
		await invalidateAll();
	}}
/>
