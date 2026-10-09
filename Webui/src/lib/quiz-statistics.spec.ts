import { describe, expect, it } from 'vitest';
import { formatDuration, toStatisticsInterval } from './quiz-statistics';

describe('toStatisticsInterval', () => {
	it('includes both selected local calendar dates', () => {
		const interval = toStatisticsInterval('2026-10-01', '2026-10-02');
		expect(interval.from).toBe(new Date(2026, 9, 1).toISOString());
		expect(interval.to).toBe(new Date(2026, 9, 3).toISOString());
	});

	it('rejects reversed dates', () => {
		expect(() => toStatisticsInterval('2026-10-03', '2026-10-02')).toThrow();
	});
});

describe('formatDuration', () => {
	it('formats seconds as a readable duration', () => {
		expect(formatDuration(90)).toBe('1m 30s');
		expect(formatDuration(3661)).toBe('1h 1m 1s');
	});

	it('shows unavailable timing clearly', () => {
		expect(formatDuration(null)).toBe('No data');
	});
});
