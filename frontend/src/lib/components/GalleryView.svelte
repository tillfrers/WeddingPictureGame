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

	// Gescrollt wird der Container, nicht das Fenster (siehe app.css). SvelteKits
	// Scroll-Handling greift damit nicht mehr - beim Blättern selbst nach oben.
	let scroller: HTMLElement | undefined = $state();

	$effect(() => {
		page;
		scroller?.scrollTo({ top: 0 });
	});

	// Zum Aktualisieren nach unten ziehen. Die eingebaute Geste des Browsers
	// hängt am Wurzeldokument, und das scrollt hier bewusst nicht (sonst wandert
	// der Upload-Button, siehe app.css) - also machen wir sie selbst.
	const ZIEH_AUSLOESER = 70; // ab dieser Zugdistanz wird geladen
	const ZIEH_MAXIMUM = 110; // weiter folgt der Indikator nicht
	const ZIEH_START = 8; // darunter gilt es noch als normales Scrollen

	let ziehen = $state(0);
	let aktualisiert = $state(false);

	$effect(() => {
		const el = scroller;
		if (!el) return;

		let startY = 0;
		let aktiv = false;

		const onStart = (e: TouchEvent) => {
			if (e.touches.length !== 1) return;
			startY = e.touches[0].clientY;
			aktiv = false;
		};

		const onMove = (e: TouchEvent) => {
			if (aktualisiert || e.touches.length !== 1) return;

			const dy = e.touches[0].clientY - startY;

			// Erst ab einer Mindestdistanz übernehmen, sonst fühlt sich jedes
			// leichte Antippen beim Scrollen wie ein Zug an.
			if (!aktiv) {
				if (dy < ZIEH_START || el.scrollTop > 0) return;
				aktiv = true;
			}

			if (dy <= 0) {
				aktiv = false;
				ziehen = 0;
				return;
			}

			// Gedämpft mitlaufen lassen, damit der Zug spürbar Widerstand hat.
			ziehen = Math.min(dy * 0.5, ZIEH_MAXIMUM);
			e.preventDefault();
		};

		const onEnd = async () => {
			if (!aktiv) return;
			aktiv = false;

			const ausgeloest = ziehen >= ZIEH_AUSLOESER;
			ziehen = 0;

			if (!ausgeloest || aktualisiert) return;

			aktualisiert = true;
			try {
				// Mindestlaufzeit, sonst blitzt der Indikator bei schnellem
				// Backend nur kurz auf und die Geste wirkt wirkungslos.
				await Promise.all([invalidate('app:gallery'), new Promise((r) => setTimeout(r, 400))]);
			} finally {
				aktualisiert = false;
			}
		};

		// touchmove ausdrücklich nicht passiv, sonst lässt sich das Scrollen des
		// Containers während des Zugs nicht unterbinden.
		el.addEventListener('touchstart', onStart, { passive: true });
		el.addEventListener('touchmove', onMove, { passive: false });
		el.addEventListener('touchend', onEnd);
		el.addEventListener('touchcancel', onEnd);

		return () => {
			el.removeEventListener('touchstart', onStart);
			el.removeEventListener('touchmove', onMove);
			el.removeEventListener('touchend', onEnd);
			el.removeEventListener('touchcancel', onEnd);
		};
	});

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

		// Bilder werden vom Backend absteigend nach Datum sortiert, neue Fotos
		// stehen also ganz vorne - zurück auf Seite 1, damit der Gast seinen
		// Upload sofort sieht. Ist er schon dort, reicht Hochscrollen.
		if (page > 1) {
			await goto('?page=1', { replaceState: true });
		} else {
			scroller?.scrollTo({ top: 0 });
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

	<div class="scrollbereich">
		{#if ziehen > 0 || aktualisiert}
			<div
				class="ziehindikator"
				class:laeuft={aktualisiert}
				class:bereit={ziehen >= ZIEH_AUSLOESER}
				style="--zug: {aktualisiert ? ZIEH_AUSLOESER : ziehen}px; --dreh: {Math.round(
					ziehen * 2.5
				)}deg"
				transition:fade={{ duration: 150 }}
			>
				<Icon name="refresh" size={20} />
			</div>
		{/if}

		<main bind:this={scroller}>
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
	</div>

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
		height: 100dvh;
		display: flex;
		flex-direction: column;
	}

	/* Steht als Flex-Geschwister über dem Scroller von selbst fest - kein
	   position: sticky nötig. */
	header {
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

	/* Bezugsrahmen für den Zieh-Indikator: der muss über dem Scroller liegen,
	   ohne mit dessen Inhalt mitzuscrollen. */
	.scrollbereich {
		position: relative;
		flex: 1;
		/* Ohne min-height:0 wächst der Flex-Kasten mit dem Inhalt statt zu
		   scrollen - dann scrollt <main> nie. */
		min-height: 0;
		display: flex;
		flex-direction: column;
	}

	.ziehindikator {
		position: absolute;
		top: 0;
		left: 50%;
		z-index: 15;
		margin-left: -18px;
		/* Startet außerhalb und wandert mit dem Zug herein. */
		margin-top: -44px;
		transform: translateY(var(--zug, 0px));
		width: 36px;
		height: 36px;
		border-radius: 50%;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		box-shadow: var(--shadow-soft);
		color: var(--color-text-muted);
		display: flex;
		align-items: center;
		justify-content: center;
		pointer-events: none;
	}

	/* Ab der Auslöseschwelle: Farbe zeigt an, dass Loslassen jetzt lädt. */
	.ziehindikator.bereit,
	.ziehindikator.laeuft {
		color: var(--color-accent);
		border-color: var(--color-accent-dark);
	}

	.ziehindikator :global(svg) {
		transform: rotate(var(--dreh, 0deg));
	}

	.ziehindikator.laeuft :global(svg) {
		animation: drehen 0.8s linear infinite;
	}

	@keyframes drehen {
		to {
			transform: rotate(360deg);
		}
	}

	/* Der scrollende Bereich der Seite - siehe overflow: hidden in app.css. */
	main {
		flex: 1;
		overflow-y: auto;
		/* Bewusst kein overscroll-behavior: contain - sonst bleibt das
		   Überziehen am oberen Rand hier hängen und der Browser bekommt die
		   Zum-Aktualisieren-Geste nie zu sehen. */
		/* Hält den fixierten Upload-Button frei (60px hoch, 24px vom Rand),
		   damit er die Seitennavigation nicht überdeckt. */
		padding: 14px calc(10px + var(--safe-right)) calc(104px + var(--safe-bottom))
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
		/* Bewusst ein fester Wert ohne --safe-bottom: env(safe-area-inset-bottom)
		   kippt auf manchen Geräten zusammen mit der Browserleiste, und jeder
		   solche Sprung wäre am fixierten Button sofort sichtbar. Der Abstand
		   muss konstant sein. */
		bottom: 24px;
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
