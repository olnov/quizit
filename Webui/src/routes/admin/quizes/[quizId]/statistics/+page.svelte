<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { SvelteDate } from 'svelte/reactivity';
	import {
		getQuizAttemptDetail,
		getQuizDetailedReport,
		getQuizStatistics,
		type QuizAttemptDetail,
		type QuizDetailedReport,
		type QuizStatisticsReport
	} from '$lib/admin-api';
	import { formatDuration, toStatisticsInterval } from '$lib/quiz-statistics';

	let { params } = $props();
	let fromDate = $state('');
	let throughDate = $state('');
	let report = $state<QuizStatisticsReport | null>(null);
	let detail = $state<QuizAttemptDetail | null>(null);
	let selectedSessionId = $state<string | null>(null);
	let loading = $state(false);
	let detailLoading = $state(false);
	let error = $state('');
	let detailError = $state('');
	let detailedReport = $state<QuizDetailedReport | null>(null);
	let detailedOpen = $state(false);
	let detailedLoading = $state(false);
	let detailedError = $state('');
	let loadedInterval = $state<{ from: string; to: string } | null>(null);
	let requestId = 0;
	let detailedRequestId = 0;

	function localDate(date: Date): string {
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	onMount(() => {
		const today = new SvelteDate();
		const start = new SvelteDate(today);
		start.setDate(start.getDate() - 29);
		fromDate = localDate(start);
		throughDate = localDate(today);
		void loadReport(1);
	});

	async function loadReport(page: number) {
		const currentRequest = ++requestId;
		loading = true;
		error = '';
		detail = null;
		selectedSessionId = null;
		detailedRequestId += 1;
		detailedOpen = false;
		detailedReport = null;
		detailedError = '';
		loadedInterval = null;
		try {
			const interval = toStatisticsInterval(fromDate, throughDate);
			const result = await getQuizStatistics(params.quizId, interval.from, interval.to, page);
			if (currentRequest === requestId) {
				report = result;
				loadedInterval = interval;
			}
		} catch (exception) {
			if (currentRequest === requestId) {
				report = null;
				error = exception instanceof Error ? exception.message : 'Could not load quiz statistics.';
			}
		} finally {
			if (currentRequest === requestId) loading = false;
		}
	}

	async function toggleDetailedReport() {
		if (detailedOpen) {
			detailedRequestId += 1;
			detailedOpen = false;
			return;
		}
		if (!loadedInterval) return;
		detailedOpen = true;
		if (detailedReport) return;
		const currentRequest = ++detailedRequestId;
		detailedLoading = true;
		detailedError = '';
		try {
			const result = await getQuizDetailedReport(
				params.quizId,
				loadedInterval.from,
				loadedInterval.to
			);
			if (currentRequest === detailedRequestId) detailedReport = result;
		} catch (exception) {
			if (currentRequest === detailedRequestId)
				detailedError =
					exception instanceof Error ? exception.message : 'Could not load detailed report.';
		} finally {
			if (currentRequest === detailedRequestId) detailedLoading = false;
		}
	}

	async function showDetail(sessionId: string) {
		if (selectedSessionId === sessionId) {
			selectedSessionId = null;
			detail = null;
			return;
		}
		selectedSessionId = sessionId;
		detail = null;
		detailError = '';
		detailLoading = true;
		try {
			const result = await getQuizAttemptDetail(params.quizId, sessionId);
			if (selectedSessionId === sessionId) detail = result;
		} catch (exception) {
			if (selectedSessionId === sessionId)
				detailError = exception instanceof Error ? exception.message : 'Could not load answers.';
		} finally {
			if (selectedSessionId === sessionId) detailLoading = false;
		}
	}
</script>

<svelte:head>
	<title>Quiz statistics | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
	<header class="topbar">
		<a class="brand" href={resolve('/')}><span class="brand-mark">Q</span><span>QuizIt</span></a>
		<a class="back-link" href={resolve('/admin/quizes/[quizId]', { quizId: params.quizId })}
			>Back to quiz</a
		>
	</header>
	<section class="page-header">
		<p class="eyebrow">Quiz administration</p>
		<h1>Solo game statistics</h1>
		{#if report}<p>{report.quizTitle}</p>{/if}
	</section>
	<form
		class="filters"
		onsubmit={(event) => {
			event.preventDefault();
			void loadReport(1);
		}}
	>
		<label>From <input type="date" bind:value={fromDate} required /></label>
		<label>Through <input type="date" bind:value={throughDate} required /></label>
		<button type="submit" disabled={loading}>Show statistics</button>
	</form>
	<p class="hint">
		Dates use your local time. Both selected dates are included. Only completed solo games are
		shown.
	</p>
	{#if error}<p class="error" role="alert">{error}</p>{/if}
	{#if loading}<p>Loading statistics…</p>{/if}
	{#if report}
		<div class="summary">
			<div><strong>{report.totalCount}</strong><span>Completed games</span></div>
			<div><strong>{report.averageScore.toFixed(1)}</strong><span>Average score</span></div>
			<button
				class="detailed-toggle"
				type="button"
				aria-expanded={detailedOpen}
				aria-controls="detailed-report"
				onclick={toggleDetailedReport}
				>{detailedOpen ? 'Hide detailed report' : 'Detailed report'}</button
			>
		</div>
		{#if detailedOpen}
			<section id="detailed-report" class="detailed-report" aria-label="Detailed report">
				{#if detailedLoading}<p>Loading detailed report…</p>{/if}
				{#if detailedError}<p class="error" role="alert">{detailedError}</p>{/if}
				{#if detailedReport}
					<div class="duration-card">
						<strong>{formatDuration(detailedReport.averageCompletionSeconds)}</strong>
						<span>Average game completion time</span>
						<p>From game start to completion, including time spent reviewing answers.</p>
					</div>
					<div class="report-grids">
						<section>
							<h2>Questions taking the most time</h2>
							<p class="report-note">
								Average time from opening a question to submitting an answer. Timing is available
								for games started after this update.
							</p>
							{#if detailedReport.slowestQuestions.length === 0}
								<p>No per-question timing data in this period.</p>
							{:else}
								<ol class="ranked-list">
									{#each detailedReport.slowestQuestions as question (question.questionId)}
										<li>
											<strong>{question.question}</strong><span
												>{formatDuration(question.averageAnswerSeconds)} average · {question.timedAnswerCount}
												timed {question.timedAnswerCount === 1 ? 'answer' : 'answers'}</span
											>
										</li>
									{/each}
								</ol>
							{/if}
						</section>
						<section>
							<h2>Most missed questions</h2>
							<p class="report-note">
								Ranked by the share of incorrect submitted answers. Unanswered questions are
								excluded.
							</p>
							{#if detailedReport.mostMissedQuestions.length === 0}
								<p>No incorrect answers in this period.</p>
							{:else}
								<ol class="ranked-list">
									{#each detailedReport.mostMissedQuestions as question (question.questionId)}
										<li>
											<strong>{question.question}</strong><span
												>{question.incorrectCount} incorrect of {question.answeredCount} answered ({Math.round(
													(question.incorrectCount / question.answeredCount) * 100
												)}%)</span
											>
										</li>
									{/each}
								</ol>
							{/if}
						</section>
					</div>
				{/if}
			</section>
		{/if}
		{#if report.totalCount === 0}
			<p>No completed solo games in this period.</p>
		{:else}
			<div class="table-wrap">
				<table>
					<thead
						><tr
							><th>Player</th><th>Completed</th><th>Score</th><th>Correct</th><th>Answered</th><th
							></th></tr
						></thead
					>
					<tbody>
						{#each report.items as attempt (attempt.sessionId)}
							<tr>
								<td>{attempt.playerName}</td>
								<td>{new Date(attempt.completedAt).toLocaleString()}</td>
								<td>{attempt.score}</td>
								<td>{attempt.correctCount} / {attempt.questionCount}</td>
								<td>{attempt.answeredCount} / {attempt.questionCount}</td>
								<td
									><button
										type="button"
										aria-expanded={selectedSessionId === attempt.sessionId}
										onclick={() => showDetail(attempt.sessionId)}
										>{selectedSessionId === attempt.sessionId
											? 'Hide answers'
											: 'View answers'}</button
									></td
								>
							</tr>
							{#if selectedSessionId === attempt.sessionId}
								<tr class="details-row"
									><td colspan="6">
										{#if detailLoading}<p>Loading answers…</p>{/if}
										{#if detailError}<p class="error" role="alert">{detailError}</p>{/if}
										{#if detail}
											<ol class="answers">
												{#each detail.rows as row, index (index)}
													<li>
														<strong>{row.question}</strong>
														<p>
															Player answer: {row.playerAnswer ?? 'No answer'} · Correct answer: {row.correctAnswer}
															· {row.isCorrect ? 'Correct' : 'Incorrect'}
														</p>
														{#if row.explanation}<small>{row.explanation}</small>{/if}
													</li>
												{/each}
											</ol>
										{/if}
									</td></tr
								>
							{/if}
						{/each}
					</tbody>
				</table>
			</div>
			<nav class="pagination" aria-label="Statistics pages">
				<button
					type="button"
					disabled={loading || report.page <= 1}
					onclick={() => loadReport(report!.page - 1)}>Previous</button
				>
				<span>Page {report.page} of {report.totalPages}</span>
				<button
					type="button"
					disabled={loading || report.page >= report.totalPages}
					onclick={() => loadReport(report!.page + 1)}>Next</button
				>
			</nav>
		{/if}
	{/if}
</main>

<style>
	.admin-shell {
		margin: 0 auto;
		max-width: 1180px;
		min-height: 100dvh;
		padding: 0 48px 64px;
	}
	.topbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
	.back-link {
		font-size: 0.82rem;
		font-weight: 800;
	}
	.page-header {
		padding: 70px 0 24px;
	}
	h1 {
		font-family: var(--font-display);
		font-size: 3.2rem;
		font-weight: 400;
		margin: 8px 0;
	}
	.page-header p:last-child,
	.hint {
		color: var(--color-muted);
	}
	.filters {
		display: flex;
		align-items: end;
		flex-wrap: wrap;
		gap: 16px;
		padding: 20px 0;
		border-top: 1px solid var(--color-border);
	}
	.filters label {
		display: grid;
		gap: 6px;
		font-size: 0.8rem;
		font-weight: 800;
	}
	input {
		min-height: 42px;
		padding: 0 10px;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		color: var(--color-ink);
		font: inherit;
	}
	button {
		min-height: 42px;
		padding: 0 16px;
		border: 1px solid var(--color-border-strong);
		background: var(--color-surface);
		color: var(--color-ink);
		cursor: pointer;
		font: inherit;
		font-weight: 800;
	}
	button:disabled {
		opacity: 0.5;
		cursor: default;
	}
	.error {
		color: #a32b1f;
	}
	.summary {
		display: flex;
		align-items: end;
		flex-wrap: wrap;
		gap: 40px;
		margin: 36px 0;
	}
	.summary div {
		display: grid;
		gap: 4px;
	}
	.summary strong {
		font-family: var(--font-display);
		font-size: 2.5rem;
		font-weight: 400;
	}
	.summary span {
		color: var(--color-muted);
		font-size: 0.82rem;
	}
	.detailed-toggle {
		margin-bottom: 2px;
	}
	.detailed-report {
		border-top: 1px solid var(--color-border);
		border-bottom: 1px solid var(--color-border);
		padding: 28px 0;
		margin-bottom: 32px;
	}
	.duration-card {
		display: grid;
		gap: 4px;
	}
	.duration-card strong {
		font-family: var(--font-display);
		font-size: 2.5rem;
		font-weight: 400;
	}
	.duration-card span {
		font-weight: 800;
	}
	.duration-card p,
	.report-note {
		color: var(--color-muted);
		font-size: 0.85rem;
		line-height: 1.5;
	}
	.duration-card p {
		margin: 4px 0 0;
	}
	.report-grids {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 40px;
		margin-top: 32px;
	}
	.report-grids h2 {
		font-family: var(--font-display);
		font-size: 1.55rem;
		font-weight: 400;
		margin: 0;
	}
	.ranked-list {
		padding-left: 22px;
	}
	.ranked-list li {
		border-top: 1px solid var(--color-border);
		padding: 12px 0;
	}
	.ranked-list strong,
	.ranked-list span {
		display: block;
	}
	.ranked-list span {
		color: var(--color-muted);
		font-size: 0.85rem;
		margin-top: 4px;
	}
	.table-wrap {
		overflow-x: auto;
	}
	table {
		width: 100%;
		border-collapse: collapse;
		text-align: left;
	}
	th,
	td {
		padding: 14px 10px;
		border-top: 1px solid var(--color-border);
		vertical-align: top;
	}
	th {
		color: var(--color-muted);
		font-size: 0.75rem;
		text-transform: uppercase;
	}
	.answers {
		margin: 0;
		padding-left: 24px;
	}
	.answers li {
		margin: 16px 0;
	}
	.answers p {
		margin: 5px 0;
	}
	.answers small {
		color: var(--color-muted);
	}
	.details-row {
		background: var(--color-surface);
	}
	.pagination {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16px;
		margin-top: 24px;
	}
	@media (max-width: 700px) {
		.admin-shell {
			padding: 0 20px 40px;
		}
		.page-header {
			padding-top: 42px;
		}
		h1 {
			font-size: 2.4rem;
		}
		.report-grids {
			grid-template-columns: 1fr;
			gap: 22px;
		}
	}
</style>
