<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';

	let selectedQuestion = $state(1);
</script>

<svelte:head>
	<title>Quiz Editor | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
	<header class="topbar">
		<a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
		<a class="back-link" href="/admin/quizes">Back to quizzes</a>
	</header>

	<section class="editor-header">
		<div><p class="eyebrow">Quiz editor</p><h1>Untitled quiz</h1></div>
		<div class="editor-actions"><Button type="button">Save changes</Button></div>
	</section>

	<div class="editor-layout">
		<aside class="question-list">
			<div class="list-title"><span>Questions</span><button type="button" aria-label="Add question">+</button></div>
			<button class:active={selectedQuestion === 1} type="button" onclick={() => selectedQuestion = 1}><span>01</span><strong>New question</strong><small>100</small></button>
		</aside>

		<form class="question-form">
			<div class="field-row">
				<label><span>Difficulty</span><input type="number" min="0" max="1000" step="100" value="100" /></label>
				<label><span>Theme</span><select><option>Choose a theme</option></select></label>
			</div>
			<label><span>Question</span><textarea rows="3" placeholder="Write the question players will see"></textarea></label>
			<label><span>Code context</span><textarea class="code-field" rows="8" placeholder="Optional code or supporting context"></textarea></label>
			<fieldset><legend>Answer options</legend><div class="options"><label><input type="radio" name="correctOption" /><span>A</span><input placeholder="Option A" /></label><label><input type="radio" name="correctOption" /><span>B</span><input placeholder="Option B" /></label><label><input type="radio" name="correctOption" /><span>C</span><input placeholder="Option C" /></label><label><input type="radio" name="correctOption" /><span>D</span><input placeholder="Option D" /></label></div></fieldset>
			<label><span>Explanation</span><textarea rows="4" placeholder="Explain the correct answer after the reveal"></textarea></label>
		</form>
	</div>
</main>

<style>
	.admin-shell { margin: 0 auto; max-width: 1280px; min-height: 100dvh; padding: 0 48px 64px; }
	.back-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
	.editor-header { align-items: end; display: flex; justify-content: space-between; gap: 24px; padding: 52px 0 34px; }
	.editor-header h1 { font-family: var(--font-display); font-size: 3rem; font-weight: 400; line-height: 1; margin: 9px 0 0; }
	.editor-layout { border-top: 1px solid var(--color-border); display: grid; grid-template-columns: 260px minmax(0, 1fr); }
	.question-list { border-right: 1px solid var(--color-border); padding: 18px 18px 18px 0; }
	.list-title { align-items: center; color: var(--color-muted); display: flex; font-size: .74rem; font-weight: 800; justify-content: space-between; letter-spacing: .08em; margin-bottom: 13px; text-transform: uppercase; }
	.list-title button { background: transparent; border: 1px solid var(--color-border); color: var(--color-ink); cursor: pointer; font-size: 1.2rem; height: 28px; line-height: 1; width: 28px; }
	.question-list > button { align-items: center; background: transparent; border: 1px solid var(--color-border); color: var(--color-ink); cursor: pointer; display: grid; gap: 9px; grid-template-columns: 28px minmax(0, 1fr) 30px; margin-bottom: 8px; padding: 11px 9px; text-align: left; width: 100%; }
	.question-list > button.active { background: #e2eafc; border-color: var(--color-accent); }
	.question-list span, .question-list small { color: var(--color-muted); font-size: .72rem; font-weight: 800; }
	.question-list strong { font-size: .84rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.question-form { display: grid; gap: 22px; padding: 22px 0 22px 36px; }
	.question-form label { display: grid; font-size: .875rem; font-weight: 800; gap: 8px; }
	.field-row { display: grid; gap: 18px; grid-template-columns: 160px minmax(0, 1fr); }
	input, select, textarea { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 46px; padding: 10px 13px; resize: vertical; width: 100%; }
	.code-field { background: var(--color-ink); color: #f7f7f2; font-family: var(--font-mono); font-size: .84rem; line-height: 1.5; }
	fieldset { border: 0; margin: 0; padding: 0; }
	legend { font-size: .875rem; font-weight: 800; margin-bottom: 8px; }
	.options { display: grid; gap: 10px; grid-template-columns: repeat(2, minmax(0, 1fr)); }
	.options label { align-items: center; border: 1px solid var(--color-border); display: grid; gap: 9px; grid-template-columns: auto 24px minmax(0, 1fr); padding: 8px; }
	.options input[type='radio'] { accent-color: var(--color-accent); min-height: auto; width: auto; }
	.options span { align-items: center; background: var(--color-ink); color: var(--color-surface); display: inline-flex; font-size: .72rem; height: 24px; justify-content: center; }
	.options input:not([type='radio']) { border: 0; min-height: 32px; padding: 0; }
	@media (max-width: 820px) { .admin-shell { padding: 0 20px 40px; } .editor-layout { grid-template-columns: 1fr; } .question-list { border-bottom: 1px solid var(--color-border); border-right: 0; padding: 18px 0; } .question-form { padding: 26px 0; } }
	@media (max-width: 560px) { .editor-header { align-items: stretch; flex-direction: column; } .editor-actions :global(.game-button) { width: 100%; } .field-row, .options { grid-template-columns: 1fr; } }
</style>
