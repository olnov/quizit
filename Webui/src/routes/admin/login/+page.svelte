<script lang="ts">
    import { goto } from '$app/navigation';
    import Button from '$lib/components/ui/Button.svelte';
    import { signIn } from '$lib/auth/oidc';

    let username = $state('');
    let password = $state('');
    let error = $state('');
    let submitting = $state(false);

    async function submitLogin() {
        error = '';
        if (!username.trim() || !password) {
            error = 'Enter your username and password.';
            return;
        }

        submitting = true;
        try {
            await signIn(username.trim(), password);
            await goto('/admin/quizes');
        } catch (exception) {
            error = exception instanceof Error ? exception.message : 'We could not sign you in.';
        } finally {
            submitting = false;
        }
    }
</script>

<svelte:head>
    <title>Admin Login | QuizIt</title>
</svelte:head>

<main class="page-shell">
    <section class="login-panel" aria-labelledby="login-title">
        <p class="eyebrow">Quiz administration</p>
        <h1 id="login-title">Admin login</h1>
        <p class="intro">Sign in to create and manage quizzes.</p>

        <form class="login-form" onsubmit={(event) => { event.preventDefault(); submitLogin(); }}>
            <label><span>Username</span><input autocomplete="username" bind:value={username} disabled={submitting} name="username" required /></label>
            <label><span>Password</span><input autocomplete="current-password" bind:value={password} disabled={submitting} name="password" required type="password" /></label>
            {#if error}<p class="error" role="alert">{error}</p>{/if}
            <Button type="submit" class="login-button" disabled={submitting}>{submitting ? 'Signing in' : 'Sign in'}</Button>
        </form>
    </section>
</main>

<style>
    .page-shell { align-items: center; display: flex; justify-content: center; margin: 0; max-width: none; min-height: 100dvh; padding: 32px 20px; }
    .login-panel { background: var(--color-surface); border: 2px solid var(--color-ink); max-width: 420px; padding: 32px; width: 100%; }
    h1 { font-size: 2.2rem; line-height: 1; margin: 8px 0 10px; }
    .intro { color: var(--color-muted); line-height: 1.5; margin: 0; }
    .login-form { display: grid; gap: 18px; margin-top: 28px; }
    .login-form label { display: grid; font-size: .82rem; font-weight: 800; gap: 7px; }
    input { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 48px; padding: 0 13px; }
    :global(.login-button) { margin-top: 4px; width: 100%; }
    .error { color: #a32b1f; font-size: .875rem; margin: 0; }
    @media (max-width: 430px) { .login-panel { padding: 25px 21px; } }
</style>
