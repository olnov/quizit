<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
</script>

<svelte:head>
    <title>Admin Login | QuizIt</title>
</svelte:head>

<main class="page-shell">
    <section class="login-panel" aria-labelledby="login-title">
        <h1 id="login-title">Admin login</h1>
        <p class="intro">Sign in to create and manage quizzes.</p>

        <form class="login-form">
            <label>
                <span>Username</span>
                <input type="text" name="username" autocomplete="username" />
            </label>
            <label>
                <span>Password</span>
                <input type="password" name="password" autocomplete="current-password" />
            </label>
            <Button type="submit" class="login-button">Login</Button>
        </form>
    </section>
</main>

<style>
    .page-shell {
        align-items: center;
        display: flex;
        justify-content: center;
        margin: 0;
        max-width: none;
        min-height: 100dvh;
        padding: 32px 20px;
    }

    .login-panel {
        background: var(--color-surface);
        border: 2px solid var(--color-ink);
        max-width: 420px;
        padding: 32px;
        width: 100%;
    }

    h1 {
        font-size: 2.2rem;
        line-height: 1;
        margin: 8px 0 10px;
    }

    .intro {
        color: var(--color-muted);
        line-height: 1.5;
        margin: 0;
    }

    .login-form {
        display: grid;
        gap: 18px;
        margin-top: 28px;
    }

    .login-form label {
        display: grid;
        font-size: 0.875rem;
        font-weight: 700;
        gap: 7px;
    }

    input {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        color: var(--color-ink);
        font: inherit;
        min-height: 46px;
        padding: 0 13px;
    }

    input:focus-visible {
        border-color: var(--color-accent);
        outline: 3px solid color-mix(in srgb, var(--color-accent) 30%, transparent);
        outline-offset: 1px;
    }

    :global(.login-button) {
        margin-top: 4px;
        width: 100%;
    }

    @media (max-width: 430px) {
        .login-panel {
            padding: 25px 21px;
        }
    }
</style>
