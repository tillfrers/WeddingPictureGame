<script lang="ts">
	import { goto, invalidate, pushState, replaceState } from '$app/navigation';
	import { page as pageState } from '$app/state';
	import { SvelteSet } from 'svelte/reactivity';
	import { fade } from 'svelte/transition';
	import { imageClient, apiErrorMessage, displayUrl } from '$lib/api';
	import {
		kannTeilen,
		saveBlob,
		teileDateien,
		STAPEL_MAX_BYTES,
		STAPEL_MAX_DATEIEN
	} from '$lib/download';
	import type { PagedResultOfGalleryDto } from '$lib/api/client';
	import UploadSheet from '$lib/components/UploadSheet.svelte';
	import TransferProgress, { type TransferItem } from '$lib/components/TransferProgress.svelte';
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

	// Umgekehrt gibt es das Herunterladen der Originale nur in der Gesamtansicht:
	// dort hängt der Gast am Einladungs-Token, das genau dafür gedacht ist.
	const canSelect = $derived(tisch === null && (gallery.totalCount ?? 0) > 0);

	// Der Viewer hängt an der History (Shallow Routing), damit die Zurück-Geste
	// des Handys ihn schließt statt die Galerie-Seite zu verlassen.
	const viewerIndex = $derived(
		typeof pageState.state.viewer === 'number' && pageState.state.viewer < urls.length
			? pageState.state.viewer
			: null
	);

	let sheetOpen = $state(false);
	let uploadItems = $state<TransferItem[]>([]);
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
				uploadItems[i] = { ...uploadItems[i], status: 'running' };
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

	// ---------------------------------------------------------------- Auswahl

	// Die Markierung hängt an den Bild-Ids, nicht an Positionen: nur so übersteht
	// sie das Blättern, bei dem die Seite komplett neu geladen wird. Die
	// Komponente selbst bleibt dabei am Leben (gleiche Route), die Auswahl also
	// auch.
	let selectMode = $state(false);
	const selectedIds = new SvelteSet<string>();
	const selectedCount = $derived(selectedIds.size);

	let markingAll = $state(false);

	const LANGDRUCK_MS = 450; // ab hier gilt ein Tipp als "gedrückt halten"
	const LANGDRUCK_TOLERANZ = 10; // px Wackeln, darüber war es doch Scrollen

	let pressTimer: ReturnType<typeof setTimeout> | undefined;
	let pressX = 0;
	let pressY = 0;
	let longPressFired = false;

	function toggleSelection(id: string) {
		if (selectedIds.has(id)) selectedIds.delete(id);
		else selectedIds.add(id);
	}

	function exitSelectMode() {
		selectMode = false;
		selectedIds.clear();
	}

	function cancelPress() {
		clearTimeout(pressTimer);
		pressTimer = undefined;
	}

	// Gedrückt halten startet den Auswahlmodus - dieselbe Geste wie in der
	// Galerie-App des Handys. Im Auswahlmodus selbst genügt ein Tipp.
	function startPress(e: PointerEvent, id: string) {
		if (!canSelect || selectMode || (e.pointerType === 'mouse' && e.button !== 0)) return;

		longPressFired = false;
		pressX = e.clientX;
		pressY = e.clientY;

		cancelPress();
		pressTimer = setTimeout(() => {
			pressTimer = undefined;
			longPressFired = true;
			selectMode = true;
			selectedIds.add(id);
			navigator.vibrate?.(25);
		}, LANGDRUCK_MS);
	}

	function movePress(e: PointerEvent) {
		if (pressTimer === undefined) return;

		if (
			Math.abs(e.clientX - pressX) > LANGDRUCK_TOLERANZ ||
			Math.abs(e.clientY - pressY) > LANGDRUCK_TOLERANZ
		)
			cancelPress();
	}

	function handleThumb(index: number, id: string) {
		cancelPress();

		// Nach einem Langdruck kommt trotzdem noch ein Klick - der würde die
		// gerade gesetzte Markierung sofort wieder aufheben.
		if (longPressFired) {
			longPressFired = false;
			return;
		}

		if (selectMode) toggleSelection(id);
		else openViewer(index);
	}

	/** Wie viele Seiten gleichzeitig geholt werden, wenn alles markiert wird. */
	const GLEICHZEITIGE_SEITEN = 4;

	/**
	 * Markiert alle Bilder - auch die auf den anderen Seiten. Das Backend liefert
	 * die Galerie nur seitenweise und kennt keinen Endpunkt für "alle Ids", also
	 * werden die übrigen Seiten hier nachgeholt. In kleinen Wellen, damit bei
	 * vielen Seiten nicht dutzende Anfragen gleichzeitig laufen.
	 */
	async function markAll() {
		if (markingAll) return;

		markingAll = true;

		const ids = new Set<string>();
		const sammle = (seite: PagedResultOfGalleryDto) => {
			for (const item of seite.items ?? []) if (item.id) ids.add(item.id);
		};

		sammle(gallery);

		try {
			const offen: number[] = [];
			for (let p = 1; p <= Math.max(gallery.totalPages ?? 1, 1); p++) if (p !== page) offen.push(p);

			for (let i = 0; i < offen.length; i += GLEICHZEITIGE_SEITEN) {
				const welle = await Promise.all(
					offen.slice(i, i + GLEICHZEITIGE_SEITEN).map((p) => imageClient.getGalleryAll(p))
				);

				welle.forEach(sammle);
			}
		} catch (err) {
			// Was schon eingesammelt ist, wird trotzdem markiert - lieber ein Teil
			// der Auswahl als gar keine.
			showToast(apiErrorMessage(err));
		} finally {
			for (const id of ids) selectedIds.add(id);
			markingAll = false;
		}
	}

	// --------------------------------------------------------------- Download

	let downloadItems = $state<TransferItem[]>([]);
	let downloadActive = $state(false);

	// laden   = Originale werden geholt
	// sichern = Stapel liegt bereit und wartet auf den Tipp aufs Teilen-Menü
	// fertig  = durch
	let downloadPhase = $state<'laden' | 'sichern' | 'fertig'>('laden');
	let shareBusy = $state(false);

	// Geht dieser Lauf über das Teilen-Menü oder über den Download-Ordner?
	let dlTeilen = $state(false);
	let dlStapel = $state<File[]>([]);

	// Reiner Ablaufzustand, den die Anzeige nicht braucht.
	let dlIds: string[] = [];
	let dlCursor = 0;
	let dlStapelIndizes: number[] = [];

	// Ein Bild, das den laufenden Stapel gesprengt hätte und deshalb schon
	// geladen auf den nächsten wartet - noch einmal holen wäre Verschwendung.
	let dlUebertrag: { datei: File; index: number } | null = null;

	const downloadHeading = $derived(
		downloadPhase === 'fertig'
			? 'Fertig'
			: downloadPhase === 'sichern'
				? 'Bereit zum Sichern'
				: dlTeilen
					? 'Bilder werden geladen…'
					: 'Bilder werden gespeichert…'
	);

	// Pause zwischen zwei Downloads: Browser drosseln Downloads, die zu dicht
	// aufeinander folgen, und verschlucken dann einzelne Bilder.
	const DOWNLOAD_PAUSE_MS = 150;

	const pause = (ms: number) => new Promise((r) => setTimeout(r, ms));

	/**
	 * Startet das Sichern der markierten Bilder.
	 *
	 * Zwei Wege, siehe $lib/download: auf dem Handy über das Teilen-Menü (die
	 * Bilder landen dann wirklich in der Galerie), sonst als Download. Der
	 * Teilen-Weg geht in Stapeln, weil Chromium höchstens 10 Dateien pro Aufruf
	 * annimmt - und weil iOS für jeden Aufruf einen frischen Tipp verlangt.
	 */
	async function downloadSelected() {
		const ids = [...selectedIds];
		if (ids.length === 0 || downloadActive) return;

		dlIds = ids;
		dlCursor = 0;
		dlStapel = [];
		dlStapelIndizes = [];
		dlUebertrag = null;
		dlTeilen = kannTeilen();

		downloadItems = ids.map((_, i) => ({ name: `Bild ${i + 1}`, status: 'pending' as const }));
		downloadPhase = 'laden';
		downloadActive = true;

		await ladeWeiter();
	}

	/**
	 * Holt Bilder, bis alles durch ist oder ein Stapel zum Teilen voll ist. Auf
	 * dem Download-Weg gibt es keine Stapel - dort wird jedes Bild sofort
	 * abgelegt und ohne Unterbrechung weitergelaufen.
	 */
	async function ladeWeiter() {
		while (dlCursor < dlIds.length || dlUebertrag) {
			downloadPhase = 'laden';
			dlStapel = [];
			dlStapelIndizes = [];
			let bytes = 0;

			// Der Übertrag des letzten Stapels eröffnet den neuen.
			if (dlUebertrag) {
				dlStapel = [dlUebertrag.datei];
				dlStapelIndizes = [dlUebertrag.index];
				bytes = dlUebertrag.datei.size;
				dlUebertrag = null;
			}

			while (dlCursor < dlIds.length && (!dlTeilen || dlStapel.length < STAPEL_MAX_DATEIEN)) {
				const i = dlCursor++;
				const id = dlIds[i];

				downloadItems[i] = { ...downloadItems[i], status: 'running' };

				try {
					const datei = await imageClient.getOriginal(id);
					const name = datei.fileName || `${id}.jpg`;

					if (dlTeilen) {
						const file = new File([datei.data], name, {
							type: datei.data.type || 'image/jpeg'
						});

						// Die Größe muss vorher passen, nicht hinterher: ein Foto wiegt
						// schnell mehrere MB, und ein Stapel, der die Grenze erst beim
						// Überschreiten bemerkt, ist bereits zu schwer fürs Teilen-Menü.
						if (dlStapel.length > 0 && bytes + file.size > STAPEL_MAX_BYTES) {
							dlUebertrag = { datei: file, index: i };
							downloadItems[i] = { ...downloadItems[i], status: 'ready' };
							break;
						}

						dlStapel = [...dlStapel, file];
						dlStapelIndizes.push(i);
						bytes += file.size;
						downloadItems[i] = { ...downloadItems[i], status: 'ready' };
					} else {
						saveBlob(datei.data, name);
						downloadItems[i] = { ...downloadItems[i], status: 'done' };
						if (dlCursor < dlIds.length) await pause(DOWNLOAD_PAUSE_MS);
					}
				} catch (err) {
					downloadItems[i] = { ...downloadItems[i], status: 'error', error: apiErrorMessage(err) };
				}
			}

			// Stapel steht: ab hier braucht es einen frischen Tipp, sonst lehnt
			// iOS das Teilen-Menü als "ohne Nutzeraktion" ab.
			if (dlStapel.length > 0) {
				downloadPhase = 'sichern';
				return;
			}
		}

		downloadPhase = 'fertig';
	}

	/** Übergibt den geladenen Stapel ans Teilen-Menü des Geräts. */
	async function stapelSichern() {
		// Die Phase muss mitgeprüft werden: Svelte schreibt das DOM verzögert,
		// der Knopf des vorigen Stapels ist also noch einen Moment sichtbar,
		// während schon der nächste geladen wird. Ein Tipp in diesem Fenster
		// (oder ein Doppeltipp) würde sonst einen halb gefüllten Stapel teilen
		// und ihn dem Ladelauf unter den Händen wegziehen.
		if (shareBusy || downloadPhase !== 'sichern' || dlStapel.length === 0) return;

		shareBusy = true;
		try {
			// Frische Kopie: navigator.share erwartet eine echte Liste, keinen
			// Reaktivitäts-Proxy.
			const geteilt = await teileDateien([...dlStapel]);

			// Abgebrochen: Stapel stehen lassen, der Knopf bleibt bedienbar.
			if (!geteilt) return;

			for (const i of dlStapelIndizes) downloadItems[i] = { ...downloadItems[i], status: 'done' };
		} catch (err) {
			const text =
				err instanceof Error && err.message
					? err.message
					: 'Das Teilen-Menü hat die Bilder nicht angenommen.';

			for (const i of dlStapelIndizes)
				downloadItems[i] = { ...downloadItems[i], status: 'error', error: text };
		} finally {
			shareBusy = false;
		}

		dlStapel = [];
		dlStapelIndizes = [];
		await ladeWeiter();
	}

	function closeDownloadProgress() {
		const sauber = downloadPhase === 'fertig' && !downloadItems.some((i) => i.status === 'error');

		downloadActive = false;
		downloadItems = [];
		dlIds = [];
		dlStapel = [];
		dlStapelIndizes = [];
		dlUebertrag = null;
		dlCursor = 0;

		// Nach einem sauberen Durchlauf ist die Auswahl erledigt. Gab es Fehler
		// oder wurde abgebrochen, bleibt sie für einen zweiten Versuch stehen.
		if (sauber) exitSelectMode();
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

	// Escape verlässt den Auswahlmodus - aber nur, wenn nicht gerade der Viewer
	// offen ist, der die Taste für sich selbst braucht.
	function handleKey(e: KeyboardEvent) {
		if (e.key === 'Escape' && selectMode && viewerIndex === null && !downloadActive)
			exitSelectMode();
	}
</script>

<svelte:window onkeydown={handleKey} />

<svelte:head>
	<title>{title}</title>
</svelte:head>

<div class="page">
	<header>
		<div class="kopfzeile">
			<div class="titel">
				{#if selectMode}
					<button class="ikone" onclick={exitSelectMode} aria-label="Auswahl beenden">
						<Icon name="close" size={18} />
					</button>
				{/if}
				<h1>
					{#if selectMode}
						{selectedCount === 0 ? 'Bilder auswählen' : `${selectedCount} ausgewählt`}
					{:else}
						{title}
					{/if}
				</h1>
			</div>

			{#if canSelect}
				<div class="aktionen">
					{#if selectMode}
						<button class="aktion" onclick={markAll} disabled={markingAll}>
							{#if markingAll}
								<span class="spinner"></span>
							{:else}
								<Icon name="check-circle" size={16} />
							{/if}
							Alle markieren
						</button>
						<button class="aktion primaer" onclick={downloadSelected} disabled={selectedCount === 0}>
							<Icon name="download" size={16} />
							Herunterladen
						</button>
					{:else}
						<button class="aktion" onclick={() => (selectMode = true)}>
							<Icon name="check-circle" size={16} />
							Auswählen
						</button>
					{/if}
				</div>
			{/if}
		</div>
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
						{@const id = item.id ?? ''}
						{@const markiert = selectedIds.has(id)}
						<button
							class="thumb"
							class:auswahl={selectMode}
							class:markiert
							onclick={() => handleThumb(i, id)}
							onpointerdown={(e) => startPress(e, id)}
							onpointermove={movePress}
							onpointerup={cancelPress}
							onpointercancel={cancelPress}
							onpointerleave={cancelPress}
							oncontextmenu={(e) => {
								// Ohne das legt das Handy beim Gedrückthalten sein eigenes
								// Bild-Menü über die Auswahl.
								if (canSelect) e.preventDefault();
							}}
							aria-pressed={selectMode ? markiert : undefined}
							aria-label={selectMode
								? markiert
									? 'Auswahl aufheben'
									: 'Bild auswählen'
								: 'Bild vergrößern'}
						>
							<img src={item.thumbnailUrl} alt="" loading="lazy" draggable="false" />
							{#if selectMode}
								<span class="haken" class:an={markiert}>
									{#if markiert}<Icon name="check" size={13} />{/if}
								</span>
							{/if}
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
	<TransferProgress
		items={uploadItems}
		finished={uploadFinished}
		heading={uploadFinished ? 'Fertig' : 'Bilder werden hochgeladen…'}
		verb="hochgeladen"
		showClose={uploadFinished}
		onClose={closeUploadProgress}
	/>
{/if}

{#if downloadActive}
	<TransferProgress
		items={downloadItems}
		finished={downloadPhase === 'fertig'}
		heading={downloadHeading}
		verb="gesichert"
		showClose={downloadPhase !== 'laden'}
		closeLabel={downloadPhase === 'fertig' ? 'Schließen' : 'Abbrechen'}
		onClose={closeDownloadProgress}
		actionLabel={downloadPhase === 'sichern' ? `Bilder sichern (${dlStapel.length})` : undefined}
		actionHint={downloadPhase === 'sichern'
			? 'Im folgenden Menü „Bilder sichern“ wählen – dann landen die Fotos direkt in deiner Galerie.'
			: undefined}
		actionBusy={shareBusy}
		onAction={downloadPhase === 'sichern' ? stapelSichern : undefined}
	/>
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

	/* Umbrechend: im Auswahlmodus stehen rechts zwei Tasten, die zusammen mit dem
	   Titel auf schmalen Displays nicht in eine Zeile passen. Sie rutschen dann
	   als Gruppe in die zweite Zeile und bleiben dort rechtsbündig. */
	.kopfzeile {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 10px;
	}

	.titel {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
	}

	h1 {
		margin: 0;
		font-size: 1.2rem;
		letter-spacing: 0.02em;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.aktionen {
		display: flex;
		align-items: center;
		gap: 8px;
		/* Hält die Tasten rechts, auch wenn sie in die zweite Zeile rutschen. */
		margin-left: auto;
	}

	.aktion {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		padding: 8px 13px;
		border-radius: 999px;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		font-size: 0.85rem;
		font-weight: 600;
		white-space: nowrap;
		transition: transform 0.15s ease;
	}

	.aktion.primaer {
		background: var(--color-accent);
		border-color: var(--color-accent);
		color: var(--color-accent-contrast);
	}

	.aktion:disabled {
		opacity: 0.45;
	}

	.aktion:not(:disabled):active {
		transform: scale(0.95);
	}

	.ikone {
		flex: none;
		width: 34px;
		height: 34px;
		border-radius: 50%;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		display: flex;
		align-items: center;
		justify-content: center;
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
		/* Beim Gedrückthalten soll der Auswahlmodus starten - nicht die
		   Bild-Vorschau bzw. Textauswahl des Betriebssystems. */
		-webkit-touch-callout: none;
		-webkit-user-select: none;
		user-select: none;
	}

	.thumb:active {
		transform: scale(0.95);
	}

	/* Im Auswahlmodus bleibt die Tipp-Animation aus: das Bild schrumpft ohnehin
	   sichtbar, sobald es markiert ist. */
	.thumb.auswahl:active {
		transform: none;
	}

	.thumb img {
		width: 100%;
		height: 100%;
		object-fit: cover;
		animation: fade-in 0.3s ease;
		transition: transform 0.15s ease;
	}

	.thumb.markiert {
		outline: 2px solid var(--color-accent);
		outline-offset: -2px;
	}

	.thumb.markiert img {
		transform: scale(0.88);
	}

	.haken {
		position: absolute;
		top: 6px;
		right: 6px;
		width: 22px;
		height: 22px;
		border-radius: 50%;
		border: 2px solid rgba(255, 255, 255, 0.85);
		background: rgba(20, 16, 12, 0.35);
		color: var(--color-accent-contrast);
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.haken.an {
		background: var(--color-accent);
		border-color: var(--color-accent);
	}

	@keyframes fade-in {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}

	.spinner {
		display: inline-block;
		width: 14px;
		height: 14px;
		border: 2px solid var(--color-border);
		border-top-color: var(--color-accent);
		border-radius: 50%;
		animation: drehen 0.7s linear infinite;
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
