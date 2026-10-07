<script lang="ts">
	import { getPlayerStatistics, type PlayerStatistics } from '$lib/game-room';
	import { downloadStatisticsPdf } from '$lib/pdf-report';

	let {
		gameCode,
		playerName,
		playerToken,
		statistics = null
	}: {
		gameCode: string;
		playerName: string;
		playerToken: string;
		statistics?: PlayerStatistics | null;
	} = $props();

	let isDownloading = $state(false);
	let error = $state('');

	async function download() {
		if (isDownloading) return;
		isDownloading = true;
		error = '';
		try {
			const report = statistics ?? (await getPlayerStatistics(gameCode, playerToken));
			await downloadStatisticsPdf(report, gameCode, playerName);
		} catch (cause) {
			error = cause instanceof Error ? cause.message : 'Unable to download the PDF report.';
		} finally {
			isDownloading = false;
		}
	}
</script>

<span class="download-control">
	<button type="button" onclick={download} disabled={isDownloading}>
		{isDownloading ? 'Preparing PDF...' : 'Download PDF'}
	</button>
	{#if error}<small role="alert">{error}</small>{/if}
</span>

<style>
	.download-control {
		display: inline-flex;
		flex-direction: column;
		gap: 5px;
	}
	button {
		background: none;
		border: 0;
		color: var(--color-ink);
		cursor: pointer;
		font: inherit;
		font-size: 0.78rem;
		font-weight: 800;
		padding: 0;
		text-decoration: underline;
		text-transform: uppercase;
	}
	button:disabled {
		color: var(--color-muted);
		cursor: wait;
	}
	button:focus-visible {
		outline: 3px solid var(--color-accent);
		outline-offset: 4px;
	}
	small {
		color: #b13929;
		font-size: 0.75rem;
		max-width: 240px;
	}
</style>
