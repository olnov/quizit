import type { PlayerStatistics } from './game-room';

const pageWidth = 842;
const pageHeight = 595;
const margin = 30;
const tableWidth = pageWidth - margin * 2;
const columnWidths = [32, 210, 110, 110, 85, 235];
const headers = [
	'#',
	'Question',
	'My answer',
	'Correct answer',
	'Answered correctly',
	'Explanation'
];
const tableTop = 161;
const tableBottom = pageHeight - 44;
const cellPadding = 7;
const lineHeight = 13;
const encoder = new TextEncoder();

type PdfPart = string | Uint8Array;

export function createImagePdf(pages: Uint8Array[]): Uint8Array {
	if (!pages.length) throw new Error('The report has no pages.');

	const chunks: Uint8Array[] = [];
	const offsets = [0];
	let length = 0;
	const append = (part: PdfPart) => {
		const bytes = typeof part === 'string' ? encoder.encode(part) : part;
		chunks.push(bytes);
		length += bytes.length;
	};
	const object = (id: number, ...parts: PdfPart[]) => {
		offsets[id] = length;
		append(`${id} 0 obj\n`);
		parts.forEach(append);
		append('\nendobj\n');
	};

	append('%PDF-1.4\n');
	object(1, '<< /Type /Catalog /Pages 2 0 R >>');
	const pageIds = pages.map((_, index) => 3 + index * 3);
	object(
		2,
		`<< /Type /Pages /Kids [${pageIds.map((id) => `${id} 0 R`).join(' ')}] /Count ${pages.length} >>`
	);

	for (const [index, image] of pages.entries()) {
		const pageId = pageIds[index];
		const imageId = pageId + 1;
		const contentId = pageId + 2;
		object(
			pageId,
			`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] /Resources << /XObject << /Im0 ${imageId} 0 R >> >> /Contents ${contentId} 0 R >>`
		);
		object(
			imageId,
			`<< /Type /XObject /Subtype /Image /Width ${pageWidth * 2} /Height ${pageHeight * 2} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length ${image.length} >>\nstream\n`,
			image,
			'\nendstream'
		);
		const drawing = `q ${pageWidth} 0 0 ${pageHeight} 0 0 cm /Im0 Do Q\n`;
		object(
			contentId,
			`<< /Length ${encoder.encode(drawing).length} >>\nstream\n${drawing}endstream`
		);
	}

	const xrefOffset = length;
	append(`xref\n0 ${offsets.length}\n0000000000 65535 f \n`);
	for (const offset of offsets.slice(1)) append(`${String(offset).padStart(10, '0')} 00000 n \n`);
	append(`trailer\n<< /Size ${offsets.length} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF\n`);

	const result = new Uint8Array(length);
	let position = 0;
	for (const chunk of chunks) {
		result.set(chunk, position);
		position += chunk.length;
	}
	return result;
}

function wrapText(context: CanvasRenderingContext2D, value: string, width: number): string[] {
	const words = value.trim().split(/\s+/);
	const lines: string[] = [];
	let line = '';
	for (const word of words) {
		const candidate = line ? `${line} ${word}` : word;
		if (context.measureText(candidate).width <= width) {
			line = candidate;
			continue;
		}
		if (line) lines.push(line);
		line = '';
		for (const character of word) {
			if (context.measureText(line + character).width > width && line) {
				lines.push(line);
				line = '';
			}
			line += character;
		}
	}
	if (line) lines.push(line);
	return lines.length ? lines : [''];
}

