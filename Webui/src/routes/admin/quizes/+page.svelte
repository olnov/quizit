<script lang="ts">
    import { onMount } from 'svelte';
    import Button from '$lib/components/ui/Button.svelte';
    import {
        downloadQuizExport,
        getAdminQuizzes,
        importQuiz,
        quizStatusLabel,
        validateQuizImport,
        type ImportValidation,
        type QuizImportDocument,
        type QuizListItem
    } from '$lib/admin-api';

    let quizzes = $state<QuizListItem[]>([]);
    let search = $state('');
    let status = $state('all');
    let theme = $state('all');
    let loading = $state(true);
    let error = $state('');
    let importDocument = $state<QuizImportDocument | null>(null);
    let importValidation = $state<ImportValidation | null>(null);
    let importError = $state('');
    let importing = $state(false);

    const themes = $derived([...new Set(quizzes.map((quiz) => quiz.themeName))].sort());
    const filteredQuizzes = $derived(quizzes.filter((quiz) => {
        const matchesSearch = `${quiz.title} ${quiz.themeName}`.toLowerCase().includes(search.trim().toLowerCase());
        const matchesStatus = status === 'all' || String(quiz.status) === status;
        const matchesTheme = theme === 'all' || quiz.themeName === theme;
        return matchesSearch && matchesStatus && matchesTheme;
    }));

    onMount(loadQuizzes);

    async function loadQuizzes() {
        loading = true;
        error = '';
        try {
            quizzes = (await getAdminQuizzes()).items;
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            loading = false;
        }
    }

    async function handleExport(quiz: QuizListItem) {
        error = '';
        try {
            await downloadQuizExport(quiz);
        } catch (exception) {
            error = getErrorMessage(exception);
        }
    }

    async function handleImportFile(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        const file = input.files?.[0];
        importDocument = null;
        importValidation = null;
        importError = '';

        if (!file) return;

        try {
            const document = JSON.parse(await file.text()) as QuizImportDocument;
            importDocument = document;
            importValidation = await validateQuizImport(document);
        } catch (exception) {
            importError = exception instanceof SyntaxError
                ? 'The selected file is not valid JSON.'
                : getErrorMessage(exception);
        }
    }

    async function confirmImport() {
        if (!importDocument || !importValidation?.isValid) return;

        importing = true;
        importError = '';
        try {
            await importQuiz(importDocument);
            importDocument = null;
            importValidation = null;
            await loadQuizzes();
        } catch (exception) {
            importError = getErrorMessage(exception);
        } finally {
            importing = false;
        }
    }

    function getErrorMessage(exception: unknown): string {
        return exception instanceof Error ? exception.message : 'The request could not be completed.';
    }
</script>

