<script lang="ts">
	let {
		name,
		size = 24
	}: {
		name: 'plus' | 'chevron-left' | 'chevron-right' | 'close' | 'alert';
		size?: number;
	} = $props();
</script>

{#if name === 'plus'}
	<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
		<path d="M12 5v14M5 12h14" />
	</svg>
{:else if name === 'chevron-left'}
	<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
		<path d="M15 6l-6 6 6 6" />
	</svg>
{:else if name === 'chevron-right'}
	<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
		<path d="M9 6l6 6-6 6" />
	</svg>
{:else if name === 'close'}
	<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
		<path d="M6 6l12 12M18 6L6 18" />
	</svg>
{:else if name === 'alert'}
	<svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
		<circle cx="12" cy="12" r="9" />
		<path d="M12 7.5v6" />
		<circle cx="12" cy="16.25" r="0.9" fill="currentColor" stroke="none" />
	</svg>
{/if}
