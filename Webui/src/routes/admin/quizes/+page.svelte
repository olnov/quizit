<svelte:head>
	<title>Quizzes | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
	<header class="topbar">
		<a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
		<a class="admin-link" href="/admin/login">Admin</a>
	</header>

	<section class="page-header">
		<div>
			<p class="eyebrow">Quiz administration</p>
			<h1>Quizzes</h1>
			<p>Build question sets for collaborative learning games.</p>
		</div>
		<a class="game-button" href="/admin/quizes/new">New quiz</a>
	</section>

	<section class="quiz-list" aria-label="Quizzes">
		<div class="list-head"><span>Quiz</span><span>Theme</span><span>Questions</span><span></span></div>
		<div class="empty-state">
			<h2>No quizzes to show</h2>
			<p>Create a quiz to start building a question set.</p>
			<a class="text-link" href="/admin/quizes/new">Create a quiz</a>
		</div>
	</section>
</main>

<style>
	.admin-shell { margin: 0 auto; max-width: 1180px; min-height: 100dvh; padding: 0 48px 64px; }
	.page-header { align-items: end; display: flex; justify-content: space-between; gap: 24px; padding: 70px 0 42px; }
	.page-header h1, h2 { font-family: var(--font-display); font-weight: 400; letter-spacing: 0; }
	.page-header h1 { font-size: 3.25rem; line-height: 1; margin: 10px 0 12px; }
	.page-header p:not(.eyebrow) { color: var(--color-muted); margin: 0; }
	.admin-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
	.quiz-list { border-top: 1px solid var(--color-border); }
	.list-head { color: var(--color-muted); display: grid; font-size: .74rem; font-weight: 800; gap: 20px; grid-template-columns: minmax(0, 2fr) minmax(140px, 1fr) 110px 72px; letter-spacing: .08em; padding: 17px 0; text-transform: uppercase; }
	.empty-state { border-top: 1px solid var(--color-border); padding: 58px 0; }
	.empty-state h2 { font-size: 1.7rem; margin: 0 0 8px; }
	.empty-state p { color: var(--color-muted); margin: 0; }
	.text-link { display: inline-block; font-weight: 800; margin-top: 22px; }
	@media (max-width: 680px) { .admin-shell { padding: 0 20px 40px; } .page-header { align-items: start; flex-direction: column; padding: 48px 0 34px; } .list-head { display: none; } }
</style>
