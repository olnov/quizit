<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import {
		connectToRoom,
		getQuizDifficultyCounts,
		getRoomSession,
		saveRoomSession,
		startRoom,
		updateRoomSettings,
		type GameRoom,
		type RoomSession
	} from '$lib/game-room';

	let { params } = $props();
	let room = $state<GameRoom | null>(null);
	let session = $state<RoomSession | null>(null);
	let message = $state('Connecting to the lobby...');
	let now = $state(Date.now());
	let starting = $state(false);
	let questionCount = $state(1);
	let answerTimeLimitSeconds = $state<number | null>(null);
	let questionSelectionMode = $state(0);
	let specificDifficulty = $state(100);
	let difficultyCounts = $state<Record<number, number>>({});
	let difficultyCountsLoaded = $state(false);

	let remainingSeconds = $derived(room ? Math.max(0, Math.ceil((Date.parse(room.lobbyExpiresAt) - now) / 1000)) : 0);
	let connectedGuests = $derived(room?.players.filter((player) => player.isConnected && player.playerId !== session?.playerId).length ?? 0);
	let hasAvailableQuestions = $derived(questionSelectionMode !== 1 || (difficultyCountsLoaded && (difficultyCounts[specificDifficulty] ?? 0) >= questionCount));
	let canStart = $derived(Boolean(session?.isHost && connectedGuests > 0 && remainingSeconds > 0 && hasAvailableQuestions));

	onMount(() => {
		const savedSession = getRoomSession(params.gameCode);
		if (!savedSession) {
			void goto('/');
			return;
		}

		session = savedSession;
		let isMounted = true;
		const interval = window.setInterval(() => { now = Date.now(); }, 1000);

		void connectToRoom(
			params.gameCode,
			savedSession,
			(updatedRoom) => {
				if (isMounted) {
					room = updatedRoom;
					questionCount = updatedRoom.questionCount;
					answerTimeLimitSeconds = updatedRoom.answerTimeLimitSeconds;
					questionSelectionMode = updatedRoom.questionSelectionMode;
					specificDifficulty = updatedRoom.specificDifficulty ?? 100;
					void loadDifficultyCounts(updatedRoom.quizId);
				}
			},
			(credentials) => {
				if (!isMounted) return;
				session = { ...savedSession, ...credentials };
				saveRoomSession(params.gameCode, session);
				message = '';
			},
			{
				onGameStarted: () => { void goto(`/game/${params.gameCode}`); }
			}
		).catch((error) => {
			message = error instanceof Error ? error.message : 'Unable to join the lobby.';
		});

		return () => {
			isMounted = false;
			window.clearInterval(interval);
		};
	});

	async function startGame() {
		if (!session || !canStart) return;

		starting = true;
		message = '';
		try {
			await updateRoomSettings(params.gameCode, session.playerToken, questionCount, answerTimeLimitSeconds, questionSelectionMode, questionSelectionMode === 1 ? specificDifficulty : null);
			await startRoom(params.gameCode, session.playerToken);
		} catch (error) {
			message = error instanceof Error ? error.message : 'Unable to start the game.';
		} finally {
			starting = false;
		}
	}

	async function loadDifficultyCounts(quizId: string) {
		try {
			const counts = await getQuizDifficultyCounts(quizId);
			difficultyCounts = Object.fromEntries(counts.map((item) => [item.difficulty, item.count]));
			difficultyCountsLoaded = true;
			clampQuestionCount();
		} catch (error) {
			message = error instanceof Error ? error.message : 'Unable to load question availability.';
		}
	}

	function clampQuestionCount() {
		if (questionSelectionMode !== 1 || !difficultyCountsLoaded) return;
		const available = difficultyCounts[specificDifficulty] ?? 0;
		if (available > 0 && questionCount > available) questionCount = available;
	}

	function changeSelectionMode(event: Event) {
		questionSelectionMode = Number((event.currentTarget as HTMLSelectElement).value);
		clampQuestionCount();
	}

	function changeSpecificDifficulty(event: Event) {
		specificDifficulty = Number((event.currentTarget as HTMLInputElement).value);
		clampQuestionCount();
	}

	function formatRemaining(seconds: number) {
		const minutes = Math.floor(seconds / 60).toString().padStart(2, '0');
		const remaining = (seconds % 60).toString().padStart(2, '0');
		return `${minutes}:${remaining}`;
	}
</script>

<svelte:head>
	<title>Lobby | QuizIt</title>
</svelte:head>

