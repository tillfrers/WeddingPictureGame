<script lang="ts">
	import { fade, scale } from 'svelte/transition';

	export interface TransferItem {
		/** `ready` = geladen, wartet noch auf das Teilen-Menü. */
		name: string;
		status: 'pending' | 'running' | 'ready' | 'done' | 'error';
		error?: string;
	}

	let {
		items,
		finished,
		heading,
		verb,
		onClose,
		closeLabel = 'Schließen',
		showClose = false,
		actionLabel,
		actionHint,
		actionBusy = false,
		onAction
	}: {
		items: TransferItem[];
		/** Alles durch - schaltet die Fehlerbilanz frei. */
		finished: boolean;
		/** Überschrift; der Aufrufer kennt die Phase, die Anzeige nicht. */
		heading: string;
		/** Partizip für die Fehlerzeile, z. B. "hochgeladen" oder "gespeichert". */
		verb: string;
		onClose: () => void;
		closeLabel?: string;
		showClose?: boolean;
		/** Optionale Haupttaste, z. B. "Bilder sichern" für das Teilen-Menü. */
		actionLabel?: string;
		actionHint?: string;
		actionBusy?: boolean;
		onAction?: () => void;
	} = $props();

	const doneCount = $derived(
		items.filter((i) => i.status === 'done' || i.status === 'error').length
	);
	const errorCount = $derived(items.filter((i) => i.status === 'error').length);
	const percent = $derived(items.length === 0 ? 0 : (doneCount / items.length) * 100);
</script>

<div class="backdrop" transition:fade={{ duration: 180 }}>
	<div class="panel" transition:scale={{ start: 0.92, duration: 240 }}>
		<h2>{heading}</h2>
		<p class="progress-text">{doneCount} / {items.length}</p>

		<!-- Bei vielen Bildern sagt der Balken schneller als die Liste, wie weit
		     es noch ist - beim Herunterladen können das dreistellig viele sein. -->
		<div
			class="bar"
			role="progressbar"
			aria-valuemin={0}
			aria-valuemax={items.length}
			aria-valuenow={doneCount}
		>
			<div class="fill" style="width: {percent}%"></div>
		</div>

		<ul>
			{#each items as item, i (i)}
				<li class={item.status}>
					<span class="name">{item.name}</span>
					<span class="status">
						{#if item.status === 'pending' || item.status === 'running'}
							<span class="spinner"></span>
						{:else if item.status === 'ready'}
							●
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
			<p class="summary-error">
				{errorCount} von {items.length} Bildern konnten nicht {verb} werden.
			</p>
		{/if}

		{#if actionLabel && onAction}
			{#if actionHint}
				<p class="action-hint">{actionHint}</p>
			{/if}
			<button class="action-btn" onclick={onAction} disabled={actionBusy} transition:fade>
				{#if actionBusy}<span class="spinner hell"></span>{/if}
				{actionLabel}
			</button>
		{/if}

		{#if showClose}
			<button class="close-btn" class:zweitrangig={!!actionLabel} onclick={onClose} transition:fade>
				{closeLabel}
			</button>
		{/if}
	</div>
</div>

<style>
	.backdrop {
		position: fixed;
		inset: 0;
		/* Ein Zug nach unten während der Übertragung würde sonst die
		   Aktualisieren-Geste auslösen und die laufenden Anfragen abbrechen. */
		touch-action: none;
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
		margin: 0 0 10px;
		text-align: center;
		color: var(--color-text-muted);
		font-variant-numeric: tabular-nums;
	}

	.bar {
		height: 4px;
		border-radius: 2px;
		background: var(--color-bg-alt);
		overflow: hidden;
		margin-bottom: 16px;
		flex: none;
	}

	.fill {
		height: 100%;
		background: var(--color-accent);
		transition: width 0.25s ease;
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

	li.ready .status {
		color: var(--color-text-muted);
		font-size: 0.7rem;
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

	.action-hint {
		margin: 4px 0 10px;
		font-size: 0.8rem;
		line-height: 1.45;
		color: var(--color-text-muted);
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

	.spinner.hell {
		border-color: rgba(28, 23, 18, 0.35);
		border-top-color: var(--color-accent-contrast);
		margin-right: 8px;
		vertical-align: -2px;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}

	.action-btn,
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

	.action-btn:disabled {
		opacity: 0.6;
	}

	/* Steht eine Haupttaste darüber, ist Schließen nur noch der Ausweg. */
	.close-btn.zweitrangig {
		background: none;
		color: var(--color-text-muted);
		padding: 10px;
	}
</style>
