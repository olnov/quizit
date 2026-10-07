import { describe, expect, it } from 'vitest';
import { createImagePdf } from './pdf-report';

describe('createImagePdf', () => {
	it('writes a downloadable PDF with every image page', () => {
		const first = Uint8Array.from([0xff, 0xd8, 0xff, 0xd9]);
		const second = Uint8Array.from([0xff, 0xd8, 0x01, 0xff, 0xd9]);
		const pdf = createImagePdf([first, second]);
		const text = new TextDecoder('latin1').decode(pdf);

		expect(text.startsWith('%PDF-1.4')).toBe(true);
		expect(text).toContain('/Count 2');
		expect(text.match(/\/Subtype \/Image/g)).toHaveLength(2);
		expect(text).toContain('/Length 4');
		expect(text).toContain('/Length 5');
		expect(text.trimEnd().endsWith('%%EOF')).toBe(true);
	});
});
