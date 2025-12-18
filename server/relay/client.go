package relay

import (
	"encoding/binary"
	"io"
	"log"
	"net"
)

// Client represents a connected TCP client
type Client struct {
	Conn   net.Conn
	Send   chan []byte // Buffered channel for outbound messages
	Server *Server
	RoomID string
}

// ReadPump handles reading from the connection and broadcasting
func (c *Client) ReadPump() {
	defer func() {
		c.Server.Unregister <- Unregistration{Client: c, RoomID: c.RoomID}
		c.Conn.Close()
	}()

	// 1. HANDSHAKE PHASE
	// Expecting: [Length(4)][Type(1)][Payload(...)]
	// Type 0x00 = Handshake (Payload = RoomID)

	header := make([]byte, 4)
	if _, err := io.ReadFull(c.Conn, header); err != nil {
		log.Println("Handshake read error:", err)
		return
	}

	length := int32(binary.LittleEndian.Uint32(header))
	body := make([]byte, length)
	if _, err := io.ReadFull(c.Conn, body); err != nil {
		log.Println("Handshake body read error:", err)
		return
	}

	msgType := body[0]
	if msgType != 0x00 {
		log.Println("Invalid handshake type:", msgType)
		return
	}

	// Payload for Handshake is just RoomID string (UTF8)
	c.RoomID = string(body[1:])
	c.Server.Register <- Registration{Client: c, RoomID: c.RoomID}

	// 2. RELAY LOOP
	// Now we just read packets and forward them as-is (Header + Body).
	// We need to read length first to know how much to read,
	// then we assume the forwarded message preserves the [Length][Type][Payload] structure.

	for {
		// Read Header
		hBuf := make([]byte, 4)
		if _, err := io.ReadFull(c.Conn, hBuf); err != nil {
			break
		}

		dataLen := int32(binary.LittleEndian.Uint32(hBuf))

		// Read Body
		bBuf := make([]byte, dataLen)
		if _, err := io.ReadFull(c.Conn, bBuf); err != nil {
			break
		}

		// Reconstruct full packet to broadcast
		fullPacket := append(hBuf, bBuf...)

		c.Server.Broadcast(fullPacket, c)
	}
}

// WritePump handles sending messages to the client
func (c *Client) WritePump() {
	defer func() {
		c.Conn.Close()
	}()

	for {
		select {
		case message, ok := <-c.Send:
			if !ok {
				// The server closed the channel.
				c.Conn.Write([]byte{})
				return
			}

			w, err := c.Conn.Write(message)
			if err != nil {
				return
			}

			// If we didn't write the full message (unlikely with TCP unless error), handle it?
			// net.Conn.Write usually writes all or returns error.
			_ = w // suppress unused variable warning
		}
	}
}
