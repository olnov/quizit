<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
</script>

<svelte:head>
	<title>New Quiz | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
	<header class="topbar">
		<a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
		<a class="back-link" href="/admin/quizes">Back to quizzes</a>
	</header>

	<section class="page-header">
		<p class="eyebrow">Quiz administration</p>
		<h1>New quiz</h1>
	</section>

	<form class="quiz-form">
		<label>
			<span>Quiz title</span>
			<input name="title" placeholder="e.g. ITP Workshop: Objects" />
		</label>
		<label>
			<span>Theme</span>
			<select name="theme"><option value="">Select a theme</option></select>
		</label>
		<label>
			<span>Questions per game</span>
			<input name="questionsPerGame" type="number" min="1" max="100" value="15" />
		</label>
		<div class="actions"><a href="/admin/quizes">Cancel</a><Button type="submit">Create quiz</Button></div>
	</form>
</main>

<style>
	.admin-shell { margin: 0 auto; max-width: 920px; min-height: 100dvh; padding: 0 48px 64px; }
	.back-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
	.page-header { padding: 62px 0 32px; }
	.page-header h1 { font-family: var(--font-display); font-size: 3.25rem; font-weight: 400; line-height: 1; margin: 10px 0 0; }
	.quiz-form { display: grid; gap: 22px; max-width: 560px; }
	.quiz-form label { display: grid; font-size: .875rem; font-weight: 800; gap: 8px; }
	input, select { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 48px; padding: 0 13px; }
	.actions { align-items: center; display: flex; gap: 24px; justify-content: flex-end; margin-top: 12px; }
	.actions a { color: var(--color-muted); font-size: .875rem; font-weight: 800; }
	@media (max-width: 680px) { .admin-shell { padding: 0 20px 40px; } .page-header { padding-top: 46px; } .actions { align-items: stretch; flex-direction: column-reverse; } .actions :global(.game-button) { width: 100%; } }
</style>