async function renderStatisticsPages(
	statistics: PlayerStatistics,
	gameCode: string,
	playerName: string
): Promise<Uint8Array[]> {
	const pages: HTMLCanvasElement[] = [];
	const correctCount = statistics.rows.filter((row) => row.isCorrect).length;
	let context!: CanvasRenderingContext2D;
	let y = tableTop;

	const startPage = () => {
		const canvas = document.createElement('canvas');
		canvas.width = pageWidth * 2;
		canvas.height = pageHeight * 2;
		const drawingContext = canvas.getContext('2d');
		if (!drawingContext) throw new Error('Unable to create the PDF report.');
		context = drawingContext;
		context.scale(2, 2);
		context.fillStyle = '#f4f2ed';
		context.fillRect(0, 0, pageWidth, pageHeight);
		context.fillStyle = '#e14d36';
		context.font = 'bold 11px Arial, sans-serif';
		context.fillText('QUIZIT  /  PERSONAL REPORT', margin, 32);
		context.fillStyle = '#6d717b';
		context.font = '10px Arial, sans-serif';
		context.fillText(`Game ${gameCode}  ·  ${new Date().toLocaleDateString()}`, margin, 51);
		context.fillStyle = '#20232d';
		context.font = '27px Georgia, serif';
		context.fillText('My statistics', margin, 86);
		context.font = 'bold 12px Arial, sans-serif';
		context.fillText(playerName, margin, 110, tableWidth - 205);
		context.textAlign = 'right';
		context.fillText(
			`${statistics.score} pts  ·  ${correctCount}/${statistics.rows.length} correct`,
			pageWidth - margin,
			110
		);
		context.textAlign = 'left';

		let x = margin;
		context.fillStyle = '#20232d';
		context.fillRect(margin, 127, tableWidth, 34);
		for (const [index, width] of columnWidths.entries()) {
			context.fillStyle = '#fffefa';
			context.font = 'bold 9px Arial, sans-serif';
			const lines = wrapText(context, headers[index], width - cellPadding * 2);
			lines.forEach((text, lineIndex) =>
				context.fillText(text, x + cellPadding, 141 + lineIndex * 11)
			);
			x += width;
		}
		pages.push(canvas);
		y = tableTop;
	};

	startPage();
	for (const [rowIndex, row] of statistics.rows.entries()) {
		const values = [
			String(rowIndex + 1),
			row.question,
			row.playerAnswer ?? 'No answer',
			row.correctAnswer,
			row.isCorrect ? 'Yes' : 'No',
			row.explanation ?? '—'
		];
		const cells = values.map((value, index) => {
			context.font = index === 1 ? 'bold 10px Arial, sans-serif' : '10px Arial, sans-serif';
			return wrapText(context, value, columnWidths[index] - cellPadding * 2);
		});
		const rowLines = Math.max(...cells.map((cell) => cell.length));
		const fullHeight = rowLines * lineHeight + cellPadding * 2;
		if (fullHeight > tableBottom - y && fullHeight <= tableBottom - tableTop) startPage();

		let lineOffset = 0;
		while (lineOffset < rowLines) {
			const availableLines = Math.floor((tableBottom - y - cellPadding * 2) / lineHeight);
			if (availableLines < 1) {
				startPage();
				continue;
			}
			const count = Math.min(availableLines, rowLines - lineOffset);
			const height = count * lineHeight + cellPadding * 2;
			context.fillStyle = rowIndex % 2 === 0 ? '#fffefa' : '#f9f7f1';
			context.fillRect(margin, y, tableWidth, height);
			let x = margin;
			for (const [index, width] of columnWidths.entries()) {
				if (index === 4) {
					context.fillStyle = row.isCorrect ? '#ccdc76' : '#f5c9c2';
					context.fillRect(x, y, width, height);
				}
				context.fillStyle = '#dedbd3';
				context.fillRect(x, y, 1, height);
				context.fillStyle = '#20232d';
				context.font = index === 1 ? 'bold 10px Arial, sans-serif' : '10px Arial, sans-serif';
				const visible = cells[index].slice(lineOffset, lineOffset + count);
				if (index === 0 && lineOffset > 0) visible[0] = String(rowIndex + 1);
				visible.forEach((text, lineIndex) =>
					context.fillText(
						text,
						x + cellPadding,
						y + cellPadding + 10 + lineIndex * lineHeight,
						width - cellPadding * 2
					)
				);
				x += width;
			}
			context.fillStyle = '#dedbd3';
			context.fillRect(margin + tableWidth - 1, y, 1, height);
			context.fillRect(margin, y + height - 1, tableWidth, 1);
			y += height;
			lineOffset += count;
			if (lineOffset < rowLines) startPage();
		}
	}

	for (const [index, canvas] of pages.entries()) {
		const footer = canvas.getContext('2d');
		if (!footer) continue;
		footer.save();
		footer.fillStyle = '#6d717b';
		footer.font = '10px Arial, sans-serif';
		footer.fillText(`Page ${index + 1} of ${pages.length}`, margin, pageHeight - 22);
		footer.restore();
	}

	const images: Uint8Array[] = [];
	for (const canvas of pages) {
		const blob = await new Promise<Blob>((resolve, reject) => {
			canvas.toBlob(
				(result) =>
					result ? resolve(result) : reject(new Error('Unable to render the PDF report.')),
				'image/jpeg',
				0.92
			);
		});
		images.push(new Uint8Array(await blob.arrayBuffer()));
		canvas.width = 0;
		canvas.height = 0;
	}
	return images;
}

export async function downloadStatisticsPdf(
	statistics: PlayerStatistics,
	gameCode: string,
	playerName: string
): Promise<void> {
	const images = await renderStatisticsPages(statistics, gameCode, playerName);
	const pdf = createImagePdf(images);
	const blob = new Blob([pdf as BlobPart], { type: 'application/pdf' });
	const url = URL.createObjectURL(blob);
	const link = document.createElement('a');
	link.href = url;
	link.download = `quizit-statistics-${gameCode}.pdf`;
	document.body.append(link);
	link.click();
	link.remove();
	setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
