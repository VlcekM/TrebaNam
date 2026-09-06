<script lang="ts">
	import { cn } from '$lib/utils';

	// Rovnaka kresba ako favicon a PWA ikony - vozik s odskrtnutou polozkou.
	// Zaoblenie dlazdice drzime na 12/48, teda rovnaky pomer ako 16/64 vo favicone.
	let { class: className }: { class?: string } = $props();
</script>

<svg viewBox="0 0 64 64" aria-hidden="true" class={cn('size-9', className)}>
	<rect width="64" height="64" rx="16" fill="var(--tn-primary)" />
	<g transform="translate(8 8) scale(2)">
		<path
			d="M2.6 3.4h2.4l1.1 5"
			fill="none"
			stroke="var(--primary-foreground)"
			stroke-width="2.4"
			stroke-linecap="round"
			stroke-linejoin="round"
		/>
		<circle cx="10.9" cy="20.2" r="1.6" fill="var(--primary-foreground)" />
		<circle cx="18.3" cy="20.2" r="1.6" fill="var(--primary-foreground)" />
		<path
			d="M7.2 8.4h14.2l-1.9 7.7a1.7 1.7 0 0 1-1.65 1.3h-7.1a1.7 1.7 0 0 1-1.65-1.3z"
			fill="var(--primary-foreground)"
		/>
		<path
			d="m10.9 12.5 1.9 1.9 4.1-4.4"
			fill="none"
			stroke="var(--tn-primary)"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
		/>
	</g>
</svg>
