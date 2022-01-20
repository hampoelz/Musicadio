'use strict';

const express = require('express');
const uuid = require('uuid');
const socketIO = require('socket.io');

const PORT = process.env.PORT || 3000;
const INDEX = '/index.html';

const server = express()
	.use((_, res) => res.sendFile(INDEX, {
		root: __dirname
	}))
	.listen(PORT, () => console.log(`Listening on ${PORT}`));

const io = socketIO(server);
//io.origins(['https://music.hampoelz.net']);

var sockets = {};

io.on('connection', (socket) => {
	var id;

	do id = uuid.v4();
	while (sockets[id]);

	sockets[id] = socket;
	socket.emit('season-id', id);

	socket.on('disconnect', () => {

		Object.keys(sockets).forEach(id => {
			if (sockets[id] === socket) {
				io.sockets.emit('disconnected', id);
			}
		});

		sockets[socket] = undefined;
		delete sockets[socket];
	});

	socket.on('message', (message) => {
		if (sockets[message.to]) {
			sockets[message.to].emit('message', message);
		} else {
			socket.emit('disconnected', message.from);
		}
	});

	socket.on('logon', (message) => {
		if (sockets[message.to]) {
			sockets[message.to].emit('logon', message);
		} else {
			socket.emit('info', 'Not Found');
		}
	});
});