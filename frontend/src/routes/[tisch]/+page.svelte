<script lang="ts">
	import { goto, invalidate } from '$app/navigation';
	import { fade } from 'svelte/transition';
	import { imageClient, apiErrorMessage } from '$lib/api';
	import UploadSheet from '$lib/components/UploadSheet.svelte';
	import UploadProgress, { type UploadItem } from '$lib/components/UploadProgress.svelte';
	import ImageViewer from '$lib/components/ImageViewer.svelte';
	import Pagination from '$lib/components/Pagination.svelte';
	import Icon from '$lib/components/Icon.svelte';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	const items = $derived(data.gallery.items ?? []);
	const ids = $derived(items.map((i) => i.id ?? ''));
	const tableNumber = $derived(data.gallery.tableNumber ?? data.tisch);

	let sheetOpen = $state(false);
	let uploadItems = $state<UploadItem[]>([]);
	let uploadActive = $state(false);
	let viewerIndex = $state<number | null>(null);
	let toast = $state('');
	let toastTimer: ReturnType<typeof setTimeout> | undefined;

	const uploadFinished = $derived(
		uploadItems.length > 0 && uploadItems.every((i) => i.status === 'done' || i.status === 'error')
	);

	function showToast(message: string) {
		toast = message;
		clearTimeout(toastTimer);
		toastTimer = setTimeout(() => (toast = ''), 4000);
	}

	async function handleSelect(files: File[], truncated: boolean) {
		if (truncated) {
			showToast('Es können maximal 10 Bilder gleichzeitig ausgewählt werden.');
		}

		uploadItems = files.map((f) => ({ name: f.name, status: 'pending' as const }));
		uploadActive = true;

		const tisch = data.tisch;

		await Promise.all(
			files.map(async (file, i) => {
				uploadItems[i] = { ...uploadItems[i], status: 'uploading' };
				try {
					await imageClient.upload(tisch, { data: file, fileName: file.name });
					uploadItems[i] = { ...uploadItems[i], status: 'done' };
				} catch (err) {
					uploadItems[i] = { ...uploadItems[i], status: 'error', error: apiErrorMessage(err) };
				}
			})
		);
	}

	async function closeUploadProgress() {
		const hadSuccess = uploadItems.some((i) => i.status === 'done');
		uploadActive = false;
		uploadItems = [];

		if (!hadSuccess) return;

		await invalidate('app:gallery');

		// Bilder werden vom Backend aufsteigend nach Datum sortiert, neue Fotos
		// landen also auf der letzten Seite - dort hin springen, damit der
		// Gast seinen Upload sofort sieht.
		const newTotalPages = data.gallery.totalPages ?? 1;
		if (data.page < newTotalPages) {
			await goto(`?page=${newTotalPages}`, { replaceState: true });
		}
	}

	function goToPage(page: number) {
		viewerIndex = null;
		goto(`?page=${page}`);
	}

	function openViewer(index: number) {
		viewerIndex = index;
	}

	function closeViewer() {
		viewerIndex = null;
	}
</script>

<svelte:head>
	<title>Fotos von Tisch {tableNumber}</title>
</svelte:head>

<div class="page">
	<header>
		<h1>Fotos von Tisch {tableNumber}</h1>
	</header>

	<main>
		{#if items.length === 0}
			<div class="empty" in:fade={{ duration: 200 }}>
				<span class="emoji">📷</span>
				<p>Noch keine Fotos an diesem Tisch.</p>
				<p class="hint">Sei die/der Erste und lade ein Bild hoch!</p>
			</div>
		{:else}
			<div class="grid" in:fade={{ duration: 200 }}>
				{#each items as item, i (item.id)}
					<button
						class="thumb"
						onclick={() => openViewer(i)}
						aria-label="Bild vergrößern"
					>
						<img src={item.thumbnailUrl} alt="" loading="lazy" />
					</button>
				{/each}
			</div>

			<Pagination
				page={data.page}
				totalPages={data.gallery.totalPages ?? 1}
				hasNext={data.gallery.hasNext ?? false}
				onNavigate={goToPage}
			/>
		{/if}
	</main>

	<button class="fab" onclick={() => (sheetOpen = true)} aria-label="Bilder hochladen">
		<Icon name="plus" size={28} />
	</button>
</div>

<UploadSheet bind:open={sheetOpen} max={10} onSelect={handleSelect} />

{#if uploadActive}
	<UploadProgress items={uploadItems} finished={uploadFinished} onClose={closeUploadProgress} />
{/if}

{#if viewerIndex !== null}
	<ImageViewer tisch={data.tisch} {ids} index={viewerIndex} onClose={closeViewer} onNavigate={openViewer} />
{/if}

{#if toast}
	<div class="toast" transition:fade={{ duration: 200 }}>{toast}</div>
{/if}

<style>
	.page {
		min-height: 100dvh;
		display: flex;
		flex-direction: column;
	}

	header {
		position: sticky;
		top: 0;
		z-index: 10;
		padding: calc(14px + var(--safe-top)) calc(20px + var(--safe-right)) 14px calc(20px + var(--safe-left));
		background: var(--color-bg);
		border-bottom: 1px solid var(--color-border);
	}

	h1 {
		margin: 0;
		font-size: 1.2rem;
		letter-spacing: 0.02em;
	}

	main {
		flex: 1;
		padding: 14px calc(10px + var(--safe-right)) 90px calc(10px + var(--safe-left));
	}

	.grid {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 6px;
	}

	.thumb {
		position: relative;
		aspect-ratio: 1;
		border: none;
		padding: 0;
		border-radius: var(--radius-s);
		overflow: hidden;
		background: var(--color-bg-alt);
		transition: transform 0.15s ease;
	}

	.thumb:active {
		transform: scale(0.95);
	}

	.thumb img {
		width: 100%;
		height: 100%;
		object-fit: cover;
		animation: fade-in 0.3s ease;
	}

	@keyframes fade-in {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}

	.empty {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		padding: 80px 24px;
		color: var(--color-text-muted);
	}

	.empty .emoji {
		font-size: 42px;
		margin-bottom: 12px;
	}

	.empty .hint {
		font-size: 0.9rem;
	}

	.fab {
		position: fixed;
		right: calc(20px + var(--safe-right));
		bottom: calc(24px + var(--safe-bottom));
		width: 60px;
		height: 60px;
		border-radius: 50%;
		border: none;
		background: var(--color-accent);
		color: var(--color-accent-contrast);
		box-shadow: var(--shadow-lift);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 20;
		transition: transform 0.15s ease;
	}

	.fab:active {
		transform: scale(0.92);
	}

	.toast {
		position: fixed;
		left: 50%;
		bottom: calc(100px + var(--safe-bottom));
		transform: translateX(-50%);
		background: rgba(20, 16, 12, 0.9);
		color: #fff;
		padding: 10px 18px;
		border-radius: 999px;
		font-size: 0.85rem;
		z-index: 70;
		max-width: calc(100vw - 40px);
		text-align: center;
	}
</style>
