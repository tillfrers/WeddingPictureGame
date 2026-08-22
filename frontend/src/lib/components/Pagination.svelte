<script lang="ts">
	import Icon from './Icon.svelte';

	let {
		page,
		totalPages,
		hasNext,
		onNavigate
	}: {
		page: number;
		totalPages: number;
		hasNext: boolean;
		onNavigate: (page: number) => void;
	} = $props();

	const lastPage = $derived(Math.max(totalPages, 1));
</script>

<nav class="pagination" aria-label="Seiten">
	<button disabled={page <= 1} onclick={() => onNavigate(1)} aria-label="Erste Seite">
		<Icon name="chevron-first" size={20} />
	</button>
	<button disabled={page <= 1} onclick={() => onNavigate(page - 1)} aria-label="Vorherige Seite">
		<Icon name="chevron-left" size={20} />
	</button>
	<span>Seite {page} / {lastPage}</span>
	<button disabled={!hasNext} onclick={() => onNavigate(page + 1)} aria-label="Nächste Seite">
		<Icon name="chevron-right" size={20} />
	</button>
	<button disabled={!hasNext} onclick={() => onNavigate(lastPage)} aria-label="Letzte Seite">
		<Icon name="chevron-last" size={20} />
	</button>
</nav>

<style>
	/* Enger als bei drei Elementen: mit den Sprungtasten für erste und letzte
	   Seite müssen fünf Bedienelemente auch auf 320px breite Displays passen. */
	.pagination {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 8px;
		padding: 10px 8px;
	}

	button {
		flex: none;
		width: 40px;
		height: 40px;
		border-radius: 50%;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		color: var(--color-text);
		display: flex;
		align-items: center;
		justify-content: center;
		box-shadow: var(--shadow-soft);
	}

	button:disabled {
		opacity: 0.35;
		box-shadow: none;
	}

	span {
		/* Feste Mindestbreite, damit die Tasten beim Blättern nicht wandern. */
		min-width: 84px;
		text-align: center;
		font-variant-numeric: tabular-nums;
		color: var(--color-text-muted);
		font-size: 0.9rem;
	}
</style>
