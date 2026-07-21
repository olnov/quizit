export function generateNickName(): string {
	const animals = [
		'Dog',
		'Cat',
		'Bird',
		'Fish',
		'Rabbit',
		'Hamster',
		'Turtle',
		'Snake',
		'Lizard',
		'Monkey'
	];
	const colors = [
		'Red',
		'Blue',
		'Green',
		'Yellow',
		'Purple',
		'Orange',
		'Pink',
		'Brown',
		'Black',
		'White'
  ];
  const randomNummber = Math.floor(Math.random() * 100);
	const animal = animals[Math.floor(Math.random() * animals.length)];
	const color = colors[Math.floor(Math.random() * colors.length)];

	return `${color}${animal}${randomNummber}`;
}
