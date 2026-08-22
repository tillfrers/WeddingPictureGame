<script lang="ts">
	import { page } from '$app/state';
	import { fade } from 'svelte/transition';
	import Icon from '$lib/components/Icon.svelte';
</script>

<main in:fade={{ duration: 250 }}>
	<div class="card">
		<span class="icon"><Icon name="alert" size={44} /></span>
		<h1>Ups!</h1>
		<p>{page.error?.message ?? 'Dieser Tisch konnte nicht gefunden werden.'}</p>
		<p class="hint">Bitte überprüfe den QR-Code oder suche Till für Hilfe.</p>
	</div>
</main>

<style>
	/* Feste Hoehe statt min-height, weil body nicht mehr scrollt (app.css):
	   nur so kann dieser Container ueberlangen Inhalt selbst scrollen.
	   Zentriert wird ueber margin:auto der Karte statt align-items, sonst
	   schneidet Flexbox bei Ueberlaenge den oberen Rand ab. */
	main {
		height: 100dvh;
		overflow-y: auto;
		display: flex;
		padding: 24px calc(24px + var(--safe-right)) 24px calc(24px + var(--safe-left));
	}

	.card {
		margin: auto;
		max-width: 380px;
		text-align: center;
		background: var(--color-surface);
		border-radius: var(--radius-l);
		box-shadow: var(--shadow-soft);
		padding: 40px 28px;
	}

	.icon {
		display: flex;
		justify-content: center;
		color: var(--color-text-muted);
		margin-bottom: 12px;
	}

	h1 {
		margin: 0 0 12px;
		font-size: 1.5rem;
	}

	p {
		margin: 0 0 8px;
		color: var(--color-text-muted);
		line-height: 1.5;
	}

	.hint {
		font-size: 0.9rem;
	}
</style>
