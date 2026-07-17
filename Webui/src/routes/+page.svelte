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
			<p class="eyebrow">Live quiz platform</p>
			<h1 id="page-title">A better way to play trivia together.</h1>
			<p class="intro">
				Start a room, share the code, and let every answer count. Built for quick games with friends,
				teams, and classrooms.
			</p>
			<div class="hero-actions">
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
			<p class="preview-label">Science &amp; discovery</p>
			<h2>Which planet has the shortest day in our solar system?</h2>
			<div class="answer-grid">
				<span><b>A</b> Mercury</span>
				<span><b>B</b> Mars</span>
				<span class="selected"><b>C</b> Jupiter</span>
				<span><b>D</b> Venus</span>
			</div>
		</div>
	</section>
</main>
