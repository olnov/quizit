<script lang="ts">
    import { beforeNavigate, goto } from '$app/navigation';
    import { page } from '$app/state';
    import { onMount } from 'svelte';
    import AdminConfirmDialog from '$lib/components/AdminConfirmDialog.svelte';
    import Button from '$lib/components/ui/Button.svelte';
    import {
        archiveAdminQuiz,
        deleteAdminQuiz,
        getAdminQuiz,
        getQuizThemes,
        publishAdminQuiz,
        quizStatusLabel,
        updateAdminQuiz,
        type AdminQuestion,
        type AdminQuiz,
        type QuizTheme
    } from '$lib/admin-api';

    type EditableQuestion = Omit<AdminQuestion, 'id'> & { id?: string; clientId: string };
    type EditableQuiz = Omit<AdminQuiz, 'questions'> & { questions: EditableQuestion[] };
    type Confirmation = 'archive' | 'delete' | 'navigate';

    let quiz = $state<EditableQuiz | null>(null);
    let themes = $state<QuizTheme[]>([]);
    let selectedQuestionId = $state<string | null>(null);
    let loading = $state(true);
    let saving = $state(false);
    let dirty = $state(false);
    let error = $state('');
    let saveMessage = $state('');
    let confirmation = $state<Confirmation | null>(null);
    let pendingPath = $state<string | null>(null);

    const quizId = $derived(page.params.quizId ?? '');
    const selectedQuestion = $derived(quiz?.questions.find((question) => question.clientId === selectedQuestionId) ?? null);

    beforeNavigate((navigation) => {
        const destination = navigation.to?.url.pathname;
        if (dirty && !saving && destination && destination !== page.url.pathname) {
            navigation.cancel();
            pendingPath = `${destination}${navigation.to?.url.search ?? ''}`;
            confirmation = 'navigate';
        }
    });

    onMount(() => {
        const onBeforeUnload = (event: BeforeUnloadEvent) => {
            if (!dirty) return;
            event.preventDefault();
            event.returnValue = '';
        };

        window.addEventListener('beforeunload', onBeforeUnload);
        void loadEditor();
        return () => window.removeEventListener('beforeunload', onBeforeUnload);
    });

    async function loadEditor() {
        loading = true;
        error = '';
        try {
            const [loadedQuiz, loadedThemes] = await Promise.all([getAdminQuiz(quizId), getQuizThemes()]);
            quiz = toEditableQuiz(loadedQuiz);
            themes = loadedThemes;
            selectedQuestionId = quiz.questions[0]?.clientId ?? null;
            dirty = false;
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            loading = false;
        }
    }

    function markDirty() {
        dirty = true;
        saveMessage = '';
    }

    function addQuestion() {
        if (!quiz) return;
        const question: EditableQuestion = {
            clientId: crypto.randomUUID(),
            text: '',
            codeContext: null,
            explanation: null,
            difficulty: 100,
            options: ['', '', '', ''],
            correctOptionIndex: 0
        };
        quiz.questions.push(question);
        selectedQuestionId = question.clientId;
        markDirty();
    }

    function removeSelectedQuestion() {
        if (!quiz || !selectedQuestion) return;
        const index = quiz.questions.findIndex((question) => question.clientId === selectedQuestion.clientId);
        quiz.questions.splice(index, 1);
        selectedQuestionId = quiz.questions[Math.max(0, index - 1)]?.clientId ?? null;
        markDirty();
    }

    async function saveQuiz() {
        if (!quiz) return;
        error = '';
        saveMessage = '';
        saving = true;
        try {
            const savedQuiz = await updateAdminQuiz(quizId, {
                title: quiz.title,
                themeId: quiz.themeId,
                questionsPerGame: quiz.questionsPerGame,
                questions: quiz.questions.map(({ clientId: _, ...question }) => question)
            });
            quiz = toEditableQuiz(savedQuiz);
            selectedQuestionId = quiz.questions.find((question) => question.clientId === selectedQuestionId)?.clientId
                ?? quiz.questions[0]?.clientId
                ?? null;
            dirty = false;
            saveMessage = `Saved ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        } catch (exception) {
            error = getErrorMessage(exception);
        } finally {
            saving = false;
        }
    }

    async function publishQuiz() {
        error = '';
        if (dirty) {
            error = 'Save changes before publishing this quiz.';
            return;
        }

        try {
            quiz = toEditableQuiz(await publishAdminQuiz(quizId));
            saveMessage = 'Quiz published.';
        } catch (exception) {
            error = getErrorMessage(exception);
        }
    }

    async function confirmAction() {
        const action = confirmation;
        confirmation = null;

        if (action === 'navigate') {
            dirty = false;
            if (pendingPath) await goto(pendingPath);
            pendingPath = null;
            return;
        }

        if (!quiz) return;
        error = '';
        try {
            if (action === 'archive') {
                quiz = toEditableQuiz(await archiveAdminQuiz(quizId));
                saveMessage = 'Quiz archived.';
            }
            if (action === 'delete') {
                await deleteAdminQuiz(quizId);
                dirty = false;
                await goto('/admin/quizes');
            }
        } catch (exception) {
            error = getErrorMessage(exception);
        }
    }

    function toEditableQuiz(value: AdminQuiz): EditableQuiz {
        return {
            ...value,
            questions: value.questions.map((question) => ({ ...question, options: [...question.options], clientId: question.id }))
        };
    }

    function getErrorMessage(exception: unknown): string {
        return exception instanceof Error ? exception.message : 'The request could not be completed.';
    }
</script>

<svelte:head>
    <title>{quiz?.title ?? 'Quiz Editor'} | QuizIt Admin</title>
</svelte:head>

<main class="admin-shell">
    <header class="topbar">
        <a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a>
        <a class="back-link" href="/admin/quizes">Back to quizzes</a>
    </header>

    {#if loading}
        <p class="loading">Loading quiz editor…</p>
    {:else if !quiz}
        <section class="failure"><h1>Quiz unavailable</h1><p>{error || 'The requested quiz could not be loaded.'}</p><a href="/admin/quizes">Return to quizzes</a></section>
    {:else}
        <section class="editor-header">
            <div>
                <p class="eyebrow">Quiz editor · {quizStatusLabel[quiz.status]}</p>
                <input class="title-input" bind:value={quiz.title} maxlength="200" oninput={markDirty} aria-label="Quiz title" />
                <p class="save-state" class:unsaved={dirty}>{error || saveMessage || (dirty ? 'Unsaved changes' : 'All changes saved')}</p>
            </div>
            <div class="editor-actions">
                {#if quiz.status !== 1}<button type="button" class="text-action" onclick={publishQuiz}>Publish</button>{/if}
                {#if quiz.status !== 2}<button type="button" class="text-action" onclick={() => confirmation = 'archive'}>Archive</button>{/if}
                <button type="button" class="text-action danger" onclick={() => confirmation = 'delete'}>Delete</button>
                <Button type="button" disabled={saving || !dirty} onclick={saveQuiz}>{saving ? 'Saving' : 'Save changes'}</Button>
            </div>
        </section>

        <section class="quiz-settings" aria-label="Quiz settings">
            <label><span>Theme</span><select bind:value={quiz.themeId} disabled={quiz.questions.length > 0} onchange={markDirty}>{#each themes as theme}<option value={theme.id}>{theme.name}</option>{/each}</select></label>
            <label><span>Questions per game</span><input bind:value={quiz.questionsPerGame} max="100" min="1" onchange={markDirty} type="number" /></label>
            {#if quiz.questions.length > 0}<p>Theme is locked while this quiz has questions because questions belong to the shared theme bank.</p>{/if}
        </section>

        <div class="editor-layout">
            <aside class="question-list">
                <div class="list-title"><span>Questions ({quiz.questions.length})</span><button type="button" aria-label="Add question" onclick={addQuestion}>+</button></div>
                {#if quiz.questions.length === 0}<p class="no-questions">Add the first question to start the quiz.</p>{/if}
                {#each quiz.questions as question, index (question.clientId)}
                    <button class:active={selectedQuestionId === question.clientId} type="button" onclick={() => selectedQuestionId = question.clientId}><span>{String(index + 1).padStart(2, '0')}</span><strong>{question.text || 'Untitled question'}</strong><small>{question.difficulty}</small></button>
                {/each}
            </aside>

            {#if selectedQuestion}
                <form class="question-form" onsubmit={(event) => { event.preventDefault(); saveQuiz(); }}>
                    <div class="question-topline"><p class="eyebrow">Question</p><button class="delete-question" onclick={removeSelectedQuestion} type="button">Remove question</button></div>
                    <label><span>Difficulty</span><input bind:value={selectedQuestion.difficulty} max="1000" min="0" oninput={markDirty} step="100" type="number" /></label>
                    <label><span>Question</span><textarea bind:value={selectedQuestion.text} maxlength="1000" oninput={markDirty} placeholder="Write the question players will see" rows="3"></textarea></label>
                    <label><span>Code context</span><textarea bind:value={selectedQuestion.codeContext} class="code-field" maxlength="5000" oninput={markDirty} placeholder="Optional code or supporting context" rows="8"></textarea></label>
                    <fieldset><legend>Answer options</legend><p class="field-hint">Select the correct answer, then write all four options.</p><div class="options">{#each selectedQuestion.options as _, index}<label><input bind:group={selectedQuestion.correctOptionIndex} onchange={markDirty} name="correctOption" type="radio" value={index} /><span>{String.fromCharCode(65 + index)}</span><input bind:value={selectedQuestion.options[index]} maxlength="1000" oninput={markDirty} placeholder={`Option ${String.fromCharCode(65 + index)}`} /></label>{/each}</div></fieldset>
                    <label><span>Explanation</span><textarea bind:value={selectedQuestion.explanation} maxlength="2000" oninput={markDirty} placeholder="Explain the correct answer after the reveal" rows="4"></textarea></label>
                </form>
            {:else}
                <section class="question-empty"><h2>No question selected</h2><p>Add a question to start building the round.</p><Button type="button" onclick={addQuestion}>Add question</Button></section>
            {/if}
        </div>
    {/if}
</main>

<AdminConfirmDialog
    open={confirmation !== null}
    title={confirmation === 'delete' ? 'Delete this quiz?' : confirmation === 'archive' ? 'Archive this quiz?' : 'Discard unsaved changes?'}
    message={confirmation === 'delete' ? 'This removes the quiz from the active library. Quizzes with game history cannot be deleted.' : confirmation === 'archive' ? 'Archived quizzes cannot be selected for new games.' : 'Your latest edits will be lost.'}
    confirmLabel={confirmation === 'delete' ? 'Delete quiz' : confirmation === 'archive' ? 'Archive quiz' : 'Discard changes'}
    destructive={confirmation === 'delete'}
    onconfirm={confirmAction}
    oncancel={() => { confirmation = null; pendingPath = null; }}
/>

<style>
    .admin-shell { margin: 0 auto; max-width: 1280px; min-height: 100dvh; padding: 0 48px 64px; }
    .back-link { color: var(--color-muted); font-size: .82rem; font-weight: 800; text-decoration: none; text-transform: uppercase; }
    .loading, .failure { padding: 80px 0; }
    .failure h1 { font-family: var(--font-display); font-size: 3rem; font-weight: 400; margin: 0 0 12px; }
    .failure p { color: var(--color-muted); }
    .editor-header { align-items: end; display: flex; justify-content: space-between; gap: 24px; padding: 52px 0 24px; }
    .title-input { background: transparent; border: 0; border-bottom: 1px solid transparent; color: var(--color-ink); font-family: var(--font-display); font-size: 3rem; font-weight: 400; line-height: 1; margin-top: 9px; padding: 0; width: min(680px, 100%); }
    .title-input:focus { border-bottom-color: var(--color-accent); outline: 0; }
    .save-state { color: var(--color-muted); font-size: .82rem; margin: 10px 0 0; }
    .save-state.unsaved { color: var(--color-accent); font-weight: 800; }
    .editor-actions { align-items: center; display: flex; flex-wrap: wrap; gap: 14px; justify-content: flex-end; }
    .text-action { background: transparent; border: 0; color: var(--color-muted); cursor: pointer; font: inherit; font-size: .78rem; font-weight: 800; padding: 8px; text-decoration: underline; text-transform: uppercase; }
    .text-action:hover { color: var(--color-ink); }
    .text-action.danger:hover { color: #a32b1f; }
    .quiz-settings { align-items: end; border-bottom: 1px solid var(--color-border); border-top: 1px solid var(--color-border); display: flex; gap: 18px; padding: 16px 0; }
    .quiz-settings label { display: grid; font-size: .72rem; font-weight: 800; gap: 6px; letter-spacing: .08em; text-transform: uppercase; }
    .quiz-settings p { color: var(--color-muted); font-size: .78rem; line-height: 1.4; margin: 0; max-width: 360px; }
    input, select, textarea { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 46px; padding: 10px 13px; resize: vertical; width: 100%; }
    .quiz-settings select { min-width: 240px; padding: 0 12px; }
    .quiz-settings input { width: 144px; }
    .editor-layout { border-top: 1px solid var(--color-border); display: grid; grid-template-columns: 260px minmax(0, 1fr); margin-top: 28px; }
    .question-list { border-right: 1px solid var(--color-border); max-height: calc(100dvh - 255px); overflow: auto; padding: 18px 18px 18px 0; position: sticky; top: 0; }
    .list-title { align-items: center; color: var(--color-muted); display: flex; font-size: .74rem; font-weight: 800; justify-content: space-between; letter-spacing: .08em; margin-bottom: 13px; text-transform: uppercase; }
    .list-title button { background: transparent; border: 1px solid var(--color-border); color: var(--color-ink); cursor: pointer; font-size: 1.2rem; height: 28px; line-height: 1; width: 28px; }
    .question-list > button { align-items: center; background: transparent; border: 1px solid var(--color-border); color: var(--color-ink); cursor: pointer; display: grid; gap: 9px; grid-template-columns: 28px minmax(0, 1fr) 30px; margin-bottom: 8px; padding: 11px 9px; text-align: left; width: 100%; }
    .question-list > button.active { background: #e2eafc; border-color: var(--color-accent); }
    .question-list span, .question-list small { color: var(--color-muted); font-size: .72rem; font-weight: 800; }
    .question-list strong { font-size: .84rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .no-questions { color: var(--color-muted); font-size: .83rem; line-height: 1.45; }
    .question-form { display: grid; gap: 22px; padding: 22px 0 22px 36px; }
    .question-topline { align-items: center; display: flex; justify-content: space-between; }
    .delete-question { background: transparent; border: 0; color: #a32b1f; cursor: pointer; font: inherit; font-size: .78rem; font-weight: 800; text-decoration: underline; }
    .question-form label { display: grid; font-size: .875rem; font-weight: 800; gap: 8px; }
    .question-form label:first-of-type { max-width: 160px; }
    .code-field { background: var(--color-ink); color: #f7f7f2; font-family: var(--font-mono); font-size: .84rem; line-height: 1.5; }
    fieldset { border: 0; margin: 0; padding: 0; }
    legend { font-size: .875rem; font-weight: 800; margin-bottom: 4px; }
    .field-hint { color: var(--color-muted); font-size: .8rem; margin: 0 0 10px; }
    .options { display: grid; gap: 10px; grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .options label { align-items: center; border: 1px solid var(--color-border); display: grid; gap: 9px; grid-template-columns: auto 24px minmax(0, 1fr); padding: 8px; }
    .options input[type='radio'] { accent-color: var(--color-accent); min-height: auto; width: auto; }
    .options span { align-items: center; background: var(--color-ink); color: var(--color-surface); display: inline-flex; font-size: .72rem; height: 24px; justify-content: center; }
    .options input:not([type='radio']) { border: 0; min-height: 32px; padding: 0; }
    .question-empty { align-items: start; display: flex; flex-direction: column; justify-content: center; min-height: 420px; padding: 36px; }
    .question-empty h2 { font-family: var(--font-display); font-size: 2rem; font-weight: 400; margin: 0 0 8px; }
    .question-empty p { color: var(--color-muted); margin: 0 0 20px; }
    @media (max-width: 820px) { .admin-shell { padding: 0 20px 40px; } .editor-header { align-items: stretch; flex-direction: column; } .editor-layout { grid-template-columns: 1fr; } .question-list { border-bottom: 1px solid var(--color-border); border-right: 0; max-height: 300px; padding: 18px 0; position: static; } .question-form { padding: 26px 0; } }
    @media (max-width: 560px) { .title-input { font-size: 2.35rem; } .quiz-settings { align-items: stretch; flex-direction: column; } .quiz-settings select, .quiz-settings input { width: 100%; } .options { grid-template-columns: 1fr; } .editor-actions :global(.game-button) { width: 100%; } }
</style>
