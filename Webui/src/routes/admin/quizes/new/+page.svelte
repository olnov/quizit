<script lang="ts">
    import { goto } from '$app/navigation';
    import { onMount } from 'svelte';
    import Button from '$lib/components/ui/Button.svelte';
    import { createAdminQuiz, createQuizTheme, getQuizThemes, type QuizTheme } from '$lib/admin-api';

    let themes = $state<QuizTheme[]>([]);
    let title = $state('');
    let themeId = $state('');
    let newThemeName = $state('');
    let questionsPerGame = $state(10);
    let loading = $state(true);
    let saving = $state(false);
    let creatingTheme = $state(false);
    let error = $state('');

    onMount(async () => {
        try {
            themes = await getQuizThemes();
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            loading = false;
        }
    });

    async function createQuiz() {
        error = '';
        if (!title.trim() || !themeId) {
            error = 'Enter a quiz title and choose a theme.';
            return;
        }

        saving = true;
        try {
            const quiz = await createAdminQuiz({ title: title.trim(), themeId, questionsPerGame });
            await goto(`/admin/quizes/${quiz.id}`);
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            saving = false;
        }
    }

    async function addTheme() {
        error = '';
        const name = newThemeName.trim();
        if (!name) {
            error = 'Enter a theme name.';
            return;
        }

        creatingTheme = true;
        try {
            const theme = await createQuizTheme(name);
            themes = [...themes, theme].sort((left, right) => left.name.localeCompare(right.name));
            themeId = theme.id;
            newThemeName = '';
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            creatingTheme = false;
        }
    }

    function getErrorMessage(exception: unknown): string {
        return exception instanceof Error ? exception.message : 'The request could not be completed.';
    }
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
        <p>New quizzes begin as drafts. Add questions, then publish when the set is ready.</p>
    </section>

    <form class="quiz-form" onsubmit={(event) => { event.preventDefault(); createQuiz(); }}>
        <label>
            <span>Quiz title</span>
            <input bind:value={title} disabled={loading || saving} maxlength="200" placeholder="e.g. ITP Workshop: Objects" required />
        </label>
        <label>
            <span>Theme</span>
            <select bind:value={themeId} disabled={loading || saving || themes.length === 0} required>
                <option value="">{loading ? 'Loading themes…' : 'Select a theme'}</option>
                {#each themes as theme}<option value={theme.id}>{theme.name}</option>{/each}
            </select>
        </label>
        <div class="new-theme">
            <label>
                <span>Or create a theme</span>
                <input bind:value={newThemeName} disabled={loading || saving || creatingTheme} maxlength="100" placeholder="e.g. JavaScript fundamentals" />
            </label>
            <Button type="button" disabled={loading || saving || creatingTheme || !newThemeName.trim()} onclick={addTheme}>
                {creatingTheme ? 'Adding' : 'Add theme'}
            </Button>
        </div>
        <label>
            <span>Questions per game</span>
            <input bind:value={questionsPerGame} disabled={loading || saving} max="100" min="1" type="number" />
        </label>
        {#if error}<p class="error" role="alert">{error}</p>{/if}
        <div class="actions"><a href="/admin/quizes">Cancel</a><Button type="submit" disabled={loading || saving || themes.length === 0}>{saving ? 'Creating' : 'Create quiz'}</Button></div>
    </form>
</main>

<style>
    .admin-shell { margin: 0 auto; max-width: 920px; min-height: 100dvh; padding: 0 48px 64px; }
    .back-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
    .page-header { padding: 62px 0 32px; }
    .page-header h1 { font-family: var(--font-display); font-size: 3.25rem; font-weight: 400; line-height: 1; margin: 10px 0 12px; }
    .page-header p:not(.eyebrow) { color: var(--color-muted); line-height: 1.5; margin: 0; max-width: 560px; }
    .quiz-form { display: grid; gap: 22px; max-width: 560px; }
    .quiz-form label { display: grid; font-size: .875rem; font-weight: 800; gap: 8px; }
    input, select { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 48px; padding: 0 13px; }
    .new-theme { align-items: end; display: grid; gap: 12px; grid-template-columns: minmax(0, 1fr) auto; }
    .new-theme :global(.game-button) { min-width: 128px; }
    .error { color: #a32b1f; margin: 0; }
    .actions { align-items: center; display: flex; gap: 24px; justify-content: flex-end; margin-top: 12px; }
    .actions a { color: var(--color-muted); font-size: .875rem; font-weight: 800; }
    @media (max-width: 680px) { .admin-shell { padding: 0 20px 40px; } .page-header { padding-top: 46px; } .new-theme, .actions { align-items: stretch; grid-template-columns: 1fr; } .actions { flex-direction: column-reverse; } .actions :global(.game-button), .new-theme :global(.game-button) { width: 100%; } }
</style>
