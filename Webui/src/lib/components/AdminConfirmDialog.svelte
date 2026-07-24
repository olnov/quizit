<script lang="ts">
    import Button from '$lib/components/ui/Button.svelte';

    let {
        open = false,
        title,
        message,
        confirmLabel = 'Confirm',
        destructive = false,
        onconfirm,
        oncancel
    }: {
        open?: boolean;
        title: string;
        message: string;
        confirmLabel?: string;
        destructive?: boolean;
        onconfirm: () => void;
        oncancel: () => void;
    } = $props();
</script>

{#if open}
    <div class="game-dialog-overlay" role="presentation" onclick={oncancel}></div>
    <dialog class="game-dialog" aria-labelledby="dialog-title" open>
        <p class="eyebrow">Confirmation</p>
        <h2 id="dialog-title">{title}</h2>
        <p>{message}</p>
        <div class="actions">
            <button class="cancel" type="button" onclick={oncancel}>Cancel</button>
            <Button class={destructive ? 'destructive' : ''} type="button" onclick={onconfirm}>{confirmLabel}</Button>
        </div>
    </dialog>
{/if}

<style>
    h2 { font-family: var(--font-display); font-size: 2rem; font-weight: 400; line-height: 1; margin: 10px 0 12px; }
    p:not(.eyebrow) { color: var(--color-muted); line-height: 1.5; margin: 0; }
    .actions { display: flex; gap: 18px; justify-content: flex-end; margin-top: 28px; }
    .cancel { background: transparent; border: 0; color: var(--color-muted); cursor: pointer; font-weight: 800; padding: 12px; }
    :global(.destructive) { background: #fde0db; }
    @media (max-width: 460px) { .actions { align-items: stretch; flex-direction: column-reverse; } :global(.game-button) { width: 100%; } }
</style>
