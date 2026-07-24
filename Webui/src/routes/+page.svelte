<script lang="ts">
	import CreateRoomDialog from '$lib/components/CreateRoomDialog.svelte';
	import JoinRoomDialog from '$lib/components/JoinRoomDialog.svelte';
</script>

<svelte:head>
	<title>QuizIt</title>
	<meta name="description" content="Create a live trivia room or join a game with a room code." />
</svelte:head>

<main class="page-shell">
	<nav class="topbar" aria-label="Main navigation">
		<a class="brand" href="/" aria-label="QuizIt home">
			<span class="brand-mark" aria-hidden="true">Q</span>
			<span>QuizIt</span>
		</a>
	</nav>

	<section class="hero" aria-labelledby="page-title">
		<div class="hero-copy">
			<h1 id="page-title">A better way to study IT foundations together.</h1>
			<p class="intro">
				Start a room, share the code, and let every answer count. Built for quick games with friends,
				teams, and classrooms.
			</p>
			<div class="hero-actions">
			    <CreateRoomDialog solo />
				<CreateRoomDialog />
				<JoinRoomDialog />
			</div>
		</div>

		<div class="quiz-preview" aria-label="Example quiz question">
			<div class="preview-topline">
				<span>Round 03</span>
				<span>00:18</span>
			</div>
			<div class="progress-track" aria-hidden="true"><span></span></div>
			<p class="preview-label">JavaScript</p>
			<h2>What would this code return?</h2>
			<h3>
				<code>{'(() => {})();'}</code>
			</h3>
			<div class="answer-grid">
				<span><b>A</b> Error</span>
				<span><b>B</b> Null</span>
				<span class="selected"><b>C</b> Undefined</span>
				<span><b>D</b> Empty String</span>
			</div>
		</div>
	</section>
</main>
