<script lang="ts">
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';
	import { userStore } from '$lib/stores/user';
	import { m } from '$lib/paraglide/messages.js';

	// Iniciala do avatara, ked Google nevrati obrazok.
	const initial = $derived(($userStore?.givenName ?? $userStore?.name ?? '?').slice(0, 1));

	async function logout() {
		await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
		// Plny reload - stav SPA po odhlaseni nema co prezit.
		window.location.href = '/';
	}
</script>

<header
	class="sticky top-0 z-40 border-b border-tn-muted/40 bg-background/85 backdrop-blur"
	style="padding-top: env(safe-area-inset-top)"
>
	<div class="mx-auto flex max-w-2xl items-center gap-3 px-4 py-2.5">
		<a href="/app" class="wordmark">
			{m.app_header_title()}
		</a>

		<div class="ml-auto flex items-center gap-2">
			<ThemeToggle />

			<!-- Avatar je kruh s bielym prstencom, presne ako stacknuti clenovia v handoffe. -->
			{#if $userStore?.pictureUrl}
				<img
					src={$userStore.pictureUrl}
					alt=""
					referrerpolicy="no-referrer"
					class="size-10 rounded-full object-cover ring-2 ring-background"
				/>
			{:else if $userStore}
				<span
					class="inline-flex size-10 items-center justify-center rounded-full bg-tn-member-a text-sm font-bold text-white ring-2 ring-background"
				>
					{initial}
				</span>
			{/if}

			<button
				type="button"
				onclick={logout}
				title={m.app_logout()}
				aria-label={m.app_logout()}
				class="inline-flex size-10 cursor-pointer items-center justify-center rounded-lg border border-tn-muted/40 bg-card/80 shadow-xs transition hover:bg-muted"
			>
				<LogOutIcon class="size-5" />
			</button>
		</div>
	</div>
</header>
