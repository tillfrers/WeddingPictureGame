<script lang="ts">
	import { fade, fly } from 'svelte/transition';
	import { quintOut } from 'svelte/easing';

	let {
		open = $bindable(false),
		max = 10,
		onSelect
	}: {
		open?: boolean;
		max?: number;
		onSelect: (files: File[], truncated: boolean) => void;
	} = $props();

	let cameraInput: HTMLInputElement | undefined = $state();
	let galleryInput: HTMLInputElement | undefined = $state();

	function close() {
		open = false;
	}

	function handleFiles(fileList: FileList | null) {
		if (!fileList || fileList.length === 0) return;

		const all = Array.from(fileList);
		const truncated = all.length > max;
		const files = truncated ? all.slice(0, max) : all;

		open = false;
		onSelect(files, truncated);
	}
</script>

{#if open}
	<div
		class="backdrop"
		role="presentation"
		onclick={close}
		transition:fade={{ duration: 180 }}
	></div>
	<div
		class="sheet"
		role="dialog"
		aria-modal="true"
		aria-label="Bilder hinzufügen"
		transition:fly={{ y: 260, duration: 320, easing: quintOut }}
	>
		<div class="handle"></div>
		<h2>Bilder hinzufügen</h2>
		<p class="hint">Bis zu {max} Bilder gleichzeitig auswählen</p>

		<button class="option" onclick={() => cameraInput?.click()}>
			<span class="icon">📷</span>
			<span>Foto aufnehmen</span>
		</button>
		<button class="option" onclick={() => galleryInput?.click()}>
			<span class="icon">🖼️</span>
			<span>Aus Galerie wählen</span>
		</button>
		<button class="cancel" onclick={close}>Abbrechen</button>
	</div>

	<input
		bind:this={cameraInput}
		type="file"
		accept="image/jpeg,image/png"
		capture="environment"
		multiple
		hidden
		onchange={(e) => handleFiles((e.currentTarget as HTMLInputElement).files)}
	/>
	<input
		bind:this={galleryInput}
		type="file"
		accept="image/jpeg,image/png"
		multiple
		hidden
		onchange={(e) => handleFiles((e.currentTarget as HTMLInputElement).files)}
	/>
{/if}

<style>
	.backdrop {
		position: fixed;
		inset: 0;
		background: rgba(20, 16, 12, 0.45);
		z-index: 40;
	}

	.sheet {
		position: fixed;
		left: 0;
		right: 0;
		bottom: 0;
		z-index: 41;
		background: var(--color-surface);
		border-radius: var(--radius-l) var(--radius-l) 0 0;
		padding: 12px 20px calc(24px + var(--safe-bottom));
		box-shadow: var(--shadow-lift);
	}

	.handle {
		width: 40px;
		height: 4px;
		border-radius: 2px;
		background: var(--color-border);
		margin: 4px auto 16px;
	}

	h2 {
		margin: 0 0 4px;
		font-size: 1.15rem;
		text-align: center;
	}

	.hint {
		margin: 0 0 20px;
		text-align: center;
		color: var(--color-text-muted);
		font-size: 0.85rem;
	}

	.option {
		width: 100%;
		display: flex;
		align-items: center;
		gap: 14px;
		padding: 16px 18px;
		background: var(--color-bg-alt);
		border: none;
		border-radius: var(--radius-m);
		margin-bottom: 10px;
		font-size: 1rem;
		font-weight: 600;
		text-align: left;
		transition: transform 0.15s ease;
	}

	.option:active {
		transform: scale(0.98);
	}

	.icon {
		font-size: 1.4rem;
	}

	.cancel {
		width: 100%;
		padding: 14px;
		border: none;
		background: none;
		color: var(--color-text-muted);
		font-weight: 600;
		margin-top: 4px;
	}
</style>
