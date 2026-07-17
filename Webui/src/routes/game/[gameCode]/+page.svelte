<script lang="ts">
	import { goto } from '$app/navigation';
	import { Dialog } from 'bits-ui';
	import { onMount } from 'svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { completeGame, connectToRoom, getCurrentQuestion, getRoomSession, nextQuestion, saveGameResult, saveRoomSession, submitAnswer, type CurrentQuestion, type GameRoom, type Reveal, type RoomSession } from '$lib/game-room';

	let { params } = $props();
	let room = $state<GameRoom | null>(null);
	let session = $state<RoomSession | null>(null);
	let question = $state<CurrentQuestion | null>(null);
	let reveal = $state<Reveal | null>(null);
	let selectedOptionId = $state<string | null>(null);
	let message = $state('Connecting to the game...');
	let now = $state(Date.now());

	let remainingSeconds = $derived(question?.answerDeadlineAt ? Math.max(0, Math.ceil((Date.parse(question.answerDeadlineAt) - now) / 1000)) : null);
	let isHost = $derived(Boolean(session?.isHost));

	onMount(() => {
		const savedSession = getRoomSession(params.gameCode);
		if (!savedSession) { void goto('/'); return; }
		session = savedSession;
		const interval = window.setInterval(() => { now = Date.now(); }, 1000);
		let connection: { stop: () => Promise<void> } | null = null;
		void connectToRoom(params.gameCode, savedSession, (updatedRoom) => { room = updatedRoom; }, (credentials) => {
			session = { ...savedSession, ...credentials };
			saveRoomSession(params.gameCode, session);
			message = '';
		}, {
			onGameStarted: (updatedRoom) => { room = updatedRoom; },
			onQuestionStarted: (updatedQuestion) => { question = updatedQuestion; reveal = null; selectedOptionId = null; },
			onQuestionRevealed: (updatedReveal) => { reveal = updatedReveal; },
			onRoomUpdated: (updatedRoom) => { room = updatedRoom; },
			onGameCompleted: (completed) => { saveGameResult(completed); void goto(`/results/${params.gameCode}`); }
		}).then(async (startedConnection) => {
			connection = startedConnection;
			try { question = await getCurrentQuestion(params.gameCode); } catch { /* The game can be between questions. */ }
		}).catch((error) => { message = error instanceof Error ? error.message : 'Unable to join the game.'; });
		return () => { window.clearInterval(interval); void connection?.stop(); };
	});

	async function answer(optionId: string) {
		if (!session || selectedOptionId || reveal) return;
		selectedOptionId = optionId;
		try { await submitAnswer(params.gameCode, session.playerToken, optionId); }
		catch (error) { selectedOptionId = null; message = error instanceof Error ? error.message : 'Unable to submit the answer.'; }
	}

	async function advance(action: 'next' | 'complete') {
		if (!session) return;
		try {
			if (action === 'next') await nextQuestion(params.gameCode, session.playerToken);
			else {
				const completed = await completeGame(params.gameCode, session.playerToken);
				saveGameResult(completed);
				await goto(`/results/${params.gameCode}`);
			}
		} catch (error) { message = error instanceof Error ? error.message : 'Unable to update the game.'; }
	}

	function formatTime(seconds: number | null) { return seconds === null ? 'Unlimited' : `00:${seconds.toString().padStart(2, '0')}`; }
</script>