<main class="lobby-shell">
	<a class="brand" href="/" aria-label="QuizIt home">
		<span class="brand-mark" aria-hidden="true">Q</span>
		<span>QuizIt</span>
	</a>

	{#if room}
		<section class="lobby-grid" aria-labelledby="lobby-title">
			<div class="lobby-main">
				<h1 id="lobby-title">Your room is ready.</h1>
				<p class="room-code-label">Share this room code</p>
				<div class="room-code">{room.gameCode}</div>
				<div class:expired={remainingSeconds === 0} class="timer">
					<span>Room closes in</span>
					<strong>{formatRemaining(remainingSeconds)}</strong>
				</div>
				{#if session?.isHost}
					<div class="round-settings">
						<label>Questions <input type="number" min="1" max={questionSelectionMode === 1 && difficultyCountsLoaded ? difficultyCounts[specificDifficulty] ?? 0 : 100} bind:value={questionCount} /></label>
						<label>Answer time <select value={answerTimeLimitSeconds ?? 'unlimited'} onchange={(event) => { const value = event.currentTarget.value; answerTimeLimitSeconds = value === 'unlimited' ? null : Number(value); }}><option value="15">15 sec</option><option value="30">30 sec</option><option value="60">60 sec</option><option value="unlimited">Unlimited</option></select></label>
						<label>Question order <select value={questionSelectionMode} onchange={changeSelectionMode}><option value={0}>Ascending</option><option value={1}>Specific</option><option value={2}>Mixed</option></select></label>
						{#if questionSelectionMode === 1}<label>Difficulty <input type="number" min="0" max="1000" step="100" value={specificDifficulty} onchange={changeSpecificDifficulty} /></label>{/if}
					</div>
					{#if questionSelectionMode === 1 && difficultyCountsLoaded}<p class="availability">Available at {specificDifficulty}: {difficultyCounts[specificDifficulty] ?? 0} questions</p>{/if}
					<p class="host-note">
						{connectedGuests > 0
							? `${connectedGuests} player${connectedGuests === 1 ? '' : 's'} connected. You can start the game.`
							: 'Waiting for at least one player to connect before the game can start.'}
					</p>
					<Button class="start-button" onclick={startGame} disabled={!canStart || starting}>
						{starting ? 'Starting...' : 'Start game'}
					</Button>
				{:else}
					<p class="host-note">You are in the room. Waiting for the host to start the game.</p>
				{/if}
				{#if message}<p class="lobby-message" aria-live="polite">{message}</p>{/if}
			</div>

			<aside class="players-panel" aria-labelledby="players-title">
				<div class="players-header">
					<div>
						<p class="eyebrow">Players</p>
						<h2 id="players-title">In the room ({room.players.length})</h2>
					</div>
					<span class="live-indicator">Live</span>
				</div>
				<ul class="player-list">
					{#each room.players as player}
						<li>
							<span class:offline={!player.isConnected} class="player-status" aria-hidden="true"></span>
							<span>{player.name}</span>
							{#if session?.isHost && player.playerId === session.playerId}<small>Host</small>{/if}
							{#if !player.isConnected}<small>Disconnected</small>{/if}
						</li>
					{/each}
				</ul>
			</aside>
		</section>
	{:else}
		<section class="loading-state"><p>{message}</p></section>
	{/if}
</main>

<style>
	.lobby-shell { margin: 0 auto; max-width: 1240px; min-height: 100vh; padding: 34px 48px 48px; }
	.lobby-grid { align-items: stretch; display: grid; gap: 72px; grid-template-columns: minmax(0, 1fr) minmax(340px, .7fr); padding-top: 94px; }
	.lobby-main { align-self: center; }
	h1, h2 { font-family: var(--font-display); font-weight: 400; letter-spacing: 0; }
	h1 { font-size: clamp(3.2rem, 6vw, 6rem); line-height: .94; margin: 17px 0 42px; }
	.room-code-label { color: var(--color-muted); font-size: .76rem; font-weight: 800; letter-spacing: .1em; margin: 0 0 10px; text-transform: uppercase; }
	.room-code { border-bottom: 3px solid var(--color-ink); display: inline-block; font-family: var(--font-display); font-size: clamp(3rem, 5vw, 5rem); letter-spacing: .12em; line-height: 1; padding: 0 0 10px .12em; }
	.timer { align-items: center; background: #e4e6ef; display: flex; gap: 14px; margin-top: 36px; padding: 14px 16px; width: fit-content; }
	.timer span { color: var(--color-muted); font-size: .75rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
	.timer strong { font-size: 1.08rem; letter-spacing: .04em; }
	.timer.expired { background: #f5c9c2; }
	.host-note { color: var(--color-muted); line-height: 1.55; margin: 25px 0 18px; max-width: 440px; }
	.round-settings { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 24px; }
	.round-settings label { color: var(--color-muted); display: grid; font-size: .75rem; font-weight: 800; gap: 6px; letter-spacing: .06em; text-transform: uppercase; }
	.round-settings input, .round-settings select { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); min-height: 40px; padding: 0 10px; }
	.availability { color: var(--color-muted); font-size: .84rem; margin: 10px 0 -8px; }
	:global(.start-button) { min-width: 192px; }
	.lobby-message { color: var(--color-muted); font-size: .9rem; line-height: 1.5; margin-top: 18px; max-width: 450px; }
	.players-panel { background: var(--color-ink); box-shadow: 12px 12px 0 var(--color-lime); color: #f7f7f2; min-height: 420px; padding: 28px; }
	.players-header { align-items: flex-start; border-bottom: 1px solid #555a64; display: flex; justify-content: space-between; padding-bottom: 23px; }
	.players-header .eyebrow { color: var(--color-lime); }
	h2 { font-size: 2rem; line-height: 1; margin: 8px 0 0; }
	.live-indicator { align-items: center; color: var(--color-lime); display: inline-flex; font-size: .72rem; font-weight: 800; gap: 7px; letter-spacing: .08em; text-transform: uppercase; }
	.live-indicator::before { background: var(--color-lime); border-radius: 50%; content: ''; height: 7px; width: 7px; }
	.player-list { display: grid; gap: 3px; list-style: none; margin: 20px 0 0; padding: 0; }
	.player-list li { align-items: center; border-bottom: 1px solid #3f444d; display: flex; gap: 11px; min-height: 53px; }
	.player-status { background: #8bbb4c; border-radius: 50%; height: 8px; width: 8px; }
	.player-status.offline { background: #858995; }
	.player-list small { color: #b8bbc5; font-size: .72rem; margin-left: auto; }
	.loading-state { display: grid; min-height: calc(100vh - 100px); place-items: center; }
	.loading-state p { color: var(--color-muted); }
	:global(.game-button:disabled) { box-shadow: none; cursor: not-allowed; opacity: .45; transform: none; }
	@media (max-width: 760px) { .lobby-shell { padding: 26px 20px; } .lobby-grid { gap: 54px; grid-template-columns: 1fr; padding-top: 64px; } .players-panel { min-height: 0; } }
</style>
