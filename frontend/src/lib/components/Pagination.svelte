<script lang="ts">
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
</script>

<nav class="pagination" aria-label="Seiten">
	<button disabled={page <= 1} onclick={() => onNavigate(page - 1)} aria-label="Vorherige Seite">
		‹
	</button>
	<span>Seite {page} / {Math.max(totalPages, 1)}</span>
	<button disabled={!hasNext} onclick={() => onNavigate(page + 1)} aria-label="Nächste Seite">
		›
	</button>
</nav>

<style>
	.pagination {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 18px;
		padding: 10px 16px calc(10px + var(--safe-bottom));
	}

	button {
		width: 40px;
		height: 40px;
		border-radius: 50%;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		font-size: 1.2rem;
		box-shadow: var(--shadow-soft);
	}

	button:disabled {
		opacity: 0.35;
		box-shadow: none;
	}

	span {
		min-width: 110px;
		text-align: center;
		font-variant-numeric: tabular-nums;
		color: var(--color-text-muted);
		font-size: 0.9rem;
	}
</style>
