<script lang="ts">
    import { goto } from '$app/navigation';
    import { onMount } from 'svelte';
    import { completeSignIn } from '$lib/auth/oidc';

    let error = $state('');

    onMount(async () => {
        try {
            await completeSignIn();
            await goto('/admin/quizes');
        } catch {
            error = 'We could not complete sign-in. Return to login and try again.';
        }
    });
</script>

<svelte:head>
    <title>Signing in | QuizIt</title>
</svelte:head>

<main class="page-shell">
    <section class="login-panel">
        <h1>{error ? 'Sign-in failed' : 'Signing in'}</h1>
        <p>{error || 'Completing your secure sign-in.'}</p>
        {#if error}
            <a href="/admin/login">Return to login</a>
        {/if}
    </section>
</main>

<style>
    .page-shell { align-items: center; display: flex; justify-content: center; margin: 0; max-width: none; min-height: 100dvh; padding: 32px 20px; }
    .login-panel { background: var(--color-surface); border: 2px solid var(--color-ink); max-width: 420px; padding: 32px; width: 100%; }
    h1 { font-size: 2.2rem; line-height: 1; margin: 8px 0 10px; }
    p { color: var(--color-muted); line-height: 1.5; margin: 0; }
    a { display: inline-block; font-weight: 800; margin-top: 20px; }
</style>
