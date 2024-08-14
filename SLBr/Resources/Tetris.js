const canvas = document.querySelector('canvas')
const ctx = canvas.getContext('2d')

let config = {
	map_width: canvas.width,
	map_height: canvas.height,
	rows: 20,
	cols: 10,
	stroke: 'gainsboro',
	bg: '#fff',
	border_size: 0.25
}
config.size = config.map_width / config.cols
canvas.style.outline = `${config.border_size}px solid ${config.stroke}`
canvas.style.borderRadius = `5px`
let prev_time = 0,
	prev_pos = { x: 0, y: 0 },
	prev_block = [[]],
	move_down = 500,
	fps = 100,
	frame_count = 0,
	score = 0

const colors = [
	'limegreen',
	'darkorange',
	'mediumorchid',
	'dodgerblue',
	'orangered',
	'cornflowerblue',
	'tomato'
]

const types = {
	'z': [
		[1, 1, 0],
		[0, 1, 1],
		[0, 0, 0]
	],
	's': [
		[0, 2, 2],
		[2, 2, 0],
		[0, 0, 0]
	],
	'i': [
		[0, 3, 0, 0],
		[0, 3, 0, 0],
		[0, 3, 0, 0],
		[0, 3, 0, 0]
	],
	'l': [
		[4, 0, 0],
		[4, 0, 0],
		[4, 4, 0]
	],
	'j': [
		[0, 0, 5],
		[0, 0, 5],
		[0, 5, 5]
	],
	'o': [
		[6, 6],
		[6, 6]
	],
	't': [
		[0, 7, 0],
		[7, 7, 7],
		[0, 0, 0]
	]
}


class Block {
	constructor(cells, x, y) {
		this.prevent = true
		this.cells = cells
		this.position = { x, y }
		this.alive = true
	}
	collision(field) {
		const { x, y } = this.position
		this.cells.forEach((row, i) => {
			row.forEach((cell, j) => {
				if (cell && ((y + i >= config.rows) || field[y + i][x + j])) {
					this.alive = false
					return
				}
			})
		})
	}
	move(e) {
		switch (e.key) {
			case 'a':
			case 'ArrowLeft':
				this.position.x--
				break
			case 'd':
			case 'ArrowRight':
				this.position.x++
				break
			case 's':
			case 'ArrowDown':
				if (this.position.y + this.cells.length < config.rows) this.position.y++
				break
			case 'w':
			case 'ArrowUp':
				this.rotate()
				break
		}
	}
	rotate() {
		let altered = []
		for (let i = 0; i < this.cells.length; i++) {
			altered[i] = []
			for (let j = 0; j < this.cells.length; j++) {
				altered[i][j] = this.cells[this.cells.length - 1 - j][i]
			}
		}
		this.cells = altered
	}
}

const checkMove = (block, field) => {
	const { cells, position } = block
	const { x, y } = position
	return !cells.some((rows, i) => {
		return rows.some((cell, j) => {
			if ((cell && x + j < 0) || (cell && x + j >= config.cols) || (cell && field[y + i][x + j]))
				return true
		})
	})
}

const draw = (field, ctx) => {
	const { size } = config
	field.forEach((row, i) => {
		row.forEach((cell, j) => {
			ctx.fillStyle = cell ? colors[cell - 1] : config.bg
			ctx.strokeStyle = config.stroke
			ctx.lineWidth = config.border_size
			const args = [j * size, i * size, size, size]
			ctx.fillRect(...args)
			ctx.strokeRect(...args)

		})
	})
}

const render = (game, block, time) => {
	if (!block) {
		const arr_types = Object.values(types)
		const type = arr_types[arr_types.length * Math.random() | 0]
		const x = ((config.cols - type.length) / 2) | 0
		block = new Block(type, x, 0)
		prev_pos = { x, y: 0 }
		addEventListener('keydown', e => block.move.bind(block)(e))
	}
	const { ctx, field } = game
	const { position } = block

	if (time - prev_time > 1000 / fps) {
		frame_count++
		if (frame_count == Math.floor((fps * move_down) / 1000)) {
			frame_count = 0
			if (block && block.alive) position.y++
			else block = null
		}
	}

	prev_time = time

	insert(prev_block, field, prev_pos.y, prev_pos.x, true)

	if (!checkMove(block, field)) {
		position.x = prev_pos.x
		block.cells = prev_block
	}

	if (position.y > prev_pos.y)
		position.y = prev_pos.y + 1

	block.collision(field)

	if (block.alive) {
		insert(block.cells, field, position.y, position.x)
		draw(field, ctx)
		prev_pos = Object.assign({}, position)
		prev_block = [].concat(block.cells)
	}
	else if (prev_pos.y > block.cells.length - 1) {
		insert(block.cells, field, prev_pos.y, prev_pos.x)
		game.field = findFilled(field)
		draw(game.field, ctx)
		block = null
	}
	else {
		insert(prev_block, field, prev_pos.y, prev_pos.x)

		let last = block.cells.filter((row) => !row.every((cell) => !cell)).slice(-prev_pos.y)
		insert(last, field, 0, position.x)

		game.field = generate_field(config.rows + 4, config.cols)
		update_score(0)
		draw(game.field, ctx)

		block = null
	}

	window.requestAnimationFrame((time) => {
		render(game, block, time)
	})
}

const insert = (child, parent, row, col, clear) => {
	for (let i = 0; i < child.length; i++) {
		for (let j = 0; j < child.length; j++) {
			parent[row + i][col + j] = !clear ? child[i][j] ? child[i][j] : parent[row + i][col + j] : child[i][j] ? 0 : parent[row + i][col + j]
		}
	}
}

const update_score = amount => {
	score = amount
	document.getElementById('score').innerHTML = amount
}

const findFilled = field => {
	const filteredField = field.filter((row) => row.some((cell) => (cell === 0)))

	const diff = field.length - filteredField.length
	score += diff * 10
	update_score(score)

	let filled = generate_field(diff, config.cols)
	return [...filled, ...filteredField]
}

const generate_field = (rows, cols) => {
	return Array.from({ length: rows }, () => Array.from({ length: cols }, () => 0))
}

let game = { ctx, field: generate_field(config.rows + 4, config.cols) }

render(game)
