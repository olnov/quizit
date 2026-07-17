<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		type = 'button',
		class: className = '',
		children,
		...rest
	}: {
		type?: 'button' | 'submit' | 'reset';
		class?: string;
		children?: Snippet;
	} & Record<string, unknown> = $props();
</script>

<button {type} class={`game-button ${className}`} {...rest}>
	{@render children?.()}
</button>
