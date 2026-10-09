export function toStatisticsInterval(
	fromDate: string,
	throughDate: string
): { from: string; to: string } {
	if (
		!/^\d{4}-\d{2}-\d{2}$/.test(fromDate) ||
		!/^\d{4}-\d{2}-\d{2}$/.test(throughDate) ||
		fromDate > throughDate
	) {
		throw new Error('Choose a valid date range. The start date must be on or before the end date.');
	}
	const from = new Date(`${fromDate}T00:00:00`);
	const to = new Date(`${throughDate}T00:00:00`);
	if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime())) {
		throw new Error('Choose valid dates.');
	}
	to.setDate(to.getDate() + 1);
	return { from: from.toISOString(), to: to.toISOString() };
}

export function formatDuration(seconds: number | null): string {
	if (seconds === null || !Number.isFinite(seconds) || seconds < 0) return 'No data';
	const rounded = Math.round(seconds);
	const hours = Math.floor(rounded / 3600);
	const minutes = Math.floor((rounded % 3600) / 60);
	const remainingSeconds = rounded % 60;
	return [hours ? `${hours}h` : '', hours || minutes ? `${minutes}m` : '', `${remainingSeconds}s`]
		.filter(Boolean)
		.join(' ');
}
