<script lang="ts">
	import { goto, invalidate, pushState, replaceState } from '$app/navigation';
	import { page as pageState } from '$app/state';
	import { fade } from 'svelte/transition';
	import { imageClient, apiErrorMessage, displayUrl } from '$lib/api';
	import type { PagedResultOfGalleryDto } from '$lib/api/client';
	import UploadSheet from '$lib/components/UploadSheet.svelte';
	import UploadProgress, { type UploadItem } from '$lib/components/UploadProgress.svelte';
	import ImageViewer from '$lib/components/ImageViewer.svelte';
	import Pagination from '$lib/components/Pagination.svelte';
	import Icon from '$lib/components/Icon.svelte';

	let {
		tisch,
		page,
		gallery
	}: {
		/** Tischcode aus der Route, oder null in der Galerie über alle Tische. */
		tisch: string | null;
		page: number;
		gallery: PagedResultOfGalleryDto;
	} = $props();

	const items = $derived(gallery.items ?? []);
	const urls = $derived(items.map((i) => displayUrl(i.thumbnailUrl)));

	// tableNumber ist null, wenn der gallery-Endpunkt alle Tische liefert.
	const tableNumber = $derived(gallery.tableNumber ?? null);
	const title = $derived(tableNumber === null ? 'Alle Fotos' : `Fotos von Tisch ${tableNumber}`);

	// Hochladen geht nur in eine Tisch-Galerie - über alle Tische fehlt das Ziel.
	const canUpload = $derived(tisch !== null);

	// Der Viewer hängt an der History (Shallow Routing), damit die Zurück-Geste
	// des Handys ihn schließt statt die Galerie-Seite zu verlassen.
	const viewerIndex = $derived(
		typeof pageState.state.viewer === 'number' && pageState.state.viewer < urls.length
			? pageState.state.viewer
			: null
	);

	let sheetOpen = $state(false);
	let uploadItems = $state<UploadItem[]>([]);
	let uploadActive = $state(false);
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
		const table = tisch;
		if (!table) return;

		if (truncated) {
			showToast('Es können maximal 10 Bilder gleichzeitig ausgewählt werden.');
		}

		uploadItems = files.map((f) => ({ name: f.name, status: 'pending' as const }));
		uploadActive = true;

		await Promise.all(
			files.map(async (file, i) => {
				uploadItems[i] = { ...uploadItems[i], status: 'uploading' };
				try {
					await imageClient.upload(table, { data: file, fileName: file.name });
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
		const newTotalPages = gallery.totalPages ?? 1;
		if (page < newTotalPages) {
			await goto(`?page=${newTotalPages}`, { replaceState: true });
		}
	}

	function goToPage(target: number) {
		goto(`?page=${target}`);
	}

	// pushState legt einen History-Eintrag ohne URL-Wechsel an: Zurück landet
	// wieder in der Galerie, und zwar auf derselben Seite wie vorher.
	function openViewer(index: number) {
		pushState('', { viewer: index });
	}

	// Beim Blättern im Viewer nur ersetzen, sonst müsste man sich durch jedes
	// angesehene Bild einzeln zurückklicken.
	function navigateViewer(index: number) {
		replaceState('', { viewer: index });
	}

	function closeViewer() {
		history.back();
	}
</script>

<svelte:head>
	<title>{title}</title>
</svelte:head>

<div class="page">
	<header>
		<h1>{title}</h1>
	</header>

	<main>
		{#if items.length === 0}
			<div class="empty" in:fade={{ duration: 200 }}>
				<span class="emoji">📷</span>
				{#if canUpload}
					<p>Noch keine Fotos an diesem Tisch.</p>
					<p class="hint">Sei die/der Erste und lade ein Bild hoch!</p>
				{:else}
					<p>Noch keine Fotos vorhanden.</p>
					<p class="hint">Sobald die Gäste hochladen, erscheinen sie hier.</p>
				{/if}
			</div>
		{:else}
			<div class="grid" in:fade={{ duration: 200 }}>
				{#each items as item, i (item.id)}
					<button class="thumb" onclick={() => openViewer(i)} aria-label="Bild vergrößern">
						<img src={item.thumbnailUrl} alt="" loading="lazy" />
					</button>
				{/each}
			</div>

			<Pagination
				{page}
				totalPages={gallery.totalPages ?? 1}
				hasNext={gallery.hasNext ?? false}
				onNavigate={goToPage}
			/>
		{/if}
	</main>

	{#if canUpload}
		<button class="fab" onclick={() => (sheetOpen = true)} aria-label="Bilder hochladen">
			<Icon name="plus" size={28} />
		</button>
	{/if}
</div>

{#if canUpload}
	<UploadSheet bind:open={sheetOpen} max={10} onSelect={handleSelect} />
{/if}

{#if uploadActive}
	<UploadProgress items={uploadItems} finished={uploadFinished} onClose={closeUploadProgress} />
{/if}

{#if viewerIndex !== null}
	<ImageViewer {urls} index={viewerIndex} onClose={closeViewer} onNavigate={navigateViewer} />
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
		padding: calc(14px + var(--safe-top)) calc(20px + var(--safe-right)) 14px
			calc(20px + var(--safe-left));
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
		/* Der untere Abstand hält den fixierten Upload-Button frei (60px hoch,
		   24px vom Rand) plus Reserve: blendet ein mobiler Browser beim Scrollen
		   seine Leiste aus, wandert der Button gegenüber dem Seiteninhalt kurz
		   nach oben. Vorher blieben nur 16px Luft, dabei schob er sich über die
		   Seitennavigation. */
		padding: 14px calc(10px + var(--safe-right)) calc(140px + var(--safe-bottom))
			calc(10px + var(--safe-left));
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
		/* max() statt Addition: der Button soll den Home-Indicator freilassen,
		   aber nicht um dessen volle Höhe nach oben springen, sobald die
		   Browserleiste beim Scrollen verschwindet. */
		bottom: max(24px, calc(var(--safe-bottom) + 6px));
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
