<script lang="ts">
	let {
		label,
		placeholder = '',
		value = $bindable(''),
		maxlength,
		className = '',
		type = 'text'
	}: {
		label: string;
		placeholder?: string;
		value?: string;
		maxlength?: number;
		className?: string;
		type?: 'text' | 'email';
	} = $props();
</script>

<label class={`field ${className}`}>
	<span>{label}</span>
	<input bind:value {placeholder} {maxlength} {type} autocomplete="off" />
</label>

<style>
	.field { display: grid; gap: 7px; color: var(--color-ink); font-size: 0.875rem; font-weight: 700; }
	input { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; font-size: 1rem; min-height: 46px; padding: 0 13px; width: 100%; }
	input::placeholder { color: var(--color-muted); }
	input:focus { border-color: var(--color-ink); outline: 3px solid var(--focus-ring); outline-offset: 0; }
</style>
