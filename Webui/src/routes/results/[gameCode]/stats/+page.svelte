<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { getPlayerStatistics, getRoomSession, type PlayerStatistics } from '$lib/game-room';

	let { params } = $props();
	let statistics = $state<PlayerStatistics | null>(null);
	let message = $state('Loading your statistics...');
	let correctAnswers = $derived(statistics?.rows.filter((row) => row.isCorrect).length ?? 0);

	onMount(async () => {
		const session = getRoomSession(params.gameCode);
		if (!session) {
			await goto('/');
			return;
		}

		try {
			statistics = await getPlayerStatistics(params.gameCode, session.playerToken);
			message = '';
		} catch (error) {
			message = error instanceof Error ? error.message : 'Unable to load your statistics.';
		}
	});
</script>

<svelte:head>
	<title>My statistics | QuizIt</title>
</svelte:head>

<main class="statistics-shell">
	<a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
	<section>
		<p class="eyebrow">Round complete</p>
		<h1>My statistics</h1>
		{#if statistics}
			<p class="score">{statistics.score} pts <span>[{correctAnswers}/{statistics.rows.length}]</span></p>
			<div class="table-wrapper">
				<table>
					<thead>
						<tr><th>#</th><th>Question</th><th>My answer</th><th>Correct answer</th><th>Answered correctly</th><th>Explanation</th></tr>
					</thead>
					<tbody>
						{#each statistics.rows as row, index}
							<tr>
								<td>{index + 1}</td>
								<td>{row.question}</td>
								<td>{row.playerAnswer ?? 'No answer'}</td>
								<td>{row.correctAnswer}</td>
								<td class:correct={row.isCorrect} class:incorrect={!row.isCorrect}>{row.isCorrect ? 'Yes' : 'No'}</td>
								<td>{row.explanation ?? '—'}</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{:else}
			<p>{message}</p>
		{/if}
		<a class="back-link" href={`/results/${params.gameCode}`}>Back to results</a>
	</section>
</main>

<style>
	.statistics-shell {
		margin: 0 auto;
		max-width: 1240px;
		min-height: 100vh;
		padding: 34px 48px 64px;
	}
	section { padding-top: 72px; }
	h1 {
		font-family: var(--font-display);
		font-size: clamp(3rem, 7vw, 6rem);
		font-weight: 400;
		line-height: .9;
		margin: 16px 0;
	}
	.score { color: var(--color-muted); font-size: 1.1rem; font-weight: 800; margin: 0 0 30px; }
	.table-wrapper { overflow-x: auto; }
	table { border-collapse: collapse; min-width: 860px; width: 100%; }
	th, td { border: 1px solid var(--color-border); padding: 15px; text-align: left; vertical-align: top; }
	th { background: var(--color-ink); color: #fff; font-size: .75rem; letter-spacing: .07em; text-transform: uppercase; }
	td { line-height: 1.5; }
	th:first-child, td:first-child { min-width: 48px; padding-inline: 10px; white-space: nowrap; width: 48px; }
	td.correct { background: #ccdc76; }
	td.incorrect { background: #f5c9c2; }
	td:nth-child(2) { font-weight: 700; min-width: 220px; }
	.back-link { display: inline-block; font-weight: 800; margin-top: 30px; }
	@media (max-width: 600px) { .statistics-shell { padding: 26px 20px 40px; } section { padding-top: 54px; } }
</style>
