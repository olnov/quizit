export function generateNickName(): string {
	const adjectives = [
		'Swift',
		'Bright',
		'Lucky',
		'Brave',
		'Calm',
		'Happy',
		'Rapid',
		'Silent',
		'Clever',
		'Bold'
	];

	const spaceObjects = [
		'Comet',
		'Nova',
		'Orbit',
		'Meteor',
		'Galaxy',
		'Nebula',
		'Pulsar',
		'Cosmos',
		'Rocket',
		'Star'
	];

	const randomNumber = Math.floor(Math.random() * 100);
	const adjective = adjectives[Math.floor(Math.random() * adjectives.length)];
	const spaceObject = spaceObjects[Math.floor(Math.random() * spaceObjects.length)];

	return `${adjective}${spaceObject}${randomNumber}`;
}