<svelte:head>
    <title>Quizzes | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
    <header class="topbar">
        <a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
        <a class="admin-link" href="/">Leave admin</a>
    </header>

    <section class="page-header">
        <div>
            <p class="eyebrow">Quiz administration</p>
            <h1>Quizzes</h1>
            <p>Build and maintain question sets for collaborative learning games.</p>
        </div>
        <a class="game-button" href="/admin/quizes/new">New quiz</a>
    </section>

    <section class="controls" aria-label="Quiz filters">
        <label><span>Search</span><input bind:value={search} placeholder="Search quizzes" /></label>
        <label><span>Status</span><select bind:value={status}><option value="all">All statuses</option><option value="0">Draft</option><option value="1">Published</option><option value="2">Archived</option></select></label>
        <label><span>Theme</span><select bind:value={theme}><option value="all">All themes</option>{#each themes as item}<option value={item}>{item}</option>{/each}</select></label>
        <label class="import-control"><span>Import JSON</span><input accept="application/json,.json" onchange={handleImportFile} type="file" /></label>
    </section>

    {#if error}
        <p class="error" role="alert">{error}</p>
    {/if}

    {#if importValidation || importError}
        <section class="import-preview" aria-live="polite">
            <div>
                <p class="eyebrow">Import preview</p>
                {#if importValidation?.isValid && importValidation.preview}
                    <h2>{importValidation.preview.title}</h2>
                    <p>{importValidation.preview.theme} · {importValidation.preview.questionCount} questions · {importValidation.preview.questionsPerGame} per game</p>
                {:else}
                    <h2>Import needs changes</h2>
                    <ul>{#each importValidation?.errors ?? [importError] as item}<li>{item}</li>{/each}</ul>
                {/if}
            </div>
            {#if importValidation?.isValid}
                <Button type="button" disabled={importing} onclick={confirmImport}>{importing ? 'Importing' : 'Confirm import'}</Button>
            {/if}
        </section>
    {/if}

    <section class="quiz-list" aria-label="Quizzes">
        <div class="list-head"><span>Quiz</span><span>Theme</span><span>Status</span><span>Questions</span><span>Actions</span></div>
        {#if loading}
            <p class="list-message">Loading quizzes…</p>
        {:else if filteredQuizzes.length === 0}
            <div class="empty-state">
                <h2>{quizzes.length ? 'No matching quizzes' : 'No quizzes to show'}</h2>
                <p>{quizzes.length ? 'Try changing the search or filters.' : 'Create or import a quiz to start building a question set.'}</p>
            </div>
        {:else}
            {#each filteredQuizzes as quiz (quiz.id)}
                <article class="quiz-row">
                    <div><a class="quiz-title" href={`/admin/quizes/${quiz.id}`}>{quiz.title}</a><small>Updated {new Date(quiz.updatedAt).toLocaleDateString()}</small></div>
                    <span class="theme">{quiz.themeName}</span>
                    <span class:published={quiz.status === 1} class:archived={quiz.status === 2} class="status-badge">{quizStatusLabel[quiz.status]}</span>
                    <span>{quiz.questionCount} / {quiz.questionsPerGame}</span>
                    <div class="row-actions"><a href={`/admin/quizes/${quiz.id}`}>Edit</a><button type="button" onclick={() => handleExport(quiz)}>Export</button></div>
                </article>
            {/each}
        {/if}
    </section>
</main>

<style>
    .admin-shell { margin: 0 auto; max-width: 1180px; min-height: 100dvh; padding: 0 48px 64px; }
    .page-header { align-items: end; display: flex; justify-content: space-between; gap: 24px; padding: 70px 0 42px; }
    .page-header h1, h2 { font-family: var(--font-display); font-weight: 400; letter-spacing: 0; }
    .page-header h1 { font-size: 3.25rem; line-height: 1; margin: 10px 0 12px; }
    .page-header p:not(.eyebrow) { color: var(--color-muted); margin: 0; }
    .admin-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
    .controls { border-bottom: 1px solid var(--color-border); border-top: 1px solid var(--color-border); display: grid; gap: 16px; grid-template-columns: 2fr repeat(3, minmax(130px, 1fr)); padding: 16px 0; }
    .controls label { display: grid; font-size: .72rem; font-weight: 800; gap: 6px; letter-spacing: .08em; text-transform: uppercase; }
    input, select { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; letter-spacing: 0; min-height: 42px; padding: 0 10px; text-transform: none; }
    .import-control input { padding: 8px; }
    .error { color: #a32b1f; margin: 18px 0 0; }
    .import-preview { align-items: center; background: #e2eafc; border: 1px solid var(--color-accent); display: flex; gap: 24px; justify-content: space-between; margin-top: 20px; padding: 18px; }
    .import-preview h2 { font-size: 1.5rem; margin: 6px 0; }
    .import-preview p, .import-preview ul { color: var(--color-muted); margin: 0; }
    .import-preview ul { padding-left: 20px; }
    .quiz-list { border-top: 1px solid var(--color-border); margin-top: 32px; }
    .list-head, .quiz-row { align-items: center; display: grid; gap: 20px; grid-template-columns: minmax(0, 2fr) minmax(120px, 1fr) 90px 105px 115px; }
    .list-head { color: var(--color-muted); font-size: .74rem; font-weight: 800; letter-spacing: .08em; padding: 17px 0; text-transform: uppercase; }
    .quiz-row { border-top: 1px solid var(--color-border); min-height: 78px; padding: 13px 0; }
    .quiz-title { font-weight: 800; text-decoration: none; }
    .quiz-title:hover, .row-actions a:hover, .row-actions button:hover { color: var(--color-accent); }
    small { color: var(--color-muted); display: block; font-size: .73rem; margin-top: 4px; }
    .theme { color: var(--color-muted); }
    .status-badge { border: 1px solid var(--color-border-strong); font-size: .7rem; font-weight: 800; padding: 5px 7px; text-align: center; text-transform: uppercase; }
    .status-badge.published { background: #e4efb2; border-color: #92a741; }
    .status-badge.archived { color: var(--color-muted); }
    .row-actions { display: flex; gap: 12px; }
    .row-actions a, .row-actions button { background: transparent; border: 0; color: var(--color-ink); cursor: pointer; font: inherit; font-size: .78rem; font-weight: 800; padding: 0; text-decoration: underline; }
    .list-message, .empty-state { border-top: 1px solid var(--color-border); color: var(--color-muted); padding: 42px 0; }
    .empty-state h2 { color: var(--color-ink); font-size: 1.7rem; margin: 0 0 8px; }
    .empty-state p { margin: 0; }
    @media (max-width: 800px) { .admin-shell { padding: 0 20px 40px; } .page-header { align-items: start; flex-direction: column; padding: 48px 0 34px; } .controls { grid-template-columns: 1fr 1fr; } .list-head { display: none; } .quiz-row { align-items: start; gap: 8px 16px; grid-template-columns: minmax(0, 1fr) 1fr; padding: 18px 0; } .row-actions { grid-column: 1 / -1; } }
    @media (max-width: 520px) { .controls { grid-template-columns: 1fr; } .import-preview { align-items: stretch; flex-direction: column; } :global(.game-button) { width: 100%; } }
</style>
