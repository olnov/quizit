<script lang="ts">
    import { onMount } from 'svelte';

    let { code }: { code: string } = $props();
    let container = $state<HTMLPreElement>();
    let hasMoreBelow = $state(false);

    function updateScrollState() {
        if (!container) return;

        hasMoreBelow = container.scrollHeight > container.clientHeight
            && container.scrollTop + container.clientHeight < container.scrollHeight - 2;
    }

    function handleScroll() {
        updateScrollState();
    }

    function scheduleScrollStateUpdate() {
        requestAnimationFrame(() => requestAnimationFrame(updateScrollState));
    }

    onMount(() => {
        const observer = new ResizeObserver(scheduleScrollStateUpdate);
        if (container) observer.observe(container);
        scheduleScrollStateUpdate();

        return () => observer.disconnect();
    });

    $effect(() => {
        code;
        scheduleScrollStateUpdate();
    });
</script>

<div class="code-context-wrap">
    <pre bind:this={container} class="code-context" onscroll={handleScroll}><code>{code}</code></pre>
    {#if hasMoreBelow}
        <div class="more-indicator" aria-hidden="true">More lines below &#8595;</div>
    {/if}
</div>

<style>
    .code-context-wrap {
        margin: 16px 0;
        position: relative;
    }

    .code-context {
        background: var(--color-ink);
        border-left: 5px solid var(--color-lime);
        color: #f7f7f2;
        font:
            0.75rem/1.42 ui-monospace,
            SFMono-Regular,
            Consolas,
            monospace;
        margin: 0;
        max-height: 180px;
        overflow: auto;
        padding: 13px 16px;
        white-space: pre;
    }

    .more-indicator {
        bottom: 0;
        color: var(--color-lime);
        font-size: 0.67rem;
        font-weight: 800;
        left: 5px;
        letter-spacing: 0.08em;
        padding: 7px 16px;
        pointer-events: none;
        position: absolute;
        right: 0;
        text-align: center;
        text-transform: uppercase;
    }
</style>
