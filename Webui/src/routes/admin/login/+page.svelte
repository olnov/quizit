<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
	import { signIn } from '$lib/auth/oidc';

	let error = $state('');

	async function startSignIn() {
		error = '';

		try {
			await signIn();
		} catch {
			error = 'We could not start sign-in. Check the authentication configuration.';
		}
	}
</script>

<svelte:head>
    <title>Admin Login | QuizIt</title>
</svelte:head>

<main class="page-shell">
    <section class="login-panel" aria-labelledby="login-title">
        <h1 id="login-title">Admin login</h1>
        <p class="intro">Sign in to create and manage quizzes.</p>

        <div class="login-form">
            <Button type="button" class="login-button" onclick={startSignIn}>Continue to sign in</Button>
            {#if error}
                <p class="error" role="alert">{error}</p>
            {/if}
        </div>
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

    :global(.login-button) {
        margin-top: 4px;
        width: 100%;
    }

    .error { color: #a32b1f; font-size: .875rem; margin: 0; }

    @media (max-width: 430px) {
        .login-panel {
            padding: 25px 21px;
        }
    }
</style>