<main class="game-shell">
	<header><a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a><span class="question-count">Question {(question?.index ?? 0) + 1} / {room?.questionCount ?? '-'}</span></header>
	{#if room && question}
		<section class="game-grid">
			<div class="question-panel">
				<div class="question-meta"><span>{formatTime(remainingSeconds)}</span><span>{reveal ? 'Answer revealed' : selectedOptionId ? 'Answer submitted' : 'Choose one answer'}</span></div>
				{#if question.question.codeContext}<pre class="code-context"><code>{question.question.codeContext}</code></pre>{/if}
				<h1>{question.question.text}</h1>
				<div class="options">
					{#each question.question.options as option, index}
						<button class:chosen={selectedOptionId === option.id} class:correct={reveal?.correctOptionId === option.id} class:incorrect={Boolean(reveal && selectedOptionId === option.id && reveal.correctOptionId !== option.id)} onclick={() => answer(option.id)} disabled={Boolean(selectedOptionId || reveal)}><b>{String.fromCharCode(65 + index)}</b>{option.text}</button>
					{/each}
				</div>
				{#if isHost}
					<div class="host-controls">
						{#if room.status === 3}<Button onclick={() => advance('next')}>Next question</Button>{/if}
						<Button class="end-button" onclick={() => advance('complete')}>End game</Button>
					</div>
				{/if}
				{#if reveal?.explanation}
					<Dialog.Root>
						<Dialog.Trigger class="game-button explanation-button">Show explanation</Dialog.Trigger>
						<Dialog.Portal>
							<Dialog.Overlay class="game-dialog-overlay" />
							<Dialog.Content class="game-dialog" aria-describedby="explanation-description">
								<div class="explanation-header">
									<div><p class="eyebrow">Learning note</p><Dialog.Title>Why this answer?</Dialog.Title></div>
									<Dialog.Close class="game-dialog-close" aria-label="Close explanation">&times;</Dialog.Close>
								</div>
								<Dialog.Description id="explanation-description" class="explanation-text">{reveal.explanation}</Dialog.Description>
							</Dialog.Content>
						</Dialog.Portal>
					</Dialog.Root>
				{/if}
			</div>
			<aside class="players-panel"><p class="eyebrow">Players</p><h2>{room.players.length} in the game</h2><ul class="player-list">{#each room.players as player}<li><span class:offline={!player.isConnected} class="player-status"></span><span>{player.name}</span><small>{player.isConnected ? (player.hasAnswered ? 'Answered' : 'Thinking') : 'Offline'}</small></li>{/each}</ul></aside>
		</section>
	{:else}<section class="loading"><p>{message}</p></section>{/if}
</main>

<style>
	.game-shell { height: 100dvh; margin: 0 auto; max-width: 1320px; overflow: hidden; padding: 22px 48px; }
	header { align-items: center; border-bottom: 1px solid var(--color-border); display: flex; justify-content: space-between; min-height: 48px; }
	.question-count { color: var(--color-muted); font-size: .78rem; font-weight: 800; letter-spacing: .09em; text-transform: uppercase; }
	.game-grid { display: grid; gap: 48px; grid-template-columns: minmax(0, 1fr) 360px; padding-top: 40px; }
	.question-meta { color: var(--color-muted); display: flex; font-size: .78rem; font-weight: 800; justify-content: space-between; letter-spacing: .07em; text-transform: uppercase; }
	h1, h2 { font-family: var(--font-display); font-weight: 400; letter-spacing: 0; }
	.code-context { background: var(--color-ink); border-left: 5px solid var(--color-lime); color: #f7f7f2; font: .75rem/1.42 ui-monospace, SFMono-Regular, Consolas, monospace; margin: 16px 0 16px; max-height: 180px; overflow: auto; padding: 13px 16px; white-space: pre; }
	h1 { font-size: clamp(1.35rem, 2.1vw, 2.2rem); line-height: 1.12; margin: 14px 0 20px; max-width: 800px; }
	.options { display: grid; gap: 12px; grid-template-columns: repeat(2, minmax(0, 1fr)); }
	.options button { align-items: center; background: var(--color-surface); border: 2px solid var(--color-ink); cursor: pointer; display: flex; font: inherit; gap: 12px; min-height: 54px; padding: 9px 12px; text-align: left; }
	.options button:hover:not(:disabled), .options button.chosen { background: #e2eafc; }
	.options button.correct { background: #ccdc76; }
	.options button.incorrect { background: #f5c9c2; }
	.options button:disabled { cursor: default; }
	.options b { align-items: center; background: var(--color-ink); color: #fff; display: inline-flex; font-size: .75rem; height: 26px; justify-content: center; width: 26px; }
	.host-controls { display: flex; flex-wrap: wrap; gap: 14px; margin-top: 18px; }
	:global(.explanation-button) { margin-top: 16px; min-height: 40px; }
	.explanation-header { align-items: flex-start; display: flex; justify-content: space-between; margin-bottom: 20px; }
	.explanation-header :global(h2) { font-family: var(--font-display); font-size: 2rem; font-weight: 400; line-height: 1; margin: 7px 0 0; }
	:global(.explanation-text) { color: var(--color-muted); font-size: 1rem; line-height: 1.6; margin: 0; }
	:global(.end-button) { background: #f5d7d1; }
	.players-panel { background: var(--color-ink); box-shadow: 10px 10px 0 var(--color-lime); color: #fff; padding: 22px; }
	.players-panel .eyebrow { color: var(--color-lime); }
	h2 { font-size: 1.65rem; margin: 8px 0 16px; }
	.player-list { list-style: none; margin: 0; padding: 0; }
	.player-list li { align-items: center; border-top: 1px solid #505560; display: flex; gap: 10px; min-height: 44px; }
	.player-status { background: #8bbb4c; border-radius: 50%; height: 8px; width: 8px; }
	.player-status.offline { background: #858995; }
	.player-list small { color: #c0c3cd; margin-left: auto; }
	.loading { display: grid; min-height: 80vh; place-items: center; }
	@media (max-width: 800px) { .game-shell { height: auto; min-height: 100vh; overflow: visible; padding: 24px 20px; } .game-grid { gap: 50px; grid-template-columns: 1fr; padding-top: 52px; } .options { grid-template-columns: 1fr; } }
</style>
