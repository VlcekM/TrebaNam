<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import CopyIcon from '@lucide/svelte/icons/copy';
	import CheckIcon from '@lucide/svelte/icons/check';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import { createHousehold, inviteLink } from '$lib/households';
	import { userStore } from '$lib/stores/user';
	import { m } from '$lib/paraglide/messages.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Domacnost nesie layout, lebo z nej zije aj bocny panel.
	const household = $derived(data.household);
	let name = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let copied = $state(false);

	const memberCount = $derived(household?.members.length ?? 0);

	const since = $derived(
		household
			? new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(
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
			await createHousehold(name.trim());
			// Nacitanie zopakuje aj layout, takze panel aj stranka dostanu novu domacnost naraz.
			await invalidateAll();
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
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
			<div class="flex flex-col gap-1 px-1.5">
				<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
					{household.name}
				</h1>
				<p class="text-sm text-tn-meta">
					{memberCount === 1
						? m.household_member_count_one()
						: m.household_member_count_other({ count: memberCount })} · {m.household_since({
						date: since
					})}
				</p>
			</div>

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

							<span class="font-bold">{member.givenName ?? member.name}</span>

							{#if member.id === $userStore?.id}
								<span
									class="rounded-lg bg-tn-tint px-2 py-0.5 text-xs font-bold text-tn-primary-strong"
								>
									{m.household_you()}
								</span>
							{/if}
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
