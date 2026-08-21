<script lang="ts">
	import { fade, scale } from 'svelte/transition';

	export interface UploadItem {
		name: string;
		status: 'pending' | 'uploading' | 'done' | 'error';
		error?: string;
	}

	let {
		items,
		finished,
		onClose
	}: {
		items: UploadItem[];
		finished: boolean;
		onClose: () => void;
	} = $props();

	const doneCount = $derived(
		items.filter((i) => i.status === 'done' || i.status === 'error').length
	);
	const errorCount = $derived(items.filter((i) => i.status === 'error').length);
</script>

<div class="backdrop" transition:fade={{ duration: 180 }}>
	<div class="panel" transition:scale={{ start: 0.92, duration: 240 }}>
		<h2>{finished ? 'Fertig' : 'Bilder werden hochgeladen…'}</h2>
		<p class="progress-text">{doneCount} / {items.length}</p>

		<ul>
			{#each items as item (item.name)}
				<li class={item.status}>
					<span class="name">{item.name}</span>
					<span class="status">
						{#if item.status === 'pending' || item.status === 'uploading'}
							<span class="spinner"></span>
						{:else if item.status === 'done'}
							✓
						{:else}
							✕
						{/if}
					</span>
				</li>
				{#if item.status === 'error' && item.error}
					<p class="error-detail">{item.error}</p>
				{/if}
			{/each}
		</ul>

		{#if errorCount > 0 && finished}
			<p class="summary-error">{errorCount} von {items.length} Bildern konnten nicht hochgeladen werden.</p>
		{/if}

		{#if finished}
			<button class="close-btn" onclick={onClose} transition:fade>Schließen</button>
		{/if}
	</div>
</div>

<style>
	.backdrop {
		position: fixed;
		inset: 0;
		background: rgba(20, 16, 12, 0.55);
		z-index: 50;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 20px;
	}

	.panel {
		width: 100%;
		max-width: 360px;
		max-height: 80dvh;
		display: flex;
		flex-direction: column;
		background: var(--color-surface);
		border-radius: var(--radius-l);
		padding: 24px;
		box-shadow: var(--shadow-lift);
	}

	h2 {
		margin: 0 0 4px;
		font-size: 1.1rem;
		text-align: center;
	}

	.progress-text {
		margin: 0 0 16px;
		text-align: center;
		color: var(--color-text-muted);
		font-variant-numeric: tabular-nums;
	}

	ul {
		list-style: none;
		margin: 0 0 8px;
		padding: 0;
		overflow-y: auto;
	}

	li {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 10px;
		padding: 8px 0;
		border-bottom: 1px solid var(--color-border);
		font-size: 0.9rem;
	}

	li:last-of-type {
		border-bottom: none;
	}

	.name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.status {
		flex-shrink: 0;
		width: 22px;
		text-align: center;
	}

	li.done .status {
		color: #3f8f5f;
	}

	li.error .status {
		color: var(--color-danger);
	}

	.error-detail {
		margin: -4px 0 6px;
		font-size: 0.78rem;
		color: var(--color-danger);
	}

	.summary-error {
		margin: 4px 0 12px;
		font-size: 0.85rem;
		color: var(--color-danger);
		text-align: center;
	}

	.spinner {
		display: inline-block;
		width: 14px;
		height: 14px;
		border: 2px solid var(--color-border);
		border-top-color: var(--color-accent);
		border-radius: 50%;
		animation: spin 0.7s linear infinite;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}

	.close-btn {
		margin-top: 8px;
		width: 100%;
		padding: 14px;
		border: none;
		border-radius: var(--radius-m);
		background: var(--color-accent);
		color: var(--color-accent-contrast);
		font-weight: 600;
	}
</style>
