const express = require('express');
const { WebSocketServer } = require('ws');
const { v4: uuidv4 } = require('uuid');
const http = require('http');

const app = express();
const server = http.createServer(app);
const wss = new WebSocketServer({ server });

const PORT = process.env.PORT || 3000;

// מרחק מקסימלי לשמיעה (ביחידות המשחק)
const PROXIMITY_DISTANCE = 2.5;

// חדרים: roomCode -> { players: Map<id, playerData> }
const rooms = new Map();

app.get('/health', (req, res) => {
  res.json({ status: 'ok', rooms: rooms.size });
});

wss.on('connection', (ws) => {
  const playerId = uuidv4();
  let playerRoom = null;
  let playerName = null;

  console.log(`[+] Player connected: ${playerId}`);

  ws.send(JSON.stringify({ type: 'connected', id: playerId }));

  ws.on('message', (data) => {
    let msg;
    try {
      msg = JSON.parse(data);
    } catch {
      return;
    }

    switch (msg.type) {

      // שחקן מצטרף לחדר
      case 'join': {
        const { roomCode, name } = msg;
        playerName = name || `Player_${playerId.slice(0, 4)}`;
        playerRoom = roomCode;

        if (!rooms.has(roomCode)) {
          rooms.set(roomCode, { players: new Map() });
        }

        const room = rooms.get(roomCode);
        room.players.set(playerId, {
          id: playerId,
          name: playerName,
          ws,
          x: 0,
          y: 0,
          isDead: false,
        });

        console.log(`[JOIN] ${playerName} joined room ${roomCode}`);

        // הודע לכולם בחדר על השחקן החדש
        broadcastToRoom(roomCode, {
          type: 'player_joined',
          id: playerId,
          name: playerName,
        }, playerId);

        // שלח לשחקן החדש את רשימת השחקנים הקיימים
        const existingPlayers = [];
        room.players.forEach((p) => {
          if (p.id !== playerId) {
            existingPlayers.push({ id: p.id, name: p.name, x: p.x, y: p.y });
          }
        });
        ws.send(JSON.stringify({ type: 'room_state', players: existingPlayers }));
        break;
      }

      // עדכון מיקום שחקן
      case 'position': {
        if (!playerRoom) return;
        const room = rooms.get(playerRoom);
        if (!room) return;

        const player = room.players.get(playerId);
        if (!player) return;

        player.x = msg.x;
        player.y = msg.y;

        // חשב proximity לכל שחקן אחר ושלח עדכונים
        room.players.forEach((other) => {
          if (other.id === playerId) return;

          const dist = getDistance(player.x, player.y, other.x, other.y);
          const canHear = dist <= PROXIMITY_DISTANCE && !player.isDead && !other.isDead;
          const volume = canHear ? calculateVolume(dist) : 0;

          // שלח לשחקן הנוכחי על השחקן האחר
          safeSend(ws, {
            type: 'proximity_update',
            targetId: other.id,
            canHear,
            volume,
            distance: dist,
          });

          // שלח לשחקן האחר על השחקן הנוכחי
          safeSend(other.ws, {
            type: 'proximity_update',
            targetId: playerId,
            canHear,
            volume,
            distance: dist,
          });
        });

        // שדר מיקום לכולם בחדר
        broadcastToRoom(playerRoom, {
          type: 'position_update',
          id: playerId,
          x: player.x,
          y: player.y,
        }, playerId);
        break;
      }

      // WebRTC signaling - offer
      case 'offer': {
        forwardToPlayer(playerRoom, msg.targetId, {
          type: 'offer',
          fromId: playerId,
          sdp: msg.sdp,
        });
        break;
      }

      // WebRTC signaling - answer
      case 'answer': {
        forwardToPlayer(playerRoom, msg.targetId, {
          type: 'answer',
          fromId: playerId,
          sdp: msg.sdp,
        });
        break;
      }

      // WebRTC signaling - ICE candidate
      case 'ice_candidate': {
        forwardToPlayer(playerRoom, msg.targetId, {
          type: 'ice_candidate',
          fromId: playerId,
          candidate: msg.candidate,
        });
        break;
      }

      // שחקן מת
      case 'player_dead': {
        if (!playerRoom) return;
        const room = rooms.get(playerRoom);
        if (!room) return;
        const player = room.players.get(playerId);
        if (player) player.isDead = true;

        broadcastToRoom(playerRoom, {
          type: 'player_dead',
          id: playerId,
        });
        break;
      }

      // פגישה (meeting) - כולם שומעים את כולם
      case 'meeting_start': {
        if (!playerRoom) return;
        broadcastToRoom(playerRoom, { type: 'meeting_start' });
        break;
      }

      case 'meeting_end': {
        if (!playerRoom) return;
        broadcastToRoom(playerRoom, { type: 'meeting_end' });
        break;
      }
    }
  });

  ws.on('close', () => {
    console.log(`[-] Player disconnected: ${playerName || playerId}`);
    if (playerRoom) {
      const room = rooms.get(playerRoom);
      if (room) {
        room.players.delete(playerId);
        broadcastToRoom(playerRoom, { type: 'player_left', id: playerId });
        if (room.players.size === 0) {
          rooms.delete(playerRoom);
          console.log(`[ROOM] Room ${playerRoom} deleted (empty)`);
        }
      }
    }
  });
});

// --- Helper functions ---

function getDistance(x1, y1, x2, y2) {
  return Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
}

function calculateVolume(distance) {
  // ווליום יורד ככל שהמרחק גדל (0.0 - 1.0)
  const vol = 1 - distance / PROXIMITY_DISTANCE;
  return Math.max(0, Math.min(1, vol));
}

function safeSend(ws, data) {
  if (ws.readyState === ws.OPEN) {
    ws.send(JSON.stringify(data));
  }
}

function broadcastToRoom(roomCode, data, excludeId = null) {
  const room = rooms.get(roomCode);
  if (!room) return;
  room.players.forEach((player) => {
    if (player.id !== excludeId) {
      safeSend(player.ws, data);
    }
  });
}

function forwardToPlayer(roomCode, targetId, data) {
  const room = rooms.get(roomCode);
  if (!room) return;
  const target = room.players.get(targetId);
  if (target) safeSend(target.ws, data);
}

server.listen(PORT, () => {
  console.log(`Proximity server running on port ${PORT}`);
});
