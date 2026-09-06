<script lang="ts">
	import { m } from '$lib/paraglide/messages.js';
	import { cn } from '$lib/utils';

	// Nazov je slovna hracka ("treba nam"), tak ju aj ukazeme: prva polovica v akcente,
	// druha v beznom atramente. Preto dva kluce namiesto jedneho brand_name.
	let { class: className }: { class?: string } = $props();
</script>

<span class={cn('font-bold', className)}
	><span class="text-tn-primary-strong">{m.brand_name_first()}</span><span class="text-foreground"
		>{m.brand_name_second()}</span
	></span
>
