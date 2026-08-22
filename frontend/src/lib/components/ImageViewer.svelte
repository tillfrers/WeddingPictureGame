<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import Icon from './Icon.svelte';

	let {
		urls,
		index,
		onClose,
		onNavigate
	}: {
		urls: string[];
		index: number;
		onClose: () => void;
		onNavigate: (newIndex: number) => void;
	} = $props();

	// Wischgesten am Bildschirmrand gehören dem Browser (Zurück-Geste auf
	// Android/iOS). Ohne diese Zone blättert der Viewer beim Zurückwischen nur
	// zum nächsten Bild, statt die Geste durchzulassen.
	const EDGE_GUARD = 28;

	const currentUrl = $derived(urls[index]);
	const hasPrev = $derived(index > 0);
	const hasNext = $derived(index < urls.length - 1);

	let touchStartX = 0;
	let swipeTracked = false;

	function handleTouchStart(e: TouchEvent) {
		const x = e.touches[0]?.clientX ?? 0;

		swipeTracked =
			e.touches.length === 1 && x > EDGE_GUARD && x < window.innerWidth - EDGE_GUARD;
		touchStartX = x;
	}

	function handleTouchEnd(e: TouchEvent) {
		if (!swipeTracked) return;
		swipeTracked = false;

		const endX = e.changedTouches[0]?.clientX ?? touchStartX;
		const dx = endX - touchStartX;
		if (Math.abs(dx) < 60) return;
		if (dx < 0 && hasNext) onNavigate(index + 1);
		if (dx > 0 && hasPrev) onNavigate(index - 1);
	}

	function handleKey(e: KeyboardEvent) {
		if (e.key === 'Escape') onClose();
		else if (e.key === 'ArrowRight' && hasNext) onNavigate(index + 1);
		else if (e.key === 'ArrowLeft' && hasPrev) onNavigate(index - 1);
	}
</script>

<svelte:window onkeydown={handleKey} />

<div
	class="viewer"
	role="dialog"
	aria-modal="true"
	aria-label="Bildansicht"
	tabindex="-1"
	transition:fade={{ duration: 200 }}
	ontouchstart={handleTouchStart}
	ontouchend={handleTouchEnd}
	ontouchcancel={() => (swipeTracked = false)}
>
	<button class="close" onclick={onClose} aria-label="Schließen"><Icon name="close" size={20} /></button>

	<div class="stage">
		{#key currentUrl}
			<img src={currentUrl} alt="" transition:scale={{ start: 0.94, duration: 200 }} />
		{/key}
	</div>

	{#if hasPrev}
		<button class="nav prev" onclick={() => onNavigate(index - 1)} aria-label="Vorheriges Bild">
			<Icon name="chevron-left" size={26} />
		</button>
	{/if}
	{#if hasNext}
		<button class="nav next" onclick={() => onNavigate(index + 1)} aria-label="Nächstes Bild">
			<Icon name="chevron-right" size={26} />
		</button>
	{/if}

	<div class="counter">{index + 1} / {urls.length}</div>
</div>

<style>
	.viewer {
		position: fixed;
		inset: 0;
		z-index: 60;
		background: rgba(10, 8, 6, 0.94);
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.stage {
		width: 100%;
		height: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 16px;
	}

	.stage img {
		max-width: 100%;
		max-height: 100%;
		object-fit: contain;
		border-radius: 4px;
	}

	.close {
		position: absolute;
		top: calc(16px + var(--safe-top));
		right: calc(16px + var(--safe-right));
		z-index: 61;
		width: 40px;
		height: 40px;
		border-radius: 50%;
		border: none;
		background: rgba(255, 255, 255, 0.15);
		color: #fff;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.nav {
		position: absolute;
		top: 50%;
		transform: translateY(-50%);
		width: 44px;
		height: 44px;
		border-radius: 50%;
		border: none;
		background: rgba(255, 255, 255, 0.15);
		color: #fff;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.nav.prev {
		left: calc(10px + var(--safe-left));
	}

	.nav.next {
		right: calc(10px + var(--safe-right));
	}

	.counter {
		position: absolute;
		bottom: calc(18px + var(--safe-bottom));
		left: 50%;
		transform: translateX(-50%);
		color: rgba(255, 255, 255, 0.75);
		font-size: 0.8rem;
		font-variant-numeric: tabular-nums;
	}
</style>
